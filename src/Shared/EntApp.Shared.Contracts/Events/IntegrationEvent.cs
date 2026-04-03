namespace EntApp.Shared.Contracts.Events;

/// <summary>
/// IntegrationEvent base record.
/// Concrete integration event'ler bu record'dan türer.
/// </summary>
/// <example>
/// <code>
/// public sealed record OrderCompletedIntegrationEvent(
///     Guid OrderId,
///     decimal TotalAmount)
///     : IntegrationEvent;
/// </code>
/// </example>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public Guid IdempotencyKey { get; init; } = Guid.NewGuid();
}
