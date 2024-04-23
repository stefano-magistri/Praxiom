using Bunit;
using DottoressaIorio.App.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace DottoressaIorio.App.Repository.Tests.Pages;

public class PaginationTests : TestContext
{
    [Fact]
    public async Task PaginationComponent_ShouldRenderCorrectly()
    {
        // Arrange
        const int currentPage = 2;
        const int totalPages = 5;
        var previousPageInvoked = false;
        var nextPageInvoked = false;

        var cut = RenderComponent<Pagination>(
            ("CurrentPage", currentPage),
            ("TotalPages", totalPages),
            ("SelectedPage", EventCallback.Factory.Create<int>(this, _ => { })),
            ("PreviousPage", new Func<Task>(() =>
            {
                previousPageInvoked = true;
                return Task.CompletedTask;
            })),
            ("NextPage", new Func<Task>(() =>
            {
                nextPageInvoked = true;
                return Task.CompletedTask;
            }))
        );

        // Act
        var pageItems = cut.FindAll("li.page-item");

        // Assert
        Assert.Equal(totalPages + 2, pageItems.Count); // Total pages + Previous and Next links

        var previousLink = cut.Find("li.page-item:first-child a.page-link");
        var nextLink = cut.Find("li.page-item:last-child a.page-link");
        Assert.Equal("«", previousLink.TextContent); // Previous link text
        Assert.Equal("»", nextLink.TextContent); // Next link text

        // Click on the previous link and verify if PreviousPage function is invoked
        await previousLink.ClickAsync(new MouseEventArgs());
        Assert.True(previousPageInvoked);

        // Click on the next link and verify if NextPage function is invoked
        await nextLink.ClickAsync(new MouseEventArgs());
        Assert.True(nextPageInvoked);
    }
}