namespace EntApp.Shared.Contracts.Caching;

/// <summary>
/// Cache invalidation marker interface'i.
/// Bu interface'i implement eden Command'lar çalıştıktan sonra
/// belirtilen cache key'leri invalidate edilir.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>Invalidate edilecek cache key'leri.</summary>
    IReadOnlyList<string> CacheKeysToInvalidate { get; }
}
