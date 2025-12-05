using AIDocumentAnalysis.Configurations;
using Serilog;

namespace AIDocumentAnalysis;

public static class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)
                .RegisterConfigurationsAndSecrets(RootStartup.DatabaseConnectionSringKeys)
                .UseSerilog((context, configuration) =>
                {
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.FromLogContext();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                    _ = webBuilder.UseStartup<RootStartup>()
                        .ConfigureKestrel((context, options) =>
                        {
                            options.Limits.MaxRequestBodySize = context.Configuration.GetValue<long>("MaxRequestBodySize");
                        });                
                 });

}
