using System.Linq.Expressions;

namespace EntApp.Shared.Kernel.Specifications;

/// <summary>
/// Specification Pattern base sınıfı.
/// Fluent builder API ile sorgu koşullarını tanımlar.
/// </summary>
/// <example>
/// <code>
/// public class ActiveCustomersInCitySpec : Specification&lt;Customer&gt;
/// {
///     public ActiveCustomersInCitySpec(string city)
///     {
///         AddCriteria(c => c.Status == Status.Active &amp;&amp; c.Address.City == city);
///         AddOrderBy(c => c.Name);
///         ApplyPaging(0, 20);
///     }
/// }
/// </code>
/// </example>
public abstract class Specification<T> : ISpecification<T> where T : class
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];

    public Expression<Func<T, bool>>? Criteria { get; private set; }

    public Expression<Func<T, object>>? OrderBy { get; private set; }

    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();

    public int? Take { get; private set; }

    public int? Skip { get; private set; }

    public bool IsPagingEnabled { get; private set; }

    protected void AddCriteria(Expression<Func<T, bool>> criteria)
        => Criteria = criteria;

    protected void AddOrderBy(Expression<Func<T, object>> orderByExpression)
        => OrderBy = orderByExpression;

    protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
        => OrderByDescending = orderByDescExpression;

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
        => _includes.Add(includeExpression);

    protected void AddInclude(string includeString)
        => _includeStrings.Add(includeString);

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}
