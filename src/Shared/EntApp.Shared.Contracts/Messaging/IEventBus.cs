using EntApp.Shared.Contracts.Events;

namespace EntApp.Shared.Contracts.Messaging;

/// <summary>
/// Event bus abstraction — integration event'leri publish eder.
/// RabbitMQ (MassTransit) veya InMemory olarak implement edilir.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Integration event'i publish eder.
    /// Production'da Outbox üzerinden gider, test'te doğrudan işlenir.
    /// </summary>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent;
}
