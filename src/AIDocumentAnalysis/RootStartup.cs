using System.Text.Json;

using AIDocumentAnalysis.Configurations;
using AIDocumentAnalysis.Extensions;
using AIDocumentAnalysis.Services;
using AIDocumentAnalysis.Services.Interfaces;
using AIDocumentAnalysis.Utils.Enums;

using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Storage.Blobs;

using FastEndpoints.Security;
using FastEndpoints.Swagger;

using Flurl;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;


using Serilog;

namespace AIDocumentAnalysis
{
    public class RootStartup
    {
        public static readonly Dictionary<KnownDatabaseServerNames, SupportedRelationalDatabases> DatabaseConnectionSringKeys = new()
        {
            {KnownDatabaseServerNames.PrimaryDb, SupportedRelationalDatabases.PostgreSQL}
        };

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public RootStartup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<JWTAuthConfiguration>(Configuration.GetSection(JWTAuthConfiguration.SectionName));
            services
                .AddOptions<DocumentIntelligenceOptions>()
                .Bind(Configuration.GetSection(DocumentIntelligenceOptions.SectionName))
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.Endpoint)
                        && !string.IsNullOrWhiteSpace(options.ApiKey)
                        && !string.IsNullOrWhiteSpace(options.ModelId)
                        && !string.IsNullOrWhiteSpace(options.OutputDirectory)
                        && !string.IsNullOrWhiteSpace(options.BlobConnectionString)
                        && !string.IsNullOrWhiteSpace(options.BlobContainerName),
                    "DocumentIntelligence Endpoint, ApiKey, ModelId, OutputDirectory, BlobConnectionString, and BlobContainerName must be set.")
                .ValidateOnStart();
            services.AddSingleton(serviceProvider =>
            {
                DocumentIntelligenceOptions options = serviceProvider.GetRequiredService<IOptions<DocumentIntelligenceOptions>>().Value;
                return new DocumentIntelligenceClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
            });

            services.AddSingleton(serviceProvider =>
            {
                DocumentIntelligenceOptions options = serviceProvider.GetRequiredService<IOptions<DocumentIntelligenceOptions>>().Value;
                BlobServiceClient blobServiceClient = new BlobServiceClient(options.BlobConnectionString);
                if (!string.IsNullOrWhiteSpace(options.BlobContainerName))
                {
                    var containerClient = blobServiceClient.GetBlobContainerClient(options.BlobContainerName);
                    containerClient.CreateIfNotExists();
                }
                return blobServiceClient;
            });
            services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();

            services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();
            services.AddHealthChecks();
            services.RegisterDbContexts(Configuration);
            services.ConfigureCorsPolicy(Configuration);
            services.AddSerilog();
            ConfigureAuthentication(services, Configuration);
            services.AddAuthorization();
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                });
            services.AddFastEndpoints()
                .SwaggerDocument(o =>
                    {
                        o.DocumentSettings = s =>
                        {
                            s.Title = "AI Document Analysis API";
                            s.Version = "v3";
                        };
                        o.EnableJWTBearerAuth = true;
                    });
        }

        public void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<AuthService>();
            var juwtConfig = configuration.GetSection(JWTAuthConfiguration.SectionName).Get<JWTAuthConfiguration>();
            services.AddAuthenticationJwtBearer(
                s => s.SigningKey = configuration["Jwt:Key"],
                o =>
                {
                    o.TokenValidationParameters.ValidIssuer = configuration["Jwt:Issuer"];
                    o.TokenValidationParameters.ValidAudience = configuration["Jwt:Audience"];
                    o.TokenValidationParameters.ValidateLifetime = true;
                });

            // 3. Configure Authorization (Secure by Default)
            services.AddAuthorization(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            app.UseDefaultExceptionHandler();
            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseCors("CorsPolicy");
            app.UseSerilogRequestLogging();

            Url composedBasePath = new Url("/api/aida-core");
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.Redirect($"{composedBasePath}/swagger");
                    return;
                }
                await next();
            });            
            app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
            {
                endpoints.MapFastEndpoints(delegate (Config cfg)
                {
                    cfg.Endpoints.RoutePrefix = composedBasePath.ToString().TrimStart('/');
                });
                endpoints.MapControllers();                
            });
            app.UseSwaggerGen(ui =>
            {
                ui.Path = $"{composedBasePath}/swagger/{{documentName}}/swagger.json";
            });
            app.UseSwaggerUi(ui =>
            {
                ui.Path = $"{composedBasePath}/swagger";
                ui.DocumentPath = $"{composedBasePath}/swagger/{{documentName}}/swagger.json";
                ui.ConfigureDefaults();
            });
        }
    }

}
