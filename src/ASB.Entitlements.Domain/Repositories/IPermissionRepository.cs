using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;

namespace ASB.Entitlements.Domain.Repositories;

/// <summary>
/// Repository interface for Permission aggregate root operations
/// </summary>
public interface IPermissionRepository
{
    Task<Result<Permission>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Permission>>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<Result<Permission>> CreateAsync(Permission permission, CancellationToken cancellationToken = default);

    Task<Result<Permission>> UpdateAsync(Permission permission, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<Result<bool>> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
