namespace EntApp.Shared.Kernel.Domain;

/// <summary>
/// Tüm entity'lerin base sınıfı.
/// Ortak alanlar: Id, CreatedAt, UpdatedAt, IsDeleted, RowVersion.
/// </summary>
public abstract class BaseEntity<TId> where TId : struct
{
    public TId Id { get; protected set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    /// <summary>
    /// Optimistic concurrency token.
    /// EF Core tarafında PostgreSQL xmin system column ile eşleştirilir.
    /// </summary>
    public uint RowVersion { get; set; }

    protected BaseEntity() { }

    protected BaseEntity(TId id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
        => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(BaseEntity<TId>? left, BaseEntity<TId>? right)
        => Equals(left, right);

    public static bool operator !=(BaseEntity<TId>? left, BaseEntity<TId>? right)
        => !Equals(left, right);
}
