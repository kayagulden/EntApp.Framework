namespace EntApp.Shared.Contracts.Caching;

/// <summary>
/// Cache'lenebilir query marker interface'i.
/// CachingBehavior bu interface'i gören query'lerin sonuçlarını cache'ler.
/// </summary>
/// <example>
/// <code>
/// public sealed record GetProductByIdQuery(Guid Id) : IRequest&lt;Result&lt;ProductDto&gt;&gt;, ICacheableQuery
/// {
///     public string CacheKey => $"product:{Id}";
///     public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
/// }
/// </code>
/// </example>
public interface ICacheableQuery
{
    /// <summary>Cache anahtarı.</summary>
    string CacheKey { get; }

    /// <summary>Cache süresi. Null ise varsayılan süre kullanılır.</summary>
    TimeSpan? Expiration { get; }
}
