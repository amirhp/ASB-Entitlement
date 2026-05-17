using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;
using ASB.Entitlements.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace ASB.Entitlements.Infrastructure.Persistence.Repositories;

public sealed class ResourceRepository : IResourceRepository
{
    private readonly INeo4jContext _context;
    private readonly ILogger<ResourceRepository> _logger;

    public ResourceRepository(INeo4jContext context, ILogger<ResourceRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Resource>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (r:Resource {id: $id})
            RETURN r.id as id, r.name as name, r.type as type, r.description as description,
                   r.classification as classification, r.isActive as isActive, r.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { id });
            if (!await cursor.FetchAsync())
                return Result.Failure<Resource>($"Resource with ID '{id}' not found");

            var resource = HydrateResource(cursor.Current);
            return Result.Success(resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resource with ID: {Id}", id);
            return Result.Failure<Resource>($"Error retrieving resource: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<IReadOnlyList<Resource>>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = "MATCH (r:Resource)";
        if (isActive.HasValue)
            query += $" WHERE r.isActive = {isActive.Value.ToString().ToLower()}";
        query += " RETURN r.id as id, r.name as name, r.type as type, r.description as description, r.classification as classification, r.isActive as isActive, r.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query);
            var resources = new List<Resource>();
            while (await cursor.FetchAsync())
                resources.Add(HydrateResource(cursor.Current));
            return Result.Success<IReadOnlyList<Resource>>(resources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resources");
            return Result.Failure<IReadOnlyList<Resource>>($"Error retrieving resources: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<Resource>> CreateAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        const string query = @"
            CREATE (r:Resource {
                id: $id, name: $name, type: $type, description: $description,
                classification: $classification, isActive: $isActive, createdAt: datetime($createdAt)
            })
            RETURN r.id as id, r.name as name, r.type as type, r.description as description,
                   r.classification as classification, r.isActive as isActive, r.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var parameters = new
            {
                id = resource.Id,
                name = resource.Name,
                type = resource.Type,
                description = resource.Description,
                classification = resource.Classification.ToString(),
                isActive = resource.IsActive,
                createdAt = resource.CreatedAt.ToString("o")
            };

            var cursor = await session.RunAsync(query, parameters);
            if (!await cursor.FetchAsync())
                return Result.Failure<Resource>("Failed to create resource");

            var createdResource = HydrateResource(cursor.Current);
            _logger.LogInformation("Resource created: {Id}", resource.Id);
            return Result.Success(createdResource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating resource with ID: {Id}", resource.Id);
            return Result.Failure<Resource>($"Error creating resource: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<Resource>> UpdateAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        const string query = @"
            MATCH (r:Resource {id: $id})
            SET r.name = $name, r.description = $description, r.classification = $classification, r.isActive = $isActive
            RETURN r.id as id, r.name as name, r.type as type, r.description as description,
                   r.classification as classification, r.isActive as isActive, r.createdAt as createdAt";

        var session = _context.GetSession();
        try
        {
            var parameters = new
            {
                id = resource.Id,
                name = resource.Name,
                description = resource.Description,
                classification = resource.Classification.ToString(),
                isActive = resource.IsActive
            };

            var cursor = await session.RunAsync(query, parameters);
            if (!await cursor.FetchAsync())
                return Result.Failure<Resource>($"Resource with ID '{resource.Id}' not found");

            var updatedResource = HydrateResource(cursor.Current);
            _logger.LogInformation("Resource updated: {Id}", resource.Id);
            return Result.Success(updatedResource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating resource with ID: {Id}", resource.Id);
            return Result.Failure<Resource>($"Error updating resource: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = "MATCH (r:Resource {id: $id}) SET r.isActive = false RETURN r.id as id";
        var session = _context.GetSession();
        try
        {
            var cursor = await session.RunAsync(query, new { id });
            if (!await cursor.FetchAsync())
                return Result.Failure($"Resource with ID '{id}' not found");

            _logger.LogInformation("Resource soft deleted: {Id}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting resource with ID: {Id}", id);
            return Result.Failure($"Error deleting resource: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<Result<bool>> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        const string query = "MATCH (r:Resource {id: $id}) RETURN count(r) as count";
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
            _logger.LogError(ex, "Error checking resource existence with ID: {Id}", id);
            return Result.Failure<bool>($"Error checking resource existence: {ex.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private Resource HydrateResource(IRecord record)
    {
        var id = record["id"].As<string>();
        var name = record["name"].As<string>();
        var type = record["type"].As<string>();
        var description = record["description"].As<string>();
        var classificationString = record["classification"].As<string>();
        var classification = Enum.Parse<ResourceClassification>(classificationString);

        var resource = new Resource(id, name, type, description, classification);
        var isActive = record["isActive"].As<bool>();
        if (!isActive) resource.Deactivate();

        return resource;
    }
}
