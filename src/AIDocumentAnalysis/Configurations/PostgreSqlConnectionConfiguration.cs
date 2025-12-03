using System.Text.Json;
using System.Text.Json.Serialization;

using AIDocumentAnalysis.Utils.Enums;

namespace AIDocumentAnalysis.Configurations
{
    public static class PostgreSqlConnectionConfiguration
    {
        public static string ObtainPostgresqlConnectionString(this IConfiguration configuration, KnownDatabaseServerNames databaseKey)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(databaseKey);
            var connectionStringKey = databaseKey.ToString();

            string? connectionString = configuration.GetConnectionString(connectionStringKey);

            var userId = configuration[$"ConnectionStrings:{connectionStringKey}:username"];
            var password = configuration[$"ConnectionStrings:{connectionStringKey}:password"];
            var port = configuration[$"ConnectionStrings:{connectionStringKey}:port"];
            var databaseName = configuration[$"ConnectionStrings:{connectionStringKey}:databaseName"];
            var host = configuration[$"ConnectionStrings:{connectionStringKey}:host"];

            return connectionString
                .Replace("[[UserId]]", userId, StringComparison.InvariantCulture)
                .Replace("[[Password]]", password, StringComparison.InvariantCulture)
                .Replace("[[Port]]", port, StringComparison.InvariantCulture)
                .Replace("[[DatabaseName]]", databaseName, StringComparison.InvariantCulture)
                .Replace("[[Host]]", host, StringComparison.InvariantCulture);

        }

        public static List<KeyValuePair<string, string?>> ReadConfigurationAndSecretsAsProperties(KnownDatabaseServerNames databaseKey)
        {
            var databaseKeyAsString = databaseKey.ToString();

            var jsonFileReaderService = new JsonFileReaderService($"Secrets/AppSecrets.{databaseKeyAsString}.json");
            var secretsData = jsonFileReaderService.ReadContentAndParseAsync<PostgresqlSecrets>().Result;

            var properties = new List<KeyValuePair<string, string?>>()
            {
                new($"ConnectionStrings:{databaseKeyAsString}:username", secretsData.Username),
                new($"ConnectionStrings:{databaseKeyAsString}:password", secretsData.Password),
                new($"ConnectionStrings:{databaseKeyAsString}:port", secretsData.Port.ToString()),
                new($"ConnectionStrings:{databaseKeyAsString}:databaseName", secretsData.DatabaseName),
                new($"ConnectionStrings:{databaseKeyAsString}:host", secretsData.Host)
            };

            return properties;
        }
    }

    /// <summary>
    /// Represents the structure of the PostgreSQL secrets JSON file
    /// </summary>
    public class PostgresqlSecrets
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("host")]
        public string Host { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public int Port { get; set; }

        [JsonPropertyName("databaseName")]
        public string DatabaseName { get; set; } = string.Empty;
    }

    public class JsonFileReaderService
    {
        private readonly string _filePath;
        public JsonFileReaderService(string filePath)
        {
            _filePath = filePath;
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"{filePath} is not a valid file path", nameof(filePath));
            }
        }

        public async Task<T> ReadContentAndParseAsync<T>()
        {
            using (FileStream fileStream = File.OpenRead(_filePath))
            {
                return await JsonSerializer.DeserializeAsync<T>(fileStream) ?? throw new InvalidOperationException("Failed to deserialize JSON content");
            }
        }
    }
}