using EntApp.Shared.Kernel.Specifications;
using FluentAssertions;
using Xunit;

namespace EntApp.Shared.Kernel.Tests.Specifications;

// Test entity
public class TestProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

// Concrete specification
public class ActiveProductsSpec : Specification<TestProduct>
{
    public ActiveProductsSpec()
    {
        AddCriteria(p => p.IsActive);
        AddOrderBy(p => p.Name);
    }
}

public class ExpensiveActiveProductsSpec : Specification<TestProduct>
{
    public ExpensiveActiveProductsSpec(decimal minPrice, int skip, int take)
    {
        AddCriteria(p => p.IsActive && p.Price >= minPrice);
        AddOrderByDescending(p => p.Price);
        ApplyPaging(skip, take);
    }
}

public class SpecificationTests
{
    private static IQueryable<TestProduct> GetTestProducts()
    {
        return new List<TestProduct>
        {
            new() { Id = 1, Name = "Laptop", Price = 15000m, IsActive = true },
            new() { Id = 2, Name = "Mouse", Price = 200m, IsActive = true },
            new() { Id = 3, Name = "Keyboard", Price = 500m, IsActive = false },
            new() { Id = 4, Name = "Monitor", Price = 8000m, IsActive = true },
            new() { Id = 5, Name = "Headset", Price = 1500m, IsActive = true },
        }.AsQueryable();
    }

    [Fact]
    public void Specification_ShouldApplyCriteria()
    {
        var spec = new ActiveProductsSpec();
        var query = GetTestProducts();

        var result = SpecificationEvaluator.GetQuery(query, spec).ToList();

        result.Should().HaveCount(4); // Keyboard is inactive
        result.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
    }

    [Fact]
    public void Specification_ShouldApplyOrdering()
    {
        var spec = new ActiveProductsSpec();
        var query = GetTestProducts();

        var result = SpecificationEvaluator.GetQuery(query, spec).ToList();

        result[0].Name.Should().Be("Headset");
        result[1].Name.Should().Be("Laptop");
    }

    [Fact]
    public void Specification_WithPaging_ShouldApplyTakeAndSkip()
    {
        var spec = new ExpensiveActiveProductsSpec(minPrice: 100m, skip: 1, take: 2);
        var query = GetTestProducts();

        var result = SpecificationEvaluator.GetQuery(query, spec).ToList();

        result.Should().HaveCount(2);
        // Ordered by price desc: Laptop(15000), Monitor(8000), Headset(1500), Mouse(200)
        // Skip 1, take 2 → Monitor, Headset
        result[0].Name.Should().Be("Monitor");
        result[1].Name.Should().Be("Headset");
    }

    [Fact]
    public void Specification_Properties_ShouldReflectBuilderCalls()
    {
        var spec = new ExpensiveActiveProductsSpec(1000m, 0, 10);

        spec.Criteria.Should().NotBeNull();
        spec.OrderByDescending.Should().NotBeNull();
        spec.OrderBy.Should().BeNull();
        spec.IsPagingEnabled.Should().BeTrue();
        spec.Skip.Should().Be(0);
        spec.Take.Should().Be(10);
    }
}
