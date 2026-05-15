using ASB.Entitlements.Domain.Repositories;
using ASB.Entitlements.Infrastructure.Persistence;
using ASB.Entitlements.Infrastructure.Persistence.Configuration;
using ASB.Entitlements.Infrastructure.Persistence.Repositories;
using ASB.Entitlements.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ASB.Entitlements.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Neo4j settings
        services.Configure<Neo4jSettings>(
            configuration.GetSection(Neo4jSettings.SectionName));

        // Register Neo4j context
        services.AddSingleton<INeo4jContext, Neo4jContext>();

        // Register repositories
        services.AddScoped<IEntitlementRepository, EntitlementRepository>();

        // Register data seeder
        services.AddScoped<IDataSeeder, DemoDataSeeder>();

        return services;
    }
}
