using System.Linq.Expressions;

namespace EntApp.Shared.Kernel.Specifications;

/// <summary>
/// Specification Pattern interface.
/// Sorgu mantığını handler'dan ayırarak tekrar kullanılabilir sorgular tanımlar.
/// </summary>
public interface ISpecification<T> where T : class
{
    /// <summary>Where koşulu.</summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>Artan sıralama.</summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>Azalan sıralama.</summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>Include ifadeleri (navigation property'ler).</summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>String bazlı include (ThenInclude desteği).</summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>Sayfalama — kaç kayıt alınacak.</summary>
    int? Take { get; }

    /// <summary>Sayfalama — kaç kayıt atlanacak.</summary>
    int? Skip { get; }

    /// <summary>Sayfalama aktif mi.</summary>
    bool IsPagingEnabled { get; }
}
