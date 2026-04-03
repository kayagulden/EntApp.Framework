using System.Text.Json;
using EntApp.Shared.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Persistence.Outbox;

/// <summary>
/// Background service: Outbox tablosundan işlenmemiş mesajları alır,
/// IEventBus üzerinden publish eder.
/// At-least-once delivery garantisi sağlar.
/// </summary>
public sealed class OutboxProcessor<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor<TDbContext>> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;
    private const int MaxRetryCount = 3;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor<{DbContext}> started.", typeof(TDbContext).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages.");
            }
#pragma warning restore CA1031

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxProcessor<{DbContext}> stopped.", typeof(TDbContext).Name);
    }

    private async Task ProcessOutboxMessages(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<Contracts.Messaging.IEventBus>();

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetryCount)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type);
                if (eventType is null)
                {
                    _logger.LogWarning("Could not resolve type: {Type}", message.Type);
                    message.Error = $"Type not found: {message.Type}";
                    message.RetryCount = MaxRetryCount; // Artık deneme
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Content, eventType);
                if (@event is null)
                {
                    _logger.LogWarning("Could not deserialize outbox message {Id}", message.Id);
                    message.Error = "Deserialization failed";
                    message.RetryCount = MaxRetryCount;
                    continue;
                }

                // IEventBus.PublishAsync generic — reflection ile çağır
                var publishMethod = typeof(Contracts.Messaging.IEventBus)
                    .GetMethod(nameof(Contracts.Messaging.IEventBus.PublishAsync))!
                    .MakeGenericMethod(eventType);

                var task = (Task?)publishMethod.Invoke(eventBus, [@event, cancellationToken]);
                if (task is not null)
                {
                    await task;
                }

                message.ProcessedAt = DateTime.UtcNow;
                _logger.LogDebug("Outbox message {Id} processed successfully.", message.Id);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogWarning(ex, "Failed to process outbox message {Id}. Retry {Retry}/{Max}",
                    message.Id, message.RetryCount, MaxRetryCount);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
