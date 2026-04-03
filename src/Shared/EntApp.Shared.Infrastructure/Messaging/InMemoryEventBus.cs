using EntApp.Shared.Contracts.Events;
using EntApp.Shared.Contracts.Messaging;
using MediatR;

namespace EntApp.Shared.Infrastructure.Messaging;

/// <summary>
/// InMemory event bus — development ve test ortamları için.
/// Integration event'leri doğrudan MediatR üzerinden publish eder.
/// Production'da RabbitMqEventBus ile değiştirilir.
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly IMediator _mediator;

    public InMemoryEventBus(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        await _mediator.Publish(@event, cancellationToken);
    }
}
