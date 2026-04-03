using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.RealTime;

/// <summary>
/// Merkezi SignalR hub — tüm modüller bu hub üzerinden istemcilere bildirim gönderir.
/// Kullanıcılar entity/modül bazlı gruplara katılarak sadece ilgilendikleri
/// değişiklikleri alırlar.
/// </summary>
/// <remarks>
/// Grup adlandırma konvansiyonu: "{EntityType}:{EntityId}" veya "{Module}:{Channel}"
/// Örn: "Order:550e8400-e29b-41d4-a716-446655440000" veya "IAM:UserChanges"
/// </remarks>
[Authorize]
public sealed class EntAppHub : Hub
{
    private readonly IUserConnectionTracker _connectionTracker;
    private readonly ILogger<EntAppHub> _logger;

    public EntAppHub(
        IUserConnectionTracker connectionTracker,
        ILogger<EntAppHub> logger)
    {
        _connectionTracker = connectionTracker;
        _logger = logger;
    }

    /// <summary>Bağlantı kurulduğunda — connection tracking.</summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
        {
            await _connectionTracker.AddConnectionAsync(userId, Context.ConnectionId);
            _logger.LogDebug("[SignalR] User {UserId} connected: {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>Bağlantı koptuğunda — connection tracking temizliği.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
        {
            await _connectionTracker.RemoveConnectionAsync(userId, Context.ConnectionId);
            _logger.LogDebug("[SignalR] User {UserId} disconnected: {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// İstemci bir entity/kanal grubuna katılır.
    /// Örn: "Order:guid" grubuna katılarak o siparişin güncellemelerini alır.
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("[SignalR] {ConnectionId} joined group: {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>İstemci bir gruptan ayrılır.</summary>
    public async Task LeaveGroup(string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("[SignalR] {ConnectionId} left group: {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>Kullanıcı ID'sini JWT claim'den okur.</summary>
    private Guid GetUserId()
    {
        var sub = Context.User?.FindFirst("sub")?.Value
                  ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
