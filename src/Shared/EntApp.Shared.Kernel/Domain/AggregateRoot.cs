using EntApp.Shared.Kernel.Domain.Events;

namespace EntApp.Shared.Kernel.Domain;

/// <summary>
/// DDD Aggregate Root. Domain event'lerini takip eder.
/// Her aggregate root aynı zamanda auditable bir entity'dir.
/// </summary>
public abstract class AggregateRoot<TId> : AuditableEntity<TId> where TId : struct
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Bu aggregate üzerinde oluşan domain event'leri.
    /// SaveChanges öncesinde dispatch edilir.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }

    /// <summary>
    /// Yeni bir domain event ekler. Handler'lar SaveChanges sırasında çalıştırılır.
    /// </summary>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Dispatch sonrası event listesini temizler.
    /// Genellikle infrastructure tarafından çağrılır.
    /// </summary>
    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
