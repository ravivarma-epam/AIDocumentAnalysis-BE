using AIDocumentAnalysis.Configurations;
using AIDocumentAnalysis.Services.Interfaces;
using Microsoft.Extensions.Options;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AIDocumentAnalysis.Services
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public AzureBlobStorageService(BlobServiceClient blobServiceClient, IOptions<DocumentIntelligenceOptions> options)
        {
            _blobServiceClient = blobServiceClient;
            _containerName = options.Value.BlobContainerName!;
        }

        public async Task UploadAsync(Stream content, string blobPath, CancellationToken cancellationToken = default)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            string normalized = blobPath.Replace('\\', '/').TrimStart('/');

            BlobClient blobClient = containerClient.GetBlobClient(normalized);
            Stream uploadStream;
            if (content.CanSeek)
            {
                content.Position = 0;
                uploadStream = content;
            }
            else
            {
                MemoryStream ms = new MemoryStream();
                await content.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
                ms.Position = 0;
                uploadStream = ms;
            }

            await blobClient.UploadAsync(uploadStream, overwrite: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public Task<Uri> GetBlobUriAsync(string blobPath, CancellationToken cancellationToken = default)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            string normalized = blobPath.Replace('\\', '/').TrimStart('/');
            BlobClient blobClient = containerClient.GetBlobClient(normalized);
            return Task.FromResult(blobClient.Uri);
        }
    }
}
