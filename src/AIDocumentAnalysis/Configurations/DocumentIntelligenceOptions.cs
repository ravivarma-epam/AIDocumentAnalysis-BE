namespace AIDocumentAnalysis.Configurations
{
    public class DocumentIntelligenceOptions
    {
        public const string SectionName = "DocumentIntelligence";

        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ModelId { get; set; } = "prebuilt-read";
        public string OutputDirectory { get; set; } = "OutputDirectory";
        public string? BlobConnectionString { get; set; }
        public string? BlobContainerName { get; set; }
    }
}
