using ASB.Entitlements.Domain.Common;

namespace ASB.Entitlements.Domain.Entities;

/// <summary>
/// Represents a Party (customer, user, or service) in the system
/// BIAN: Party Reference Data Management
/// </summary>
public class Identity : Entity
{
    public string Name { get; private set; }
    public IdentityType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Identity() : base()
    {
        Name = string.Empty;
    }

    public Identity(string id, string name, IdentityType type) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Identity name cannot be null or empty", nameof(name));

        Name = name;
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

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Identity name cannot be null or empty", nameof(name));

        Name = name;
    }
}

public enum IdentityType
{
    Customer,
    Employee,
    Service,
    System
}
