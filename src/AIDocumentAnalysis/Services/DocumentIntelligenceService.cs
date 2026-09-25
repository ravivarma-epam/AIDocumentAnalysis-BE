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

        public DocumentIntelligenceService(DocumentIntelligenceClient documentIntelligenceClient, IOptions<DocumentIntelligenceOptions> options, ILogger<DocumentIntelligenceService> logger)
        {
            _documentIntelligenceClient = documentIntelligenceClient;
            _options = options.Value;
            _logger = logger;
        }

        #region Public Methods

        public async Task<DocumentAnalysisResult> SaveAnalysisAsync(Stream documentStream, string? inputFileName = null, CancellationToken cancellationToken = default)
        {
            string analysis = await GetDocumentJsonAsync(documentStream, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(analysis))
            {
                throw new DocumentIntelligenceException("Azure Document Intelligence returned an empty analysis result.");
            }

            string filePath = GetAnalysisOutputPath(inputFileName);
            await File.WriteAllTextAsync(filePath, analysis, cancellationToken);
            _logger.LogInformation("Saved document analysis to {FilePath}", filePath);

            return new DocumentAnalysisResult
            {
                FilePath = filePath,
                JsonResult = analysis
            };
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
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(operation.Value, jsonOptions);
        }

        #endregion

        #region Private Methods

        private string GetAnalysisOutputPath(string? inputFileName)
        {
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), _options.OutputDirectory);
            Directory.CreateDirectory(outputDirectory);
            string fileName = $"Analysis_{GetSanitizedFileName(inputFileName)}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
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
