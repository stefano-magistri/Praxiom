using DottoressaIorio.Models;

namespace DottoressaIorio.Services.Tests;

/// <summary>
/// Contains unit tests for the TherapyExtensions class.
/// </summary>
public class TherapyExtensionsTests
{
    /// <summary>
    /// Tests that the GetHtmlDescription method correctly converts a therapy description with multiple lines into HTML paragraphs.
    /// </summary>
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