using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;

namespace ASB.Entitlements.Domain.Repositories;

/// <summary>
/// Repository interface for Identity aggregate root operations
/// </summary>
public interface IIdentityRepository
{
    /// <summary>
    /// Gets an identity by its unique identifier
    /// </summary>
    Task<Result<Identity>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all identities with optional filtering
    /// </summary>
    Task<Result<IReadOnlyList<Identity>>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new identity
    /// </summary>
    Task<Result<Identity>> CreateAsync(Identity identity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing identity
    /// </summary>
    Task<Result<Identity>> UpdateAsync(Identity identity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an identity (soft delete by deactivating)
    /// </summary>
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an identity exists
    /// </summary>
    Task<Result<bool>> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
