using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;
using ASB.Entitlements.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace ASB.Entitlements.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly INeo4jContext _context;
    private readonly ILogger<PermissionRepository> _logger;

    public PermissionRepository(INeo4jContext context, ILogger<PermissionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Permission>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (p:Permission {id: $id})
            RETURN p.id as id, p.name as name, p.action as action, p.description as description,
                   p.scope as scope, p.isActive as isActive, p.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { id });
            if (!await cursor.FetchAsync())
                return Result.Failure<Permission>($"Permission with ID '{id}' not found");

            var permission = HydratePermission(cursor.Current);
            return Result.Success(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission with ID: {Id}", id);
            return Result.Failure<Permission>($"Error retrieving permission: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<IReadOnlyList<Permission>>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = "MATCH (p:Permission)";
        if (isActive.HasValue)
            query += $" WHERE p.isActive = {isActive.Value.ToString().ToLower()}";
        query += " RETURN p.id as id, p.name as name, p.action as action, p.description as description, p.scope as scope, p.isActive as isActive, p.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query);
            var permissions = new List<Permission>();
            while (await cursor.FetchAsync())
                permissions.Add(HydratePermission(cursor.Current));
            return Result.Success<IReadOnlyList<Permission>>(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions");
            return Result.Failure<IReadOnlyList<Permission>>($"Error retrieving permissions: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<Permission>> CreateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        const string query = @"
            CREATE (p:Permission {
                id: $id, name: $name, action: $action, description: $description,
                scope: $scope, isActive: $isActive, createdAt: datetime($createdAt)
            })
            RETURN p.id as id, p.name as name, p.action as action, p.description as description,
                   p.scope as scope, p.isActive as isActive, p.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var parameters = new
            {
                id = permission.Id,
                name = permission.Name,
                action = permission.Action,
                description = permission.Description,
                scope = permission.Scope.ToString(),
                isActive = permission.IsActive,
                createdAt = permission.CreatedAt.ToString("o")
            };

            var cursor = await session.RunAsync(query, parameters);
            if (!await cursor.FetchAsync())
                return Result.Failure<Permission>("Failed to create permission");

            var createdPermission = HydratePermission(cursor.Current);
            _logger.LogInformation("Permission created: {Id}", permission.Id);
            return Result.Success(createdPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission with ID: {Id}", permission.Id);
            return Result.Failure<Permission>($"Error creating permission: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<Permission>> UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (p:Permission {id: $id})
            SET p.name = $name, p.description = $description, p.isActive = $isActive
            RETURN p.id as id, p.name as name, p.action as action, p.description as description,
                   p.scope as scope, p.isActive as isActive, p.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var parameters = new { id = permission.Id, name = permission.Name, description = permission.Description, isActive = permission.IsActive };
            var cursor = await session.RunAsync(query, parameters);
            if (!await cursor.FetchAsync())
                return Result.Failure<Permission>($"Permission with ID '{permission.Id}' not found");

            var updatedPermission = HydratePermission(cursor.Current);
            _logger.LogInformation("Permission updated: {Id}", permission.Id);
            return Result.Success(updatedPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission with ID: {Id}", permission.Id);
            return Result.Failure<Permission>($"Error updating permission: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = "MATCH (p:Permission {id: $id}) SET p.isActive = false RETURN p.id as id";
        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { id });
            if (!await cursor.FetchAsync())
                return Result.Failure($"Permission with ID '{id}' not found");

            _logger.LogInformation("Permission soft deleted: {Id}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission with ID: {Id}", id);
            return Result.Failure($"Error deleting permission: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<bool>> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = "MATCH (p:Permission {id: $id}) RETURN count(p) as count";
        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { id });
            if (!await cursor.FetchAsync())
                return Result.Success(false);

            var count = cursor.Current["count"].As<long>();
            return Result.Success(count > 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission existence with ID: {Id}", id);
            return Result.Failure<bool>($"Error checking permission existence: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private Permission HydratePermission(IRecord record)
    {
        var id = record["id"].As<string>();
        var name = record["name"].As<string>();
        var action = record["action"].As<string>();
        var description = record["description"].As<string>();
        var scopeString = record["scope"].As<string>();
        var scope = Enum.Parse<PermissionScope>(scopeString);

        var permission = new Permission(id, name, action, description, scope);
        var isActive = record["isActive"].As<bool>();
        if (!isActive) permission.Deactivate();

        return permission;
    }
}
