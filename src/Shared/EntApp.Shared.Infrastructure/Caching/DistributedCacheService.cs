using System.Text.Json;
using EntApp.Shared.Contracts.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Caching;

/// <summary>
/// IDistributedCache üzerinden cache service implementasyonu.
/// Redis veya InMemory distributed cache ile çalışır.
/// </summary>
public sealed class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var data = await _cache.GetStringAsync(key, cancellationToken);
        if (data is null)
        {
            return default;
        }

        _logger.LogDebug("[CACHE:GET] {Key}", key);
        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
        };

        var serialized = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serialized, options, cancellationToken);

        _logger.LogDebug("[CACHE:SET] {Key} (TTL: {Ttl})", key, options.AbsoluteExpirationRelativeToNow);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
        _logger.LogDebug("[CACHE:REMOVE] {Key}", key);
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // IDistributedCache prefix silme desteklemez.
        // Redis kullanıyorsanız IConnectionMultiplexer ile SCAN+DEL yapılmalı.
        // Bu implementasyon placeholder — Redis-specific implementasyon gerektiğinde override edilecek.
        _logger.LogWarning(
            "[CACHE:REMOVE_PREFIX] Prefix removal '{Prefix}' not supported by IDistributedCache. " +
            "Use Redis-specific implementation.", prefix);

        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var data = await _cache.GetStringAsync(key, cancellationToken);
        return data is not null;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, expiration, cancellationToken);

        return value;
    }
}
