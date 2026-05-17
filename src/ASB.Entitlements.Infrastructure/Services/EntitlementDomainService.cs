using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;
using ASB.Entitlements.Domain.Repositories;
using ASB.Entitlements.Domain.Services;
using ASB.Entitlements.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace ASB.Entitlements.Infrastructure.Services;

public sealed class EntitlementDomainService : IEntitlementDomainService
{
    private readonly INeo4jContext _context;
    private readonly IIdentityRepository _identityRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly ILogger<EntitlementDomainService> _logger;

    public EntitlementDomainService(
        INeo4jContext context,
        IIdentityRepository identityRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IResourceRepository resourceRepository,
        ILogger<EntitlementDomainService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _identityRepository = identityRepository ?? throw new ArgumentNullException(nameof(identityRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _resourceRepository = resourceRepository ?? throw new ArgumentNullException(nameof(resourceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> AssignRoleToIdentityAsync(
        string identityId,
        string roleId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assigning role {RoleId} to identity {IdentityId}", roleId, identityId);

        // Validate entities exist
        var identityExists = await _identityRepository.ExistsAsync(identityId, cancellationToken);
        if (identityExists.IsFailure || !identityExists.Value)
            return Result.Failure($"Identity '{identityId}' not found");

        var roleExists = await _roleRepository.ExistsAsync(roleId, cancellationToken);
        if (roleExists.IsFailure || !roleExists.Value)
            return Result.Failure($"Role '{roleId}' not found");

        const string query = @"
            MATCH (i:Identity {id: $identityId})
            MATCH (r:Role {id: $roleId})
            MERGE (i)-[:HAS_ROLE]->(r)
            RETURN i.id as identityId, r.id as roleId";

        var session = _context.GetSession();
        try
        {
            await session.RunAsync(query, new { identityId, roleId });
            _logger.LogInformation("Role {RoleId} assigned to identity {IdentityId}", roleId, identityId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to identity {IdentityId}", roleId, identityId);
            return Result.Failure($"Error assigning role: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result> RemoveRoleFromIdentityAsync(
        string identityId,
        string roleId,
        CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (i:Identity {id: $identityId})-[rel:HAS_ROLE]->(r:Role {id: $roleId})
            DELETE rel
            RETURN count(rel) as deletedCount";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { identityId, roleId });
            await cursor.FetchAsync();
            var deletedCount = cursor.Current["deletedCount"].As<long>();

            if (deletedCount == 0)
            {
                return Result.Failure($"Role assignment not found between identity '{identityId}' and role '{roleId}'");
            }

            _logger.LogInformation("Role {RoleId} removed from identity {IdentityId}", roleId, identityId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleId} from identity {IdentityId}", roleId, identityId);
            return Result.Failure($"Error removing role: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result> GrantPermissionToRoleAsync(
        string roleId,
        string permissionId,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Granting permission {PermissionId} to role {RoleId} for resource {ResourceId}",
            permissionId, roleId, resourceId);

        // Validate entities exist
        var roleExists = await _roleRepository.ExistsAsync(roleId, cancellationToken);
        if (roleExists.IsFailure || !roleExists.Value)
            return Result.Failure($"Role '{roleId}' not found");

        var permissionExists = await _permissionRepository.ExistsAsync(permissionId, cancellationToken);
        if (permissionExists.IsFailure || !permissionExists.Value)
            return Result.Failure($"Permission '{permissionId}' not found");

        var resourceExists = await _resourceRepository.ExistsAsync(resourceId, cancellationToken);
        if (resourceExists.IsFailure || !resourceExists.Value)
            return Result.Failure($"Resource '{resourceId}' not found");

        const string query = @"
            MATCH (r:Role {id: $roleId})
            MATCH (p:Permission {id: $permissionId})
            MATCH (res:Resource {id: $resourceId})
            MERGE (r)-[:GRANTS]->(p)
            MERGE (p)-[:ON]->(res)
            RETURN r.id as roleId, p.id as permissionId, res.id as resourceId";

        var session = _context.GetSession();
        try
        {
            await session.RunAsync(query, new { roleId, permissionId, resourceId });
            _logger.LogInformation(
                "Permission {PermissionId} granted to role {RoleId} for resource {ResourceId}",
                permissionId, roleId, resourceId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error granting permission {PermissionId} to role {RoleId} for resource {ResourceId}",
                permissionId, roleId, resourceId);
            return Result.Failure($"Error granting permission: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result> RevokePermissionFromRoleAsync(
        string roleId,
        string permissionId,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (r:Role {id: $roleId})-[grants:GRANTS]->(p:Permission {id: $permissionId})-[on:ON]->(res:Resource {id: $resourceId})
            DELETE grants, on
            RETURN count(grants) as deletedCount";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { roleId, permissionId, resourceId });
            await cursor.FetchAsync();
            var deletedCount = cursor.Current["deletedCount"].As<long>();

            if (deletedCount == 0)
            {
                return Result.Failure($"Permission grant not found");
            }

            _logger.LogInformation(
                "Permission {PermissionId} revoked from role {RoleId} for resource {ResourceId}",
                permissionId, roleId, resourceId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error revoking permission {PermissionId} from role {RoleId} for resource {ResourceId}",
                permissionId, roleId, resourceId);
            return Result.Failure($"Error revoking permission: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<IReadOnlyList<Role>>> GetIdentityRolesAsync(
        string identityId,
        CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (i:Identity {id: $identityId})-[:HAS_ROLE]->(r:Role)
            WHERE r.isActive = true
            RETURN r.id as id, r.name as name, r.description as description,
                   r.type as type, r.isActive as isActive, r.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { identityId });
            var roles = new List<Role>();

            while (await cursor.FetchAsync())
            {
                var record = cursor.Current;
                var role = HydrateRole(record);
                roles.Add(role);
            }

            return Result.Success<IReadOnlyList<Role>>(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for identity {IdentityId}", identityId);
            return Result.Failure<IReadOnlyList<Role>>($"Error retrieving roles: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<IReadOnlyList<Permission>>> GetIdentityPermissionsForResourceAsync(
        string identityId,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (i:Identity {id: $identityId})-[:HAS_ROLE]->(r:Role)-[:GRANTS]->(p:Permission)-[:ON]->(res:Resource {id: $resourceId})
            WHERE p.isActive = true
            RETURN DISTINCT p.id as id, p.name as name, p.action as action,
                   p.description as description, p.scope as scope,
                   p.isActive as isActive, p.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { identityId, resourceId });
            var permissions = new List<Permission>();

            while (await cursor.FetchAsync())
            {
                var record = cursor.Current;
                var permission = HydratePermission(record);
                permissions.Add(permission);
            }

            return Result.Success<IReadOnlyList<Permission>>(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving permissions for identity {IdentityId} and resource {ResourceId}",
                identityId, resourceId);
            return Result.Failure<IReadOnlyList<Permission>>($"Error retrieving permissions: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private Role HydrateRole(IRecord record)
    {
        var id = record["id"].As<string>();
        var name = record["name"].As<string>();
        var description = record["description"].As<string>();
        var typeString = record["type"].As<string>();
        var type = Enum.Parse<RoleType>(typeString);

        return new Role(id, name, description, type);
    }

    private Permission HydratePermission(IRecord record)
    {
        var id = record["id"].As<string>();
        var name = record["name"].As<string>();
        var action = record["action"].As<string>();
        var description = record["description"].As<string>();
        var scopeString = record["scope"].As<string>();
        var scope = Enum.Parse<PermissionScope>(scopeString);

        return new Permission(id, name, action, description, scope);
    }
}
