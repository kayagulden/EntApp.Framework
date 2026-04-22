using EntApp.Shared.Kernel.Domain.Events;

namespace EntApp.Modules.StateFlow.Domain.Events;

/// <summary>
/// State geçişi tamamlandığında yayınlanan domain event.
/// Diğer modüller bu event'i dinleyerek side effect'ler (SLA güncelleme, bildirim vb.) tetikleyebilir.
/// </summary>
public sealed record StateTransitionCompletedEvent(
    string EntityType,
    Guid EntityId,
    string FromState,
    string ToState,
    string Trigger,
    Guid FlowDefinitionId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
