namespace DottoressaIorio.Services.Tests;

public class HtmlExtensionsTests
{
    [Fact]
    public void GetHtml_ShouldConvertTextToHtmlParagraphs()
    {
        // Arrange
        var text = "Line 1\nLine 2";

        // Act
        var htmlResult = StringExtensions.GetHtml(text);

        // Assert
        Assert.Contains("<p>Line 1</p>", htmlResult);
        Assert.Contains("<p>Line 2</p>", htmlResult);
    }

    [Fact]
    public void GetHtml_ShouldHandleEmptyString()
    {
        // Arrange
        var emptyText = "";

        // Act
        var htmlResult = StringExtensions.GetHtml(emptyText);

        // Assert
        Assert.Equal("", htmlResult);
    }

    [Fact]
    public void GetHtml_ShouldHandleNullString()
    {
        // Arrange
        string nullText = null;

        // Act
        var htmlResult = StringExtensions.GetHtml(nullText);

        // Assert
        Assert.Equal("", htmlResult);
    }
}





 