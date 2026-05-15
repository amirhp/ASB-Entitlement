using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Repositories;
using ASB.Entitlements.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace ASB.Entitlements.Infrastructure.Persistence.Repositories;

public sealed class EntitlementRepository : IEntitlementRepository
{
    private readonly INeo4jContext _context;
    private readonly ILogger<EntitlementRepository> _logger;

    public EntitlementRepository(
        INeo4jContext context,
        ILogger<EntitlementRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<EntitlementCheckResult>> CheckEntitlementAsync(
        string identityId,
        string resourceId,
        string action,
        CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (i:Identity {id: $identityId})
            MATCH (res:Resource {id: $resourceId})
            OPTIONAL MATCH path = (i)-[:HAS_ROLE]->(r:Role)-[:GRANTS]->(p:Permission)-[:ON]->(res)
            WHERE p.action = $action AND p.isActive = true
            WITH i, res, path, p
            RETURN
                CASE WHEN path IS NOT NULL
                    THEN true
                    ELSE false
                END as entitled,
                CASE WHEN path IS NOT NULL
                    THEN p.name
                    ELSE null
                END as permissionName,
                CASE WHEN path IS NOT NULL
                    THEN 'Access granted via role-based entitlement'
                    WHEN i IS NULL
                        THEN 'Identity not found'
                    WHEN res IS NULL
                        THEN 'Resource not found'
                    ELSE 'No matching permission found for the requested action'
                END as reason";

        var session = _context.GetSession();
        try
        {
            _logger.LogDebug(
                "Executing entitlement check query for Identity: {IdentityId}, Resource: {ResourceId}, Action: {Action}",
                identityId, resourceId, action);

            var cursor = await session.RunAsync(query, new
            {
                identityId,
                resourceId,
                action
            });

            if (!await cursor.FetchAsync())
            {
                _logger.LogWarning("Entitlement check returned no results");
                return Result.Success(EntitlementCheckResult.Denied(
                    "Unable to determine entitlement status"));
            }

            var record = cursor.Current;
            var entitled = record["entitled"].As<bool>();
            var reason = record["reason"].As<string>();
            var permissionName = record["permissionName"].As<string?>();

            var result = entitled
                ? EntitlementCheckResult.Granted(permissionName!, reason)
                : EntitlementCheckResult.Denied(reason);

            _logger.LogInformation(
                "Entitlement check completed. Entitled: {Entitled}, Reason: {Reason}",
                entitled, reason);

            return Result.Success(result);
        }
        catch (Neo4jException ex)
        {
            _logger.LogError(ex,
                "Neo4j error during entitlement check for Identity: {IdentityId}",
                identityId);

            return Result.Failure<EntitlementCheckResult>(
                $"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during entitlement check for Identity: {IdentityId}",
                identityId);

            return Result.Failure<EntitlementCheckResult>(
                "An unexpected error occurred while checking entitlement");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<IReadOnlyList<string>>> GetIdentityPermissionsAsync(
        string identityId,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (i:Identity {id: $identityId})-[:HAS_ROLE]->(r:Role)-[:GRANTS]->(p:Permission)-[:ON]->(res:Resource {id: $resourceId})
            WHERE p.isActive = true
            RETURN DISTINCT p.name as permissionName";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { identityId, resourceId });
            var permissions = new List<string>();

            while (await cursor.FetchAsync())
            {
                permissions.Add(cursor.Current["permissionName"].As<string>());
            }

            return Result.Success<IReadOnlyList<string>>(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving permissions for Identity: {IdentityId}, Resource: {ResourceId}",
                identityId, resourceId);

            return Result.Failure<IReadOnlyList<string>>(
                $"Error retrieving permissions: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<IReadOnlyList<string>>> GetIdentityRolesAsync(
        string identityId,
        CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (i:Identity {id: $identityId})-[:HAS_ROLE]->(r:Role)
            WHERE r.isActive = true
            RETURN r.name as roleName";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { identityId });
            var roles = new List<string>();

            while (await cursor.FetchAsync())
            {
                roles.Add(cursor.Current["roleName"].As<string>());
            }

            return Result.Success<IReadOnlyList<string>>(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving roles for Identity: {IdentityId}",
                identityId);

            return Result.Failure<IReadOnlyList<string>>(
                $"Error retrieving roles: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }
}
