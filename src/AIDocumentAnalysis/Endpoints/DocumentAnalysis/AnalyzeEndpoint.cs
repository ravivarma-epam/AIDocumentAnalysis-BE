using AIDocumentAnalysis.Services.Interfaces;

namespace AIDocumentAnalysis.Endpoints.DocumentAnalysis
{
    public class AnalyzeEndpoint : Endpoint<AnalyzeRequest, AnalyzeResponse>
    {
        private readonly IDocumentIntelligenceService _documentIntelligenceService;

        public AnalyzeEndpoint(IDocumentIntelligenceService documentIntelligenceService)
        {
            _documentIntelligenceService = documentIntelligenceService;
        }

        public override void Configure()
        {
            Post("api/documentanalysis/analyze");
            AllowAnonymous();
        }

        public override async Task HandleAsync(AnalyzeRequest req, CancellationToken ct)
        {
            if (req.File == null || req.File.Length == 0)
            {
                await SendAsync(new AnalyzeResponse { FilePath = string.Empty }, 400, ct);
                return;
            }
            await using var stream = req.File.OpenReadStream();
            var saved = await _documentIntelligenceService.SaveAnalysisAsync(stream, ct);

            if (saved == null || string.IsNullOrEmpty(saved.FilePath))
            {
                await SendAsync(new AnalyzeResponse { FilePath = string.Empty }, 500, ct);
                return;
            }

            await SendAsync(new AnalyzeResponse { FilePath = saved.FilePath }, 200, ct);
        }
    }
}
