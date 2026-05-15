namespace ASB.Entitlements.Domain.Common;

/// <summary>
/// Base class for all domain entities
/// </summary>
public abstract class Entity
{
    public string Id { get; protected set; } = string.Empty;

    protected Entity() { }

    protected Entity(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(id));

        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity? a, Entity? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(Entity? a, Entity? b)
    {
        return !(a == b);
    }
}
