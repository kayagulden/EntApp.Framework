namespace EntApp.Shared.Contracts.Caching;

/// <summary>
/// Cache service abstraction.
/// Redis veya InMemory implementasyonu ile çalışır.
/// </summary>
public interface ICacheService
{
    /// <summary>Cache'den değer okur. Yoksa default döner.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Değeri cache'e yazar.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>Cache key'ini siler.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Belirtilen pattern ile eşleşen key'leri siler (ör: "product:*").</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>Cache key'i mevcut mu?</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache'de yoksa factory ile üretir ve cache'ler (Cache-Aside pattern).
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);
}
