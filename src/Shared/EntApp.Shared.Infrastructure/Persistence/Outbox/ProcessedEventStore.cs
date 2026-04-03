using Microsoft.EntityFrameworkCore;

namespace EntApp.Shared.Infrastructure.Persistence.Outbox;

/// <summary>
/// İşlenmiş event store — consumer tarafında idempotency.
/// Event handler'larında kullanılır.
/// </summary>
public sealed class ProcessedEventStore
{
    private readonly DbContext _dbContext;

    public ProcessedEventStore(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Bu IdempotencyKey ile event daha önce işlenmiş mi?
    /// </summary>
    public async Task<bool> IsProcessedAsync(Guid idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ProcessedEvent>()
            .AnyAsync(e => e.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    /// <summary>
    /// Event'i işlenmiş olarak işaretle.
    /// </summary>
    public async Task MarkAsProcessedAsync(Guid idempotencyKey, string eventType, CancellationToken cancellationToken = default)
    {
        var processedEvent = new ProcessedEvent
        {
            IdempotencyKey = idempotencyKey,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow
        };

        await _dbContext.Set<ProcessedEvent>().AddAsync(processedEvent, cancellationToken);
    }
}
