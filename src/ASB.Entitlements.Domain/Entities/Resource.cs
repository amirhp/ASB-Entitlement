using ASB.Entitlements.Domain.Common;

namespace ASB.Entitlements.Domain.Entities;

/// <summary>
/// Represents a resource (object/asset) that can be accessed
/// BIAN: Product/Service - The object being accessed or protected
/// </summary>
public class Resource : Entity
{
    public string Name { get; private set; }
    public string Type { get; private set; }
    public string Description { get; private set; }
    public ResourceClassification Classification { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Resource() : base()
    {
        Name = string.Empty;
        Type = string.Empty;
        Description = string.Empty;
    }

    public Resource(string id, string name, string type, string description, ResourceClassification classification) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Resource name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Resource type cannot be null or empty", nameof(type));

        Name = name;
        Type = type;
        Description = description ?? string.Empty;
        Classification = classification;
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

    public void UpdateDetails(string name, string type, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Resource name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Resource type cannot be null or empty", nameof(type));

        Name = name;
        Type = type;
        Description = description ?? string.Empty;
    }
}

public enum ResourceClassification
{
    Public,
    Internal,
    Confidential,
    Restricted
}
