using MediatR;

namespace EntApp.Shared.Kernel.Domain.Events;

/// <summary>
/// Pre-commit domain event marker interface.
/// Aynı transaction içinde dispatch edilir (SaveChanges sırasında).
/// Kullanım: stok düşürme, bakiye güncelleme gibi aynı TX içinde olması gereken yan etkiler.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Event'in oluştuğu zaman (UTC).
    /// </summary>
    DateTime OccurredOn { get; }
}
