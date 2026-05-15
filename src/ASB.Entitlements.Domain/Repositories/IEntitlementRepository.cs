using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.ValueObjects;

namespace ASB.Entitlements.Domain.Repositories;

/// <summary>
/// Repository interface for entitlement operations
/// </summary>
public interface IEntitlementRepository
{
    /// <summary>
    /// Checks if an identity is entitled to perform an action on a resource
    /// </summary>
    Task<Result<EntitlementCheckResult>> CheckEntitlementAsync(
        string identityId,
        string resourceId,
        string action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions granted to an identity for a specific resource
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetIdentityPermissionsAsync(
        string identityId,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles assigned to an identity
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetIdentityRolesAsync(
        string identityId,
        CancellationToken cancellationToken = default);
}
