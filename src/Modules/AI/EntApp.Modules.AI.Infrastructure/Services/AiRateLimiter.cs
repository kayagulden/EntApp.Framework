using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.AI.Infrastructure.Services;

/// <summary>
/// AI çağrıları için rate limiting — tenant/modül bazlı.
/// In-memory sliding window (Redis'e taşınabilir).
/// </summary>
public sealed class AiRateLimiter
{
    private readonly ConcurrentDictionary<string, SlidingWindow> _windows = new();
    private readonly int _maxRequestsPerMinute;
    private readonly int _maxTokensPerMinute;
    private readonly ILogger<AiRateLimiter> _logger;

    public AiRateLimiter(IConfiguration configuration, ILogger<AiRateLimiter> logger)
    {
        _logger = logger;
        var section = configuration.GetSection("AiSettings:RateLimiting");
        _maxRequestsPerMinute = section.GetValue("MaxRequestsPerMinute", 60);
        _maxTokensPerMinute = section.GetValue("MaxTokensPerMinute", 100_000);
    }

    /// <summary>Rate limit kontrolü. Aşıldıysa false döner.</summary>
    public bool TryAcquire(Guid tenantId, string? moduleName = null)
    {
        var key = $"{tenantId}:{moduleName ?? "global"}";
        var window = _windows.GetOrAdd(key, _ => new SlidingWindow());
        window.CleanExpired();

        if (window.RequestCount >= _maxRequestsPerMinute)
        {
            _logger.LogWarning("[AI:RateLimit] Request limit exceeded for {Key}: {Count}/{Max}",
                key, window.RequestCount, _maxRequestsPerMinute);
            return false;
        }

        if (window.TokenCount >= _maxTokensPerMinute)
        {
            _logger.LogWarning("[AI:RateLimit] Token limit exceeded for {Key}: {Tokens}/{Max}",
                key, window.TokenCount, _maxTokensPerMinute);
            return false;
        }

        window.AddRequest();
        return true;
    }

    /// <summary>Tüketilen token'ları kaydet.</summary>
    public void RecordTokens(Guid tenantId, int tokenCount, string? moduleName = null)
    {
        var key = $"{tenantId}:{moduleName ?? "global"}";
        var window = _windows.GetOrAdd(key, _ => new SlidingWindow());
        window.AddTokens(tokenCount);
    }

    /// <summary>Mevcut kullanım durumunu döndür.</summary>
    public RateLimitStatus GetStatus(Guid tenantId, string? moduleName = null)
    {
        var key = $"{tenantId}:{moduleName ?? "global"}";
        if (_windows.TryGetValue(key, out var window))
        {
            window.CleanExpired();
            return new RateLimitStatus(
                window.RequestCount, _maxRequestsPerMinute,
                window.TokenCount, _maxTokensPerMinute);
        }

        return new RateLimitStatus(0, _maxRequestsPerMinute, 0, _maxTokensPerMinute);
    }

    private sealed class SlidingWindow
    {
        private readonly ConcurrentQueue<DateTime> _requests = new();
        private int _tokenCount;

        public int RequestCount => _requests.Count;
        public int TokenCount => _tokenCount;

        public void AddRequest() => _requests.Enqueue(DateTime.UtcNow);

        public void AddTokens(int count) => Interlocked.Add(ref _tokenCount, count);

        public void CleanExpired()
        {
            var threshold = DateTime.UtcNow.AddMinutes(-1);
            while (_requests.TryPeek(out var oldest) && oldest < threshold)
            {
                _requests.TryDequeue(out _);
            }

            // Token sayacını da her dakika sıfırla
            if (_requests.IsEmpty)
            {
                Interlocked.Exchange(ref _tokenCount, 0);
            }
        }
    }
}

/// <summary>Rate limit durumu.</summary>
public sealed record RateLimitStatus(
    int RequestsUsed,
    int RequestsLimit,
    int TokensUsed,
    int TokensLimit);
