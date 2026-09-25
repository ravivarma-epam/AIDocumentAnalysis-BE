using System.Text;
using System.Text.Json;

using AIDocumentAnalysis.Configurations;
using AIDocumentAnalysis.Exceptions;
using AIDocumentAnalysis.Models;
using AIDocumentAnalysis.Services.Interfaces;

using Azure;
using Azure.AI.DocumentIntelligence;

using Microsoft.Extensions.Options;

namespace AIDocumentAnalysis.Services
{
    public class DocumentIntelligenceService : IDocumentIntelligenceService
    {
        private readonly DocumentIntelligenceClient _documentIntelligenceClient;
        private readonly DocumentIntelligenceOptions _options;
        private readonly ILogger<DocumentIntelligenceService> _logger;
        private readonly IAzureBlobStorageService _azureBlobStorageService;

        public DocumentIntelligenceService(DocumentIntelligenceClient documentIntelligenceClient, IOptions<DocumentIntelligenceOptions> options, ILogger<DocumentIntelligenceService> logger, IAzureBlobStorageService azureBlobStorageService)
        {
            _documentIntelligenceClient = documentIntelligenceClient;
            _options = options.Value;
            _logger = logger;
            _azureBlobStorageService = azureBlobStorageService;
        }

        #region Public Methods

        public async Task<DocumentAnalysisResult> SaveAnalysisAsync(Stream documentStream, string? inputFileName = null, CancellationToken cancellationToken = default)
        {
            string analysis = await GetDocumentJsonAsync(documentStream, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(analysis))
            {
                throw new DocumentIntelligenceException("Azure Document Intelligence returned an empty analysis result.");
            }

            DateTime dateTimeUtcNow = DateTime.UtcNow;
            string blobFolder = $"Analysis_{dateTimeUtcNow:yyyyMMdd}";
            string sanitizedFileName = GetSanitizedFileName(inputFileName);
            string blobFileName = $"Analysis_{sanitizedFileName}_{dateTimeUtcNow:yyyyMMddHHmmss}.json";
            string blobPath = $"{blobFolder}/{blobFileName}";

            string filePath = GetAnalysisOutputPath(sanitizedFileName, dateTimeUtcNow);
            await File.WriteAllTextAsync(filePath, analysis, cancellationToken);
            _logger.LogInformation("Saved document analysis to {FilePath}", filePath);

            try
            {
                using MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(analysis));
                await _azureBlobStorageService.UploadAsync(ms, blobPath, cancellationToken);
                _logger.LogInformation("Uploaded analysis to blob storage at {BlobPath}", blobPath);
                Uri uri = await _azureBlobStorageService.GetBlobUriAsync(blobPath, cancellationToken);
                return new DocumentAnalysisResult
                {
                    FilePath = uri.ToString(),
                    JsonResult = analysis
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload analysis to Azure Blob Storage for blobPath {BlobPath}", blobPath);
                throw new DocumentIntelligenceException("Failed to store analysis in Azure Blob Storage.", ex);
            }
        }

        public async Task<string> GetDocumentJsonAsync(Stream documentStream, string? modelId = null, CancellationToken cancellationToken = default)
        {
            string effectiveModelId = !string.IsNullOrWhiteSpace(modelId) ? modelId : _options.ModelId;
            AnalyzeDocumentOptions analyzeDocumentOptions = new AnalyzeDocumentOptions(effectiveModelId, BinaryData.FromStream(documentStream));
            Operation<AnalyzeResult> operation = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, analyzeDocumentOptions, cancellationToken);
            _logger.LogInformation("Completed Azure document analysis using model {ModelId}", effectiveModelId);
            if (operation.Value is null)
            {
                throw new DocumentIntelligenceException("Azure Document Intelligence returned a null analysis result.");
            }
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(operation.Value, jsonOptions);
        }

        #endregion

        #region Private Methods

        private string GetAnalysisOutputPath(string? inputFileName, DateTime dateTimeUtcNow)
        {
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), _options.OutputDirectory);
            Directory.CreateDirectory(outputDirectory);
            string fileName = $"Analysis_{inputFileName}_{dateTimeUtcNow:yyyyMMddHHmmss}.json";
            return Path.Combine(outputDirectory, fileName);
        }

        private static string GetSanitizedFileName(string? inputFileName)
        {
            string baseName;
            if (!string.IsNullOrWhiteSpace(inputFileName))
            {
                try
                {
                    baseName = Path.GetFileNameWithoutExtension(inputFileName);
                }
                catch
                {
                    baseName = inputFileName ?? "document";
                }
            }
            else
            {
                baseName = "document";
            }
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(invalid, '_');
            }

            if (baseName.Length > 50)
            {
                baseName = baseName.Substring(0, 50);
            }

            return baseName;
        }

        #endregion
    }
}
