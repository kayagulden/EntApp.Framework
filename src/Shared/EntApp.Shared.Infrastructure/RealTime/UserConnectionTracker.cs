using System.Collections.Concurrent;

namespace EntApp.Shared.Infrastructure.RealTime;

/// <summary>
/// Kullanıcı bağlantı takibi kontratı.
/// Hangi kullanıcının hangi bağlantılarla aktif olduğunu izler.
/// </summary>
public interface IUserConnectionTracker
{
    /// <summary>Yeni bağlantı ekler.</summary>
    Task AddConnectionAsync(Guid userId, string connectionId);

    /// <summary>Bağlantı kaldırır.</summary>
    Task RemoveConnectionAsync(Guid userId, string connectionId);

    /// <summary>Kullanıcının tüm aktif bağlantılarını döner.</summary>
    Task<IReadOnlyList<string>> GetConnectionsAsync(Guid userId);

    /// <summary>Kullanıcının en az bir aktif bağlantısı var mı?</summary>
    Task<bool> IsOnlineAsync(Guid userId);

    /// <summary>Tüm çevrimiçi kullanıcı ID'lerini döner.</summary>
    Task<IReadOnlyList<Guid>> GetOnlineUsersAsync();
}

/// <summary>
/// In-memory kullanıcı bağlantı takip implementasyonu.
/// Tek sunucu senaryoları için uygundur.
/// Çoklu sunucu (scale-out) için Redis Backplane ile birlikte kullanılmalıdır.
/// </summary>
public sealed class InMemoryUserConnectionTracker : IUserConnectionTracker
{
    /// <summary>
    /// UserId → ConnectionId set mapping.
    /// Thread-safe ConcurrentDictionary + HashSet.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, HashSet<string>> Connections = new();
    private static readonly object Lock = new();

    public Task AddConnectionAsync(Guid userId, string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (Lock)
        {
            var connections = Connections.GetOrAdd(userId, _ => []);
            connections.Add(connectionId);
        }

        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(Guid userId, string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (Lock)
        {
            if (Connections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);

                if (connections.Count == 0)
                {
                    Connections.TryRemove(userId, out _);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetConnectionsAsync(Guid userId)
    {
        lock (Lock)
        {
            if (Connections.TryGetValue(userId, out var connections))
            {
                return Task.FromResult<IReadOnlyList<string>>(connections.ToList().AsReadOnly());
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    public Task<bool> IsOnlineAsync(Guid userId)
    {
        lock (Lock)
        {
            return Task.FromResult(
                Connections.TryGetValue(userId, out var connections) && connections.Count > 0);
        }
    }

    public Task<IReadOnlyList<Guid>> GetOnlineUsersAsync()
    {
        lock (Lock)
        {
            var onlineUsers = Connections
                .Where(kvp => kvp.Value.Count > 0)
                .Select(kvp => kvp.Key)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<Guid>>(onlineUsers);
        }
    }
}
