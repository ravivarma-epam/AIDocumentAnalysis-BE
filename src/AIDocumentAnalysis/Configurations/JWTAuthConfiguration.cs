namespace AIDocumentAnalysis.Configurations
{
    public class JWTAuthConfiguration
    {
        public const string SectionName = "JwtAuth";
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public int TokenExpiryInMinutes { get; set; }

    }
}
