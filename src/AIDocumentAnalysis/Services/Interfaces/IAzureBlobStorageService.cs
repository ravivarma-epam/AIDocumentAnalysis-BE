namespace AIDocumentAnalysis.Services.Interfaces
{
    public interface IAzureBlobStorageService
    {
        Task UploadAsync(Stream content, string blobPath, CancellationToken cancellationToken = default);
        Task<Uri> GetBlobUriAsync(string blobPath, CancellationToken cancellationToken = default);
    }
}
