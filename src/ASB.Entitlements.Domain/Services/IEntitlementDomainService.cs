using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;

namespace ASB.Entitlements.Domain.Services;

/// <summary>
/// Domain service for complex entitlement operations spanning multiple aggregates
/// </summary>
public interface IEntitlementDomainService
{
    /// <summary>
    /// Assigns a role to an identity
    /// </summary>
    Task<Result> AssignRoleToIdentityAsync(
        string identityId,
        string roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from an identity
    /// </summary>
    Task<Result> RemoveRoleFromIdentityAsync(
        string identityId,
        string roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants a permission to a role for a specific resource
    /// </summary>
    Task<Result> GrantPermissionToRoleAsync(
        string roleId,
        string permissionId,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a permission from a role
    /// </summary>
    Task<Result> RevokePermissionFromRoleAsync(
        string roleId,
        string permissionId,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles assigned to an identity
    /// </summary>
    Task<Result<IReadOnlyList<Role>>> GetIdentityRolesAsync(
        string identityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions granted to an identity for a specific resource
    /// </summary>
    Task<Result<IReadOnlyList<Permission>>> GetIdentityPermissionsForResourceAsync(
        string identityId,
        string resourceId,
        CancellationToken cancellationToken = default);
}
