using DottoressaIorio.Shared.Components;

namespace DottoressaIorio.Services.Tests;

public class PaginationBuilderTests
{
    [Fact]
    public void BuildPagination_Total1_Current1_ReturnsSinglePage()
    {
        var items = PaginationBuilder.BuildPagination(1, 1);
        Assert.Equal(["1"], ToTokens(items));
    }

    [Fact]
    public void BuildPagination_Total5_Current3_ReturnsAllPagesWithoutEllipsis()
    {
        var items = PaginationBuilder.BuildPagination(3, 5);
        Assert.Equal(["1", "2", "3", "4", "5"], ToTokens(items));
    }

    [Fact]
    public void BuildPagination_Total21_Current1_ReturnsExpectedWindow()
    {
        var items = PaginationBuilder.BuildPagination(1, 21);
        Assert.Equal(["1", "2", "3", "...", "21"], ToTokens(items));
    }

    [Fact]
    public void BuildPagination_Total21_Current2_ReturnsExpectedWindow()
    {
        var items = PaginationBuilder.BuildPagination(2, 21);
        Assert.Equal(["1", "2", "3", "4", "...", "21"], ToTokens(items));
    }

    [Fact]
    public void BuildPagination_Total21_Current10_ReturnsExpectedWindow()
    {
        var items = PaginationBuilder.BuildPagination(10, 21);
        Assert.Equal(["1", "...", "8", "9", "10", "11", "12", "...", "21"], ToTokens(items));
    }

    [Fact]
    public void BuildPagination_Total21_Current20_ReturnsExpectedWindow()
    {
        var items = PaginationBuilder.BuildPagination(20, 21);
        Assert.Equal(["1", "...", "18", "19", "20", "21"], ToTokens(items));
    }

    [Fact]
    public void BuildPagination_Total21_Current21_ReturnsExpectedWindow()
    {
        var items = PaginationBuilder.BuildPagination(21, 21);
        Assert.Equal(["1", "...", "19", "20", "21"], ToTokens(items));
    }

    [Fact]
    public void BuildPagination_CurrentIsClampedBetweenOneAndTotal()
    {
        var low = PaginationBuilder.BuildPagination(-10, 21);
        var high = PaginationBuilder.BuildPagination(999, 21);

        Assert.Equal(["1", "2", "3", "...", "21"], ToTokens(low));
        Assert.Equal(["1", "...", "19", "20", "21"], ToTokens(high));
    }

    [Fact]
    public void BuildPagination_HasIncreasingUniquePages_AndEllipsisOnlyForRealGaps()
    {
        var items = PaginationBuilder.BuildPagination(10, 21);

        var pages = items.Where(x => !x.IsEllipsis).Select(x => x.PageNumber!.Value).ToList();
        Assert.Equal(pages.Count, pages.Distinct().Count());
        for (var i = 1; i < pages.Count; i++)
        {
            Assert.True(pages[i] > pages[i - 1]);
        }

        for (var i = 1; i < items.Count - 1; i++)
        {
            if (!items[i].IsEllipsis)
            {
                continue;
            }

            var left = items[i - 1].PageNumber!.Value;
            var right = items[i + 1].PageNumber!.Value;
            Assert.True(right - left > 1);
        }
    }

    private static string[] ToTokens(IReadOnlyList<PageItem> items)
    {
        return items
            .Select(x => x.IsEllipsis ? "..." : x.PageNumber!.Value.ToString())
            .ToArray();
    }
}
