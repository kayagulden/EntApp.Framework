using MediatR;

namespace EntApp.Shared.Contracts.Events;

/// <summary>
/// Modüller arası integration event kontratı.
/// Outbox tablosu üzerinden publish edilir.
/// At-least-once delivery — consumer tarafında IdempotencyKey ile deduplikasyon yapılır.
/// </summary>
public interface IIntegrationEvent : INotification
{
    /// <summary>Event'in benzersiz kimliği.</summary>
    Guid Id { get; }

    /// <summary>Event'in oluştuğu zaman (UTC).</summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Idempotency anahtarı — consumer tarafında aynı event'in
    /// birden fazla işlenmesini engellemek için kullanılır.
    /// </summary>
    Guid IdempotencyKey { get; }
}
