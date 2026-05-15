namespace ASB.Entitlements.Domain.ValueObjects;

/// <summary>
/// Value object representing the result of an entitlement check
/// </summary>
public sealed class EntitlementCheckResult
{
    public bool IsEntitled { get; }
    public string Reason { get; }
    public string? GrantedPermission { get; }
    public DateTime CheckedAt { get; }

    private EntitlementCheckResult(bool isEntitled, string reason, string? grantedPermission)
    {
        IsEntitled = isEntitled;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        GrantedPermission = grantedPermission;
        CheckedAt = DateTime.UtcNow;
    }

    public static EntitlementCheckResult Granted(string permissionName, string reason)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name cannot be null or empty", nameof(permissionName));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

        return new EntitlementCheckResult(true, reason, permissionName);
    }

    public static EntitlementCheckResult Denied(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

        return new EntitlementCheckResult(false, reason, null);
    }
}
