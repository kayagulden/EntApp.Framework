using System.Text;

namespace EntApp.Shared.Infrastructure.Caching;

/// <summary>
/// Type-safe cache key oluşturucu.
/// Tutarlı, çakışmayan cache key'leri garanti eder.
/// </summary>
/// <example>
/// <code>
/// var key = CacheKeyBuilder.Build("product", productId);
/// // → "product:3fa85f64-5717-4562-b3fc-2c963f66afa6"
///
/// var listKey = CacheKeyBuilder.Build("products", "active", "page:1");
/// // → "products:active:page:1"
/// </code>
/// </example>
public static class CacheKeyBuilder
{
    private const char Separator = ':';

    /// <summary>
    /// Verilen parçalardan cache key oluşturur.
    /// </summary>
    public static string Build(params string[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        if (parts.Length == 0)
        {
            throw new ArgumentException("At least one key part is required.", nameof(parts));
        }

        return string.Join(Separator, parts);
    }

    /// <summary>
    /// Entity tipi ve ID ile cache key oluşturur.
    /// </summary>
    public static string Build<TEntity>(Guid id) where TEntity : class
    {
        return $"{typeof(TEntity).Name.ToLowerInvariant()}{Separator}{id}";
    }

    /// <summary>
    /// Entity tipi ile liste cache key oluşturur.
    /// </summary>
    public static string BuildList<TEntity>(params string[] suffix) where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(suffix);
        var sb = new StringBuilder(typeof(TEntity).Name.ToLowerInvariant());
        sb.Append(Separator).Append("list");

        foreach (var s in suffix)
        {
            sb.Append(Separator).Append(s);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Prefix pattern oluşturur (ör: RemoveByPrefix için).
    /// </summary>
    public static string BuildPrefix<TEntity>() where TEntity : class
    {
        return $"{typeof(TEntity).Name.ToLowerInvariant()}{Separator}";
    }
}
