namespace EntApp.Shared.Kernel.Domain.ValueObjects;

/// <summary>
/// Value Object base sınıfı.
/// Equality, GetEqualityComponents pattern ile sağlanır.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Eşitlik karşılaştırmasında kullanılacak bileşenler.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null || GetType() != other.GetType())
        {
            return false;
        }

        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
        => obj is ValueObject other && Equals(other);

    public override int GetHashCode()
        => GetEqualityComponents()
            .Aggregate(0, (hash, component)
                => HashCode.Combine(hash, component?.GetHashCode() ?? 0));

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !Equals(left, right);
}
