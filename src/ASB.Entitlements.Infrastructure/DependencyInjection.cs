using ASB.Entitlements.Domain.Repositories;
using ASB.Entitlements.Domain.Services;
using ASB.Entitlements.Infrastructure.Persistence;
using ASB.Entitlements.Infrastructure.Persistence.Configuration;
using ASB.Entitlements.Infrastructure.Persistence.Repositories;
using ASB.Entitlements.Infrastructure.Seeding;
using ASB.Entitlements.Infrastructure.Services;
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

        // Register aggregate root repositories
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();

        // Register entitlement query repository (read-only operations)
        services.AddScoped<IEntitlementRepository, EntitlementRepository>();

        // Register domain services
        services.AddScoped<IEntitlementDomainService, EntitlementDomainService>();

        // Register data seeder
        services.AddScoped<IDataSeeder, DemoDataSeeder>();

        return services;
    }
}
