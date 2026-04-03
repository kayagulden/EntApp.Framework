namespace EntApp.Shared.Kernel.Specifications;

/// <summary>
/// IQueryable üzerine Specification uygular.
/// EF Core sorgularında kullanılır.
/// </summary>
public static class SpecificationEvaluator
{
    /// <summary>
    /// Verilen specification'ı IQueryable'a uygular.
    /// </summary>
    public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(specification);

        var query = inputQuery;

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (specification.IsPagingEnabled)
        {
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }
        }

        // Note: Expression-based includes are applied here.
        // String-based includes require EF Core and will be handled in Shared.Infrastructure.
        query = specification.Includes
            .Aggregate(query, (current, include) => current); // Placeholder — EF Core'da .Include() ile değiştirilecek

        return query;
    }
}
