using System.Text.Json;

using AIDocumentAnalysis.Configurations;
using AIDocumentAnalysis.Extensions;
using AIDocumentAnalysis.Services;
using AIDocumentAnalysis.Utils.Enums;

using FastEndpoints.Security;
using FastEndpoints.Swagger;

using Flurl;

using Microsoft.AspNetCore.Authorization;

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
            ConfigureApplicationConfiguration(services);
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

        public void ConfigureApplicationConfiguration(IServiceCollection services)
        {
            services.Configure<JWTAuthConfiguration>(Configuration.GetSection(JWTAuthConfiguration.SectionName));
        }
    }

}
