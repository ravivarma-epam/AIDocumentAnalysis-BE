using AIDocumentAnalysis.Utils.Enums;

namespace AIDocumentAnalysis.Configurations
{
    public static class ApplicationConfigurationService
    {
        public static IHostBuilder RegisterConfigurationsAndSecrets(this IHostBuilder builder, Dictionary<KnownDatabaseServerNames, SupportedRelationalDatabases> databaseConnectionStringKeys, TextFileService? textFileService = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(databaseConnectionStringKeys, nameof(databaseConnectionStringKeys));

            var fileService = textFileService ?? new TextFileService();

            builder = builder.ConfigureAppConfiguration(_ =>
            {
                CheckRequiredFiles(fileService, databaseConnectionStringKeys.Keys);
            })
            .ConfigureApplicationSettings()
            .ConfigureApplicationSecrets(databaseConnectionStringKeys);

            return builder;
        }

        private static void CheckRequiredFiles(TextFileService fileService, IEnumerable<KnownDatabaseServerNames> databases)
        {
            ArgumentNullException.ThrowIfNull(fileService);

            // Check AppSecrets.json
            if (!fileService.DoesFileExists(new FilePathString("Secrets/AppSecrets.json")))
                throw new FileNotFoundException("File AppSecrets.json is missing");

            // Check database-specific secrets
            foreach (var db in databases)
            {
                var fileName = $"Secrets/AppSecrets.{db}.json";
                if (!fileService.DoesFileExists(new FilePathString(fileName)) &&
                    !fileService.DoesFileExists(new FilePathString(fileName.ToLowerInvariant())))
                {
                    throw new FileNotFoundException($"File AppSecrets.{db}.json is missing");
                }
            }

            // Check AppSettings.json
            if (!fileService.DoesFileExists(new FilePathString("AppSettings.json")) &&
                !fileService.DoesFileExists(new FilePathString("appsettings.json")))
            {
                throw new FileNotFoundException("File AppSettings.json is missing");
            }

            // Check environment-specific AppSettings
            foreach (var env in Enum.GetValues(typeof(KnownEnvironment)))
            {
                var envFile = $"AppSettings.{env}.json";
                if (!fileService.DoesFileExists(new FilePathString(envFile)) &&
                    !fileService.DoesFileExists(new FilePathString(envFile.ToLowerInvariant())))
                {
                    throw new FileNotFoundException($"File AppSettings.{env}.json is missing");
                }
            }
        }

        private static IHostBuilder ConfigureApplicationSettings(this IHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("AppSettings.json");

                if (hostingContext.HostingEnvironment.IsDevelopment())
                {
                    config.AddJsonFile("AppSettings.Development.json");
                }
                else
                {
                    foreach (var env in Enum.GetValues<KnownEnvironment>())
                    {
                        var envName = env.ToString();
                        if (hostingContext.HostingEnvironment.IsEnvironment(envName.ToLowerInvariant()) ||
                            hostingContext.HostingEnvironment.IsEnvironment(envName.ToUpperInvariant()) ||
                            hostingContext.HostingEnvironment.IsEnvironment(envName))
                        {
                            config.AddJsonFile($"AppSettings.{envName}.json");
                            break;
                        }
                    }
                }
            });
        }

        private static IHostBuilder ConfigureApplicationSecrets(this IHostBuilder builder, Dictionary<KnownDatabaseServerNames, SupportedRelationalDatabases> databaseConnectionStringKeys)
        {

            return builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("Secrets/AppSecrets.json", optional: false, reloadOnChange: true);

                foreach (var databaseServer in databaseConnectionStringKeys)
                {
                    _ = databaseServer.Value switch
                    {
                        SupportedRelationalDatabases.PostgreSQL =>
                        config.AddInMemoryCollection(initialData: PostgreSqlConnectionConfiguration.ReadConfigurationAndSecretsAsProperties(databaseServer.Key)),
                        _ => throw new InvalidOperationException("Unknown database server")
                    };
                }
            });
        }
    }
}
