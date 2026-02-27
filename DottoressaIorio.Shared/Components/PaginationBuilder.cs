namespace DottoressaIorio.Shared.Components;

public sealed record PageItem(int? PageNumber, bool IsEllipsis);

public static class PaginationBuilder
{
    public static IReadOnlyList<PageItem> BuildPagination(int current, int total, int window = 2, int edge = 1)
    {
        if (total <= 0) return Array.Empty<PageItem>();

        if (window < 0) window = 0;
        if (edge < 0) edge = 0;

        current = Math.Clamp(current, 1, total);
        var allPagesThreshold = window * 2 + edge * 2 + 1;
        if (total <= allPagesThreshold)
            return Enumerable.Range(1, total)
                .Select(page => new PageItem(page, false))
                .ToList();

        var visiblePages = new SortedSet<int>();

        for (var i = 1; i <= Math.Min(edge, total); i++) visiblePages.Add(i);

        var middleStart = Math.Max(current - window, edge + 1);
        var middleEnd = Math.Min(current + window, total - edge);
        for (var i = middleStart; i <= middleEnd; i++) visiblePages.Add(i);

        var lastEdgeStart = Math.Max(total - edge + 1, 1);
        for (var i = lastEdgeStart; i <= total; i++) visiblePages.Add(i);

        var items = new List<PageItem>();
        var previousPage = 0;
        foreach (var page in visiblePages)
        {
            if (previousPage != 0 && page - previousPage > 1) items.Add(new PageItem(null, true));

            items.Add(new PageItem(page, false));
            previousPage = page;
        }

        return items;
    }
}