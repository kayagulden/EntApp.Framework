using EntApp.Shared.Infrastructure.Caching;
using FluentAssertions;
using Xunit;

namespace EntApp.Shared.Infrastructure.Tests.Caching;

public class TestEntity { }
public class TestProduct { }

public class CacheKeyBuilderTests
{
    [Fact]
    public void Build_SinglePart_ShouldReturnDirectly()
    {
        var key = CacheKeyBuilder.Build("products");

        key.Should().Be("products");
    }

    [Fact]
    public void Build_MultipleParts_ShouldJoinWithColon()
    {
        var key = CacheKeyBuilder.Build("products", "active", "page:1");

        key.Should().Be("products:active:page:1");
    }

    [Fact]
    public void Build_EmptyParts_ShouldThrow()
    {
        var act = () => CacheKeyBuilder.Build();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Build_NullParts_ShouldThrow()
    {
        var act = () => CacheKeyBuilder.Build(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildGeneric_ShouldUseTypeNameAndId()
    {
        var id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

        var key = CacheKeyBuilder.Build<TestProduct>(id);

        key.Should().Be("testproduct:3fa85f64-5717-4562-b3fc-2c963f66afa6");
    }

    [Fact]
    public void BuildList_ShouldIncludeListSuffix()
    {
        var key = CacheKeyBuilder.BuildList<TestProduct>("active");

        key.Should().Be("testproduct:list:active");
    }

    [Fact]
    public void BuildList_NoSuffix_ShouldReturnBaseList()
    {
        var key = CacheKeyBuilder.BuildList<TestEntity>();

        key.Should().Be("testentity:list");
    }

    [Fact]
    public void BuildPrefix_ShouldEndWithColon()
    {
        var prefix = CacheKeyBuilder.BuildPrefix<TestProduct>();

        prefix.Should().Be("testproduct:");
    }
}
