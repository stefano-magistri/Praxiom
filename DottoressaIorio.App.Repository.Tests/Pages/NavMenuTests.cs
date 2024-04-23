using Bunit;
using DottoressaIorio.App.Shared;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace DottoressaIorio.App.Repository.Tests.Pages;

public class NavMenuTests : TestContext
{
    [Fact]
    public async Task NavMenuComponent_ShouldRenderCorrectly()
    {
        // Arrange
        var jsRuntimeMock = new Mock<IJSRuntime>();

        jsRuntimeMock.Setup(js => js.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(true);

        Services.AddSingleton(jsRuntimeMock.Object);

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var navLinks = cut.FindAll(".nav-link");

        Assert.Equal(5, navLinks.Count); // Assuming 5 navigation links
    }
}