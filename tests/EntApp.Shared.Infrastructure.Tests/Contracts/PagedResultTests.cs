using EntApp.Shared.Contracts.Common;
using FluentAssertions;
using Xunit;

namespace EntApp.Shared.Infrastructure.Tests.Contracts;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        var result = new PagedResult<string>
        {
            Items = ["a", "b", "c"],
            TotalCount = 25,
            PageNumber = 1,
            PageSize = 10
        };

        result.TotalPages.Should().Be(3); // ceil(25/10)
    }

    [Fact]
    public void HasNextPage_ShouldBeTrue_WhenMorePagesExist()
    {
        var result = new PagedResult<int>
        {
            Items = [1, 2],
            TotalCount = 30,
            PageNumber = 1,
            PageSize = 10
        };

        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_ShouldBeTrue_WhenNotFirstPage()
    {
        var result = new PagedResult<int>
        {
            Items = [1, 2],
            TotalCount = 30,
            PageNumber = 2,
            PageSize = 10
        };

        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void LastPage_ShouldHaveNoNextPage()
    {
        var result = new PagedResult<int>
        {
            Items = [1],
            TotalCount = 21,
            PageNumber = 3,
            PageSize = 10
        };

        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Empty_ShouldReturnEmptyResult()
    {
        var result = PagedResult<string>.Empty();

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void TotalPages_WithZeroPageSize_ShouldReturnZero()
    {
        var result = new PagedResult<int>
        {
            Items = [],
            TotalCount = 10,
            PageNumber = 1,
            PageSize = 0
        };

        result.TotalPages.Should().Be(0);
    }
}

public class PagedRequestTests
{
    [Fact]
    public void Defaults_ShouldBeCorrect()
    {
        var request = new PagedRequest();

        request.PageNumber.Should().Be(1);
        request.PageSize.Should().Be(20);
        request.Skip.Should().Be(0);
        request.Take.Should().Be(20);
    }

    [Fact]
    public void Skip_ShouldCalculateCorrectly()
    {
        var request = new PagedRequest { PageNumber = 3, PageSize = 10 };

        request.Skip.Should().Be(20); // (3-1) * 10
        request.Take.Should().Be(10);
    }

    [Fact]
    public void Take_ShouldClampToMax100()
    {
        var request = new PagedRequest { PageSize = 500 };

        request.Take.Should().Be(100);
    }

    [Fact]
    public void Take_ShouldClampToMin1()
    {
        var request = new PagedRequest { PageSize = -5 };

        request.Take.Should().Be(1);
    }

    [Fact]
    public void Skip_WithNegativePage_ShouldUsePageOne()
    {
        var request = new PagedRequest { PageNumber = -1, PageSize = 10 };

        request.Skip.Should().Be(0); // Max(1, -1) = 1 → (1-1)*10 = 0
    }
}
