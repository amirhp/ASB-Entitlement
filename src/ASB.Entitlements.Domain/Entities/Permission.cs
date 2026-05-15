using ASB.Entitlements.Domain.Common;

namespace ASB.Entitlements.Domain.Entities;

/// <summary>
/// Represents an Authorization entitlement/permission
/// BIAN: Party Authorization - Defines what actions can be performed
/// </summary>
public class Permission : Entity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Action { get; private set; }
    public PermissionScope Scope { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Permission() : base()
    {
        Name = string.Empty;
        Description = string.Empty;
        Action = string.Empty;
    }

    public Permission(string id, string name, string action, string description, PermissionScope scope) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Permission action cannot be null or empty", nameof(action));

        Name = name;
        Action = action;
        Description = description ?? string.Empty;
        Scope = scope;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void UpdateDetails(string name, string action, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Permission action cannot be null or empty", nameof(action));

        Name = name;
        Action = action;
        Description = description ?? string.Empty;
    }
}

public enum PermissionScope
{
    Read,
    Write,
    Execute,
    Delete,
    Admin
}
