using System.Text.Json;

using AIDocumentAnalysis.Configurations;
using AIDocumentAnalysis.Extensions;
using AIDocumentAnalysis.Utils.Enums;

using FastEndpoints.Swagger;

using Flurl;

using NSwag;
using NSwag.AspNetCore;

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
            services.AddHealthChecks();
            services.RegisterDbContexts(Configuration);
            services.ConfigureCorsPolicy(Configuration);
            services.AddSerilog();
            services.AddAuthentication();
            services.AddAuthorization();
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                });
            services.AddFastEndpoints().AddOpenApiDocument().AddEndpointsApiExplorer();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            app.UseDefaultExceptionHandler();
            app.UseStaticFiles();
            app.UseHttpsRedirection();
            if (environment.IsDevelopment())
            {
                app.UseHsts();
            }
            app.UseCors();
            app.UseSerilogRequestLogging();
           
            Url composedBasePath = new Url("/api/aida-core");
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.Redirect("/swagger");
                    return;
                }
                await next();
            });

            app.UseOpenApi();
            app.UseSwaggerUi(x => x.ConfigureDefaults());
            app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
            {
                endpoints.MapFastEndpoints(delegate (Config cfg)
                {
                    cfg.Endpoints.RoutePrefix = composedBasePath.ToString().TrimStart('/');
                });
                endpoints.MapControllers();
            });
            app.UseSwaggerGen(delegate (OpenApiDocumentMiddlewareSettings cfg)
            {
                cfg.Path = composedBasePath.Clone().AppendPathSegment("swagger").AppendPathSegment("v1")
                    .AppendPathSegment("swagger.json");
                cfg.PostProcess = delegate (OpenApiDocument context, HttpRequest next)
                {
                    context.Schemes.Clear();
                    context.Host = string.Empty;
                };
            });            
        }
    }

}
