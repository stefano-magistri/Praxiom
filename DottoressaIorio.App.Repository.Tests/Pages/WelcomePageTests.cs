using Bunit;
using Index = DottoressaIorio.App.Pages.Index;

namespace DottoressaIorio.App.Repository.Tests.Pages;

public class WelcomePageTests : TestContext
{
    [Fact]
    public void ShouldRenderWelcomeMessage()
    {
        // Arrange
        var cut = RenderComponent<Index>();

        // Act - no action required for rendering

        // Assert
        cut.MarkupMatches(@"<h2>Benvenuta</h2>");
    }
}
