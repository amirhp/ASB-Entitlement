using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;
using ASB.Entitlements.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace ASB.Entitlements.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly INeo4jContext _context;
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(INeo4jContext context, ILogger<RoleRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Role>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (r:Role {id: $id})
            RETURN r.id as id, r.name as name, r.description as description,
                   r.type as type, r.isActive as isActive, r.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { id });
            if (!await cursor.FetchAsync())
                return Result.Failure<Role>($"Role with ID '{id}' not found");

            var role = HydrateRole(cursor.Current);
            return Result.Success(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role with ID: {Id}", id);
            return Result.Failure<Role>($"Error retrieving role: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<IReadOnlyList<Role>>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = "MATCH (r:Role)";
        if (isActive.HasValue)
            query += $" WHERE r.isActive = {isActive.Value.ToString().ToLower()}";
        query += " RETURN r.id as id, r.name as name, r.description as description, r.type as type, r.isActive as isActive, r.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query);
            var roles = new List<Role>();
            while (await cursor.FetchAsync())
                roles.Add(HydrateRole(cursor.Current));
            return Result.Success<IReadOnlyList<Role>>(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return Result.Failure<IReadOnlyList<Role>>($"Error retrieving roles: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<Role>> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        const string query = @"
            CREATE (r:Role {
                id: $id, name: $name, description: $description,
                type: $type, isActive: $isActive, createdAt: datetime($createdAt)
            })
            RETURN r.id as id, r.name as name, r.description as description,
                   r.type as type, r.isActive as isActive, r.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var parameters = new
            {
                id = role.Id,
                name = role.Name,
                description = role.Description,
                type = role.Type.ToString(),
                isActive = role.IsActive,
                createdAt = role.CreatedAt.ToString("o")
            };

            var cursor = await session.RunAsync(query, parameters);
            if (!await cursor.FetchAsync())
                return Result.Failure<Role>("Failed to create role");

            var createdRole = HydrateRole(cursor.Current);
            _logger.LogInformation("Role created: {Id}", role.Id);
            return Result.Success(createdRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role with ID: {Id}", role.Id);
            return Result.Failure<Role>($"Error creating role: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<Role>> UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (r:Role {id: $id})
            SET r.name = $name, r.description = $description, r.isActive = $isActive
            RETURN r.id as id, r.name as name, r.description as description,
                   r.type as type, r.isActive as isActive, r.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var parameters = new { id = role.Id, name = role.Name, description = role.Description, isActive = role.IsActive };
            var cursor = await session.RunAsync(query, parameters);
            if (!await cursor.FetchAsync())
                return Result.Failure<Role>($"Role with ID '{role.Id}' not found");

            var updatedRole = HydrateRole(cursor.Current);
            _logger.LogInformation("Role updated: {Id}", role.Id);
            return Result.Success(updatedRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role with ID: {Id}", role.Id);
            return Result.Failure<Role>($"Error updating role: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = "MATCH (r:Role {id: $id}) SET r.isActive = false RETURN r.id as id";
        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { id });
            if (!await cursor.FetchAsync())
                return Result.Failure($"Role with ID '{id}' not found");

            _logger.LogInformation("Role soft deleted: {Id}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role with ID: {Id}", id);
            return Result.Failure($"Error deleting role: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<bool>> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = "MATCH (r:Role {id: $id}) RETURN count(r) as count";
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
            _logger.LogError(ex, "Error checking role existence with ID: {Id}", id);
            return Result.Failure<bool>($"Error checking role existence: {ex.Message}");
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

        var role = new Role(id, name, description, type);
        var isActive = record["isActive"].As<bool>();
        if (!isActive) role.Deactivate();

        return role;
    }
}
