using Microsoft.AspNetCore.Components;

namespace DottoressaIorio.Shared.Components;

public class PaginationBase : ComponentBase
{
    [Parameter] public int CurrentPage { get; set; }
    [Parameter] public int TotalPages { get; set; }
    [Parameter] public EventCallback<int> SelectedPage { get; set; }
    [Parameter] public EventCallback PreviousPage { get; set; }
    [Parameter] public EventCallback NextPage { get; set; }

    protected IReadOnlyList<PageItem> PageItems => PaginationBuilder.BuildPagination(CurrentPage, TotalPages);
    protected bool IsPreviousDisabled => CurrentPage <= 1 || TotalPages <= 1;
    protected bool IsNextDisabled => CurrentPage >= TotalPages || TotalPages <= 1;

    protected Task SelectPageAsync(int page)
    {
        return SelectedPage.InvokeAsync(page);
    }

    protected Task PreviousPageAsync()
    {
        if (IsPreviousDisabled || !PreviousPage.HasDelegate) return Task.CompletedTask;

        return PreviousPage.InvokeAsync();
    }

    protected Task NextPageAsync()
    {
        if (IsNextDisabled || !NextPage.HasDelegate) return Task.CompletedTask;

        return NextPage.InvokeAsync();
    }
}