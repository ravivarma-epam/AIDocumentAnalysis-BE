using AIDocumentAnalysis.Configurations;
using AIDocumentAnalysis.DataAccess;
using AIDocumentAnalysis.Utils.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AIDocumentAnalysis
{
    public static class RegisterDbContextConfiguration
    {
        public static IServiceCollection RegisterDbContexts(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var dataBuilder = new NpgsqlDataSourceBuilder(configuration.ObtainPostgresqlConnectionString(KnownDatabaseServerNames.PrimaryDb));
            NpgsqlDataSource dataSource = dataBuilder.Build();
            services.AddDbContext<AidaServiceContext>(options =>
                options.UseNpgsql(dataSource));
            return services;
        }
    }
}
