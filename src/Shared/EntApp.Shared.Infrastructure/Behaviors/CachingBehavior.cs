using System.Text.Json;
using EntApp.Shared.Contracts.Caching;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior — ICacheableQuery marker ile cache yönetimi.
/// Önce cache'de arar, yoksa handler'ı çalıştırır ve sonucu cache'ler.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        // ICacheableQuery değilse doğrudan handler'a git
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next();
        }

        var cacheKey = cacheableQuery.CacheKey;

        // Cache'de var mı?
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cachedData is not null)
        {
            try
            {
                var cached = JsonSerializer.Deserialize<TResponse>(cachedData);
                if (cached is not null)
                {
                    _logger.LogDebug("[CACHE:HIT] {CacheKey}", cacheKey);
                    return cached;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[CACHE:DESERIALIZE_ERROR] {CacheKey} — falling back to handler.", cacheKey);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogWarning(ex, "[CACHE:DESERIALIZE_ERROR] {CacheKey} — type not supported, falling back to handler.", cacheKey);
            }
        }

        // Cache'de yok — handler'ı çalıştır
        _logger.LogDebug("[CACHE:MISS] {CacheKey}", cacheKey);
        var response = await next();

        // Sonucu cache'le
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheableQuery.Expiration ?? DefaultExpiration
        };

        var serialized = JsonSerializer.Serialize(response);
        await _cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);

        _logger.LogDebug("[CACHE:SET] {CacheKey} (TTL: {Ttl})",
            cacheKey, options.AbsoluteExpirationRelativeToNow);

        return response;
    }
}
