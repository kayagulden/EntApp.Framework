using System.Text.Json;
using EntApp.Shared.Contracts.Events;
using EntApp.Shared.Contracts.Messaging;
using EntApp.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Messaging;

/// <summary>
/// Outbox-based event bus — production ortamı için.
/// Event'leri doğrudan publish etmez; Outbox tablosuna yazar.
/// OutboxProcessor background service ile asenkron olarak publish edilir.
/// İleride MassTransit + RabbitMQ transport eklenecektir.
/// </summary>
public sealed class OutboxEventBus<TDbContext> : IEventBus
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<OutboxEventBus<TDbContext>> _logger;

    public OutboxEventBus(TDbContext dbContext, ILogger<OutboxEventBus<TDbContext>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var outboxMessage = new OutboxMessage
        {
            Id = @event.Id,
            Type = @event.GetType().AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(@event, @event.GetType()),
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Set<OutboxMessage>().AddAsync(outboxMessage, cancellationToken);

        _logger.LogDebug("Integration event {EventType} ({EventId}) written to outbox.",
            @event.GetType().Name, @event.Id);
    }
}
