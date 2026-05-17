using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;
using ASB.Entitlements.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace ASB.Entitlements.Infrastructure.Persistence.Repositories;

public sealed class IdentityRepository : IIdentityRepository
{
    private readonly INeo4jContext _context;
    private readonly ILogger<IdentityRepository> _logger;

    public IdentityRepository(
        INeo4jContext context,
        ILogger<IdentityRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Identity>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (i:Identity {id: $id})
            RETURN i.id as id, i.name as name, i.type as type,
                   i.isActive as isActive, i.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { id });

            if (!await cursor.FetchAsync())
            {
                return Result.Failure<Identity>($"Identity with ID '{id}' not found");
            }

            var record = cursor.Current;
            var identity = HydrateIdentity(record);

            return Result.Success(identity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving identity with ID: {Id}", id);
            return Result.Failure<Identity>($"Error retrieving identity: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<IReadOnlyList<Identity>>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = "MATCH (i:Identity)";
        if (isActive.HasValue)
        {
            query += $" WHERE i.isActive = {isActive.Value.ToString().ToLower()}";
        }
        query += " RETURN i.id as id, i.name as name, i.type as type, i.isActive as isActive, i.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query);
            var identities = new List<Identity>();

            while (await cursor.FetchAsync())
            {
                identities.Add(HydrateIdentity(cursor.Current));
            }

            return Result.Success<IReadOnlyList<Identity>>(identities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving identities");
            return Result.Failure<IReadOnlyList<Identity>>($"Error retrieving identities: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<Identity>> CreateAsync(Identity identity, CancellationToken cancellationToken = default)
    {
        const string query = @"
            CREATE (i:Identity {
                id: $id,
                name: $name,
                type: $type,
                isActive: $isActive,
                createdAt: datetime($createdAt)
            })
            RETURN i.id as id, i.name as name, i.type as type,
                   i.isActive as isActive, i.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var parameters = new
            {
                id = identity.Id,
                name = identity.Name,
                type = identity.Type.ToString(),
                isActive = identity.IsActive,
                createdAt = identity.CreatedAt.ToString("o")
            };

            var cursor = await session.RunAsync(query, parameters);

            if (!await cursor.FetchAsync())
            {
                return Result.Failure<Identity>("Failed to create identity");
            }

            var createdIdentity = HydrateIdentity(cursor.Current);
            _logger.LogInformation("Identity created: {Id}", identity.Id);

            return Result.Success(createdIdentity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating identity with ID: {Id}", identity.Id);
            return Result.Failure<Identity>($"Error creating identity: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<Identity>> UpdateAsync(Identity identity, CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (i:Identity {id: $id})
            SET i.name = $name,
                i.isActive = $isActive
            RETURN i.id as id, i.name as name, i.type as type,
                   i.isActive as isActive, i.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var parameters = new
            {
                id = identity.Id,
                name = identity.Name,
                isActive = identity.IsActive
            };

            var cursor = await session.RunAsync(query, parameters);

            if (!await cursor.FetchAsync())
            {
                return Result.Failure<Identity>($"Identity with ID '{identity.Id}' not found");
            }

            var updatedIdentity = HydrateIdentity(cursor.Current);
            _logger.LogInformation("Identity updated: {Id}", identity.Id);

            return Result.Success(updatedIdentity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating identity with ID: {Id}", identity.Id);
            return Result.Failure<Identity>($"Error updating identity: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (i:Identity {id: $id})
            SET i.isActive = false
            RETURN i.id as id";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { id });

            if (!await cursor.FetchAsync())
            {
                return Result.Failure($"Identity with ID '{id}' not found");
            }

            _logger.LogInformation("Identity soft deleted: {Id}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting identity with ID: {Id}", id);
            return Result.Failure($"Error deleting identity: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<bool>> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = "MATCH (i:Identity {id: $id}) RETURN count(i) as count";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { id });

            if (!await cursor.FetchAsync())
            {
                return Result.Success(false);
            }

            var count = cursor.Current["count"].As<long>();
            return Result.Success(count > 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking identity existence with ID: {Id}", id);
            return Result.Failure<bool>($"Error checking identity existence: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <summary>
    /// Hydrates a domain Identity entity from Neo4j record
    /// This is the critical link between infrastructure and domain
    /// </summary>
    private Identity HydrateIdentity(IRecord record)
    {
        var id = record["id"].As<string>();
        var name = record["name"].As<string>();
        var typeString = record["type"].As<string>();
        var type = Enum.Parse<IdentityType>(typeString);

        // Use reflection to create entity with private constructor, then set properties
        var identity = new Identity(id, name, type);

        // Handle IsActive state
        var isActive = record["isActive"].As<bool>();
        if (!isActive)
        {
            identity.Deactivate();
        }

        return identity;
    }
}
