using EntApp.Shared.Contracts.Messaging;

namespace EntApp.Modules.MyModule.Application.IntegrationEvents;

/// <summary>
/// Modüller arası integration event tanımları.
/// Diğer modüllerin dinleyebileceği event'leri burada tanımlayın.
/// </summary>

// Örnek: Yeni kayıt oluşturulduğunda yayınlanan event
// public sealed record SampleEntityCreatedEvent(Guid EntityId, string Name) : IntegrationEvent;

// Örnek: Durum değiştiğinde yayınlanan event
// public sealed record SampleEntityStatusChangedEvent(Guid EntityId, string OldStatus, string NewStatus) : IntegrationEvent;
