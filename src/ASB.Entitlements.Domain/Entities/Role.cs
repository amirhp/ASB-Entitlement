using ASB.Entitlements.Domain.Common;

namespace ASB.Entitlements.Domain.Entities;

/// <summary>
/// Represents a Party Role as per BIAN standards
/// BIAN: Party Role - A role played by a party in a specific context
/// </summary>
public class Role : Entity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public RoleType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Role() : base()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    public Role(string id, string name, string description, RoleType type) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or empty", nameof(name));

        Name = name;
        Description = description ?? string.Empty;
        Type = type;
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

    public void UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or empty", nameof(name));

        Name = name;
        Description = description ?? string.Empty;
    }
}

public enum RoleType
{
    SystemDefined,
    Custom,
    Temporary
}
