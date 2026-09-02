using AIDocumentAnalysis.Models;

namespace AIDocumentAnalysis.Services.Interfaces
{
    public interface IDocumentIntelligenceService
    {
        Task<DocumentAnalysisResult> SaveAnalysisAsync(Stream documentStream, CancellationToken cancellationToken = default);
        Task<string> GetDocumentJsonAsync(Stream documentStream, string? modelId = null, CancellationToken cancellationToken = default);
    }
}
