namespace AIDocumentAnalysis.Endpoints.DocumentAnalysis
{
    public class AnalyzeRequest
    {
        public IFormFile File { get; set; } = default!;
    }

    public class AnalyzeRequestValidator : Validator<AnalyzeRequest>
    {
        public AnalyzeRequestValidator()
        {
            RuleFor(request => request.File)
                .NotNull()
                .WithMessage("File is required.");

            RuleFor(request => request.File.Length)
                .GreaterThan(0)
                .When(request => request.File != null)
                .WithMessage("File is empty.");
        }
    }
}
