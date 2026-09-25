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

        public async Task<DocumentAnalysisResult> SaveAnalysisAsync(Stream documentStream, CancellationToken cancellationToken = default)
        {
            string analysis = await GetDocumentJsonAsync(documentStream, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(analysis))
            {
                throw new DocumentIntelligenceException("Azure Document Intelligence returned an empty analysis result.");
            }

            string filePath = GetAnalysisOutputPath();
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
            return JsonSerializer.Serialize(operation.Value);
        }

        #endregion

        #region Private Methods

        private string GetAnalysisOutputPath()
        {
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), _options.OutputDirectory);
            Directory.CreateDirectory(outputDirectory);
            string fileName = $"analysis_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}.txt";
            return Path.Combine(outputDirectory, fileName);
        }

        #endregion
    }
}
