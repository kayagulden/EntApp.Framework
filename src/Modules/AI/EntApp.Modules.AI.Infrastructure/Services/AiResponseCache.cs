using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.AI.Infrastructure.Services;

/// <summary>
/// LLM response caching — aynı prompt için aynı yanıtı tekrar üretmemek.
/// IDistributedCache kullanır (Redis, InMemory vb.)
/// </summary>
public sealed class AiResponseCache
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<AiResponseCache> _logger;

    /// <summary>Cache TTL (dakika)</summary>
    private const int DefaultTtlMinutes = 60;

    public AiResponseCache(IDistributedCache cache, ILogger<AiResponseCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Cache'den yanıt al.</summary>
    public async Task<T?> GetAsync<T>(string prompt, string? modelName = null, CancellationToken ct = default)
        where T : class
    {
        var key = BuildKey(prompt, modelName);
        var cached = await _cache.GetStringAsync(key, ct);

        if (cached is null) return null;

        _logger.LogDebug("[AI:Cache] HIT — {Key}", key[..Math.Min(key.Length, 40)]);
        return JsonSerializer.Deserialize<T>(cached);
    }

    /// <summary>Yanıtı cache'e kaydet.</summary>
    public async Task SetAsync<T>(string prompt, T response, string? modelName = null,
        int ttlMinutes = DefaultTtlMinutes, CancellationToken ct = default)
        where T : class
    {
        var key = BuildKey(prompt, modelName);
        var json = JsonSerializer.Serialize(response);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttlMinutes)
        };

        await _cache.SetStringAsync(key, json, options, ct);
        _logger.LogDebug("[AI:Cache] SET — {Key}, TTL={Ttl}min", key[..Math.Min(key.Length, 40)], ttlMinutes);
    }

    /// <summary>Belirli bir prompt'un cache'ini sil.</summary>
    public async Task InvalidateAsync(string prompt, string? modelName = null, CancellationToken ct = default)
    {
        var key = BuildKey(prompt, modelName);
        await _cache.RemoveAsync(key, ct);
    }

    private static string BuildKey(string prompt, string? modelName)
    {
        var raw = $"ai:cache:{modelName ?? "default"}:{prompt}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
        return $"ai:cache:{hash}";
    }
}
