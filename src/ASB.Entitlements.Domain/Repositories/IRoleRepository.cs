using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;

namespace ASB.Entitlements.Domain.Repositories;

/// <summary>
/// Repository interface for Role aggregate root operations
/// </summary>
public interface IRoleRepository
{
    Task<Result<Role>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Role>>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<Result<Role>> CreateAsync(Role role, CancellationToken cancellationToken = default);

    Task<Result<Role>> UpdateAsync(Role role, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<Result<bool>> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
