namespace AIDocumentAnalysis.Endpoints.DocumentAnalysis
{
    public class AnalyzeRequest
    {
        public IFormFile File { get; set; } = default!;
    }
}
