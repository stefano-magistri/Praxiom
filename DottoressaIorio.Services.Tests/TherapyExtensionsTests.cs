using DottoressaIorio.Models;

namespace DottoressaIorio.Services.Tests;

public class TherapyExtensionsTests
{
    [Fact]
    public void GetHtmlDescription_ShouldConvertToHtmlParagraphs()
    {
        // Arrange
        var therapy = new Therapy
        {
            Description = "Line 1\nLine 2\nLine 3"
        };

        // Act
        var htmlDescription = therapy.GetHtmlDescription();

        // Assert
        Assert.Contains("<p>Line 1</p>", htmlDescription);
        Assert.Contains("<p>Line 2</p>", htmlDescription);
        Assert.Contains("<p>Line 3</p>", htmlDescription);
    }
}