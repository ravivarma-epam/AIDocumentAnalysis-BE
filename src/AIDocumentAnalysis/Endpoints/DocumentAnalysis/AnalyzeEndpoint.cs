using AIDocumentAnalysis.Models;
using AIDocumentAnalysis.Services.Interfaces;

namespace AIDocumentAnalysis.Endpoints.DocumentAnalysis
{
    public class AnalyzeEndpoint : Endpoint<AnalyzeRequest, AnalyzeResponse>
    {
        private readonly IDocumentIntelligenceService _documentIntelligenceService;
        private readonly ILogger<AnalyzeEndpoint> _logger;

        public AnalyzeEndpoint(IDocumentIntelligenceService documentIntelligenceService, ILogger<AnalyzeEndpoint> logger)
        {
            _documentIntelligenceService = documentIntelligenceService;
            _logger = logger;
        }

        #region Public Methods

        public override void Configure()
        {
            Post("documentanalysis/analyze");
            AllowFileUploads();
        }

        public override async Task HandleAsync(AnalyzeRequest req, CancellationToken ct)
        {
            _logger.LogInformation("Document analysis request received");
            await using Stream stream = req.File.OpenReadStream();
            DocumentAnalysisResult saved = await _documentIntelligenceService.SaveAnalysisAsync(stream, ct);
            await SendOkAsync(new AnalyzeResponse { FilePath = saved.FilePath }, ct);
        }

        #endregion
    }
}
