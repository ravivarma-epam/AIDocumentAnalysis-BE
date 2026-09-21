using System.Text.Json;

using AIDocumentAnalysis.Configurations;
using AIDocumentAnalysis.Models;
using AIDocumentAnalysis.Services.Interfaces;

using Azure;
using Azure.AI.DocumentIntelligence;

namespace AIDocumentAnalysis.Services
{
    public class DocumentIntelligenceService : IDocumentIntelligenceService
    {
        private readonly DocumentIntelligenceClient? _documentIntelligenceClient;
        private readonly DocumentIntelligenceOptions _options;

        public DocumentIntelligenceService(DocumentIntelligenceClient? documentIntelligenceClient, DocumentIntelligenceOptions options)
        {
            _documentIntelligenceClient = documentIntelligenceClient;
            _options = options;
        }

        public async Task<DocumentAnalysisResult> SaveAnalysisAsync(Stream documentStream, CancellationToken cancellationToken = default)
        {
            var analysis = await GetDocumentJsonAsync(documentStream, cancellationToken: cancellationToken);

            if (analysis == null || string.IsNullOrEmpty(analysis))
            {
                return new DocumentAnalysisResult();
            }

            if (string.IsNullOrWhiteSpace("OutputDirectory"))
                throw new InvalidOperationException("No OutputDirectory configured. Set DocumentIntelligence:OutputDirectory in configuration.");

            var outputDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "OutputDirectory"!);
            System.IO.Directory.CreateDirectory(outputDir);

            var fileName = $"analysis_{System.DateTime.UtcNow:yyyyMMddHHmmss}_{System.Guid.NewGuid()}.txt";
            var filePath = System.IO.Path.Combine(outputDir, fileName);

            await System.IO.File.WriteAllTextAsync(filePath, analysis, cancellationToken);

            return new DocumentAnalysisResult
            {
                FilePath = filePath,
                JsonResult = analysis
            };
        }

        public async Task<string> GetDocumentJsonAsync(Stream documentStream, string? modelId = null, CancellationToken cancellationToken = default)
        {
            if (_documentIntelligenceClient == null)
                throw new InvalidOperationException("DocumentIntelligenceClient is not configured. Please set DocumentIntelligence:Endpoint and DocumentIntelligence:ApiKey in configuration.");

            var effectiveModelId = !string.IsNullOrWhiteSpace(modelId) ? modelId : _options.ModelId;

            AnalyzeDocumentOptions options = new AnalyzeDocumentOptions(effectiveModelId!, BinaryData.FromStream(documentStream));
            Operation<AnalyzeResult> operation = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, options);
            var result = await operation.WaitForCompletionAsync(cancellationToken);
            return JsonSerializer.Serialize(result.Value);
        }
    }
}
