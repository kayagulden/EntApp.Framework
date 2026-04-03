using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.RealTime;

/// <summary>
/// Entity değişiklik bildirim servisi.
/// Domain event handler'lar veya UoW sonrası bu servisi çağırarak
/// ilgili gruptaki istemcilere push bildirim gönderir.
/// </summary>
public interface IEntityChangeNotifier
{
    /// <summary>
    /// Entity oluşturulduğunda ilgili gruba bildirim gönderir.
    /// </summary>
    Task NotifyCreatedAsync<TEntity>(string entityId, TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Entity güncellendiğinde ilgili gruba bildirim gönderir.
    /// </summary>
    Task NotifyUpdatedAsync<TEntity>(string entityId, TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Entity silindiğinde ilgili gruba bildirim gönderir.
    /// </summary>
    Task NotifyDeletedAsync(string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir gruba özel mesaj gönderir.
    /// </summary>
    Task NotifyGroupAsync(string groupName, string method, object payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// SignalR üzerinden entity değişiklik bildirimi gönderen implementasyon.
/// </summary>
public sealed class EntityChangeNotifier : IEntityChangeNotifier
{
    private readonly IHubContext<EntAppHub> _hubContext;
    private readonly ILogger<EntityChangeNotifier> _logger;

    public EntityChangeNotifier(
        IHubContext<EntAppHub> hubContext,
        ILogger<EntityChangeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyCreatedAsync<TEntity>(
        string entityId,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(entity);

        var entityType = typeof(TEntity).Name;
        var groupName = $"{entityType}:list"; // Liste izleyenler

        var payload = new EntityChangePayload(entityType, entityId, ChangeType.Created, entity);

        await _hubContext.Clients.Group(groupName)
            .SendAsync("EntityChanged", payload, cancellationToken);

        _logger.LogDebug("[SignalR:PUSH] {ChangeType} {EntityType}:{EntityId} → group:{Group}",
            ChangeType.Created, entityType, entityId, groupName);
    }

    public async Task NotifyUpdatedAsync<TEntity>(
        string entityId,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(entity);

        var entityType = typeof(TEntity).Name;
        var entityGroup = $"{entityType}:{entityId}"; // Spesifik entity izleyenler
        var listGroup = $"{entityType}:list";         // Liste izleyenler

        var payload = new EntityChangePayload(entityType, entityId, ChangeType.Updated, entity);

        // Hem entity grubuna hem liste grubuna bildirim
        await Task.WhenAll(
            _hubContext.Clients.Group(entityGroup)
                .SendAsync("EntityChanged", payload, cancellationToken),
            _hubContext.Clients.Group(listGroup)
                .SendAsync("EntityChanged", payload, cancellationToken));

        _logger.LogDebug("[SignalR:PUSH] {ChangeType} {EntityType}:{EntityId}",
            ChangeType.Updated, entityType, entityId);
    }

    public async Task NotifyDeletedAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        var entityGroup = $"{entityType}:{entityId}";
        var listGroup = $"{entityType}:list";

        var payload = new EntityChangePayload(entityType, entityId, ChangeType.Deleted, null);

        await Task.WhenAll(
            _hubContext.Clients.Group(entityGroup)
                .SendAsync("EntityChanged", payload, cancellationToken),
            _hubContext.Clients.Group(listGroup)
                .SendAsync("EntityChanged", payload, cancellationToken));

        _logger.LogDebug("[SignalR:PUSH] {ChangeType} {EntityType}:{EntityId}",
            ChangeType.Deleted, entityType, entityId);
    }

    public async Task NotifyGroupAsync(
        string groupName,
        string method,
        object payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentNullException.ThrowIfNull(payload);

        await _hubContext.Clients.Group(groupName)
            .SendAsync(method, payload, cancellationToken);

        _logger.LogDebug("[SignalR:PUSH] {Method} → group:{Group}", method, groupName);
    }
}

/// <summary>Entity değişiklik tipi.</summary>
public enum ChangeType
{
    Created,
    Updated,
    Deleted
}

/// <summary>
/// İstemciye gönderilen entity değişiklik payload'u.
/// </summary>
/// <param name="EntityType">Entity tipi (ör: "Order")</param>
/// <param name="EntityId">Entity kimliği</param>
/// <param name="ChangeType">Değişiklik tipi</param>
/// <param name="Data">Entity verisi (Created/Updated için dolu, Deleted için null)</param>
public sealed record EntityChangePayload(
    string EntityType,
    string EntityId,
    ChangeType ChangeType,
    object? Data);
