using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;

namespace ASB.Entitlements.Domain.Repositories;

/// <summary>
/// Repository interface for Resource aggregate root operations
/// </summary>
public interface IResourceRepository
{
    Task<Result<Resource>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Resource>>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<Result<Resource>> CreateAsync(Resource resource, CancellationToken cancellationToken = default);

    Task<Result<Resource>> UpdateAsync(Resource resource, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<Result<bool>> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
