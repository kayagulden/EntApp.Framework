using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EntApp.Shared.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges interceptor — AggregateRoot'lardaki domain event'leri dispatch eder.
/// İki aşamalı dispatch:
///   1. Pre-commit: IDomainEvent — aynı transaction içinde (SavingChanges)
///   2. Post-commit: IPostCommitDomainEvent — transaction sonrasında (SavedChanges)
/// </summary>
public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;
    private List<IPostCommitDomainEvent> _postCommitEvents = [];

    public DomainEventDispatchInterceptor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null)
        {
            await DispatchPreCommitEvents(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchPostCommitEvents(cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Pre-commit: IDomainEvent'leri aynı transaction içinde dispatch et.
    /// Sonra post-commit event'leri topla ve temizle.
    /// </summary>
    private async Task DispatchPreCommitEvents(DbContext context, CancellationToken cancellationToken)
    {
        var allEvents = CollectDomainEvents(context);

        // Post-commit event'leri ayır ve sakla
        _postCommitEvents = allEvents
            .OfType<IPostCommitDomainEvent>()
            .ToList();

        // Pre-commit event'leri dispatch et
        var preCommitEvents = allEvents
            .Where(e => e is not IPostCommitDomainEvent)
            .ToList();

        foreach (var domainEvent in preCommitEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }

    /// <summary>
    /// Post-commit: IPostCommitDomainEvent'leri transaction sonrasında dispatch et.
    /// Email, bildirim, cache invalidation gibi yan etkiler burada çalışır.
    /// </summary>
    private async Task DispatchPostCommitEvents(CancellationToken cancellationToken)
    {
        foreach (var domainEvent in _postCommitEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        _postCommitEvents = [];
    }

    /// <summary>
    /// ChangeTracker'dan AggregateRoot entity'lerini bulur,
    /// domain event'lerini toplar ve event listelerini temizler.
    /// </summary>
    private static List<IDomainEvent> CollectDomainEvents(DbContext context)
    {
        var events = new List<IDomainEvent>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var entityType = entry.Entity.GetType();
            if (!IsAggregateRoot(entityType))
            {
                continue;
            }

            // AggregateRoot<TId>.DomainEvents property'sine reflection ile eriş
            var domainEventsProperty = entityType.GetProperty("DomainEvents");
            if (domainEventsProperty?.GetValue(entry.Entity) is IReadOnlyList<IDomainEvent> domainEvents)
            {
                events.AddRange(domainEvents);
            }

            // ClearDomainEvents() çağır
            var clearMethod = entityType.GetMethod("ClearDomainEvents");
            clearMethod?.Invoke(entry.Entity, null);
        }

        return events;
    }

    private static bool IsAggregateRoot(Type? type)
    {
        while (type is not null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }
}
