using AssignmentSubmissionSystem.Application.Common.Paging;

namespace AssignmentSubmissionSystem.UnitTests.Common;

public sealed class PageQueryTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    public void NormalizedPage_ClampsToFirstPage_WhenBelowOne(int requested, int expected)
    {
        var query = new PageQuery { Page = requested };

        query.NormalizedPage.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, PageQuery.DefaultPageSize)]
    [InlineData(-1, PageQuery.DefaultPageSize)]
    [InlineData(50, 50)]
    [InlineData(100_000, PageQuery.MaxPageSize)]
    public void NormalizedPageSize_ClampsToConfiguredBounds(int requested, int expected)
    {
        var query = new PageQuery { PageSize = requested };

        query.NormalizedPageSize.Should().Be(expected);
    }

    [Fact]
    public void Skip_CountsWholePagesBeforeTheRequestedOne()
    {
        var query = new PageQuery { Page = 3, PageSize = 25 };

        query.Skip.Should().Be(50);
    }

    [Fact]
    public void Skip_IsZero_ForOutOfRangePage()
    {
        var query = new PageQuery { Page = -2, PageSize = 25 };

        query.Skip.Should().Be(0);
    }
}

public sealed class PagedResultTests
{
    [Fact]
    public void Map_ProjectsItems_AndPreservesPageTotals()
    {
        var page = new PagedResult<int>(new[] { 1, 2, 3 }, Total: 42, Page: 2, PageSize: 3);

        var mapped = page.Map(i => i.ToString());

        mapped.Items.Should().Equal("1", "2", "3");
        mapped.Total.Should().Be(42);
        mapped.Page.Should().Be(2);
        mapped.PageSize.Should().Be(3);
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    public void TotalPages_RoundsUpPartialPages(int total, int pageSize, int expected)
    {
        var page = new PagedResult<int>(Array.Empty<int>(), total, Page: 1, PageSize: pageSize);

        page.TotalPages.Should().Be(expected);
    }

    [Fact]
    public void ToMeta_ExposesEveryFieldAClientNeedsToPaginate()
    {
        var page = new PagedResult<int>(new[] { 1 }, Total: 5, Page: 2, PageSize: 2);

        page.ToMeta().Should().Be(new PageMeta(Total: 5, Page: 2, PageSize: 2, TotalPages: 3));
    }
}
