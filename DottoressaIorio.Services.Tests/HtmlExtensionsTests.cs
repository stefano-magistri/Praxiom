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

    [Fact]
    public void GetHtml_ShouldHandleSingleLineText()
    {
        // Arrange
        var singleLineText = "Single line";

        // Act
        var htmlResult = StringExtensions.GetHtml(singleLineText);

        // Assert
        Assert.Equal("<p>Single line</p>", htmlResult);
    }

    [Fact]
    public void GetHtml_ShouldHandleMultipleEmptyLines()
    {
        // Arrange
        var textWithEmptyLines = "Line 1\n\nLine 3";

        // Act
        var htmlResult = StringExtensions.GetHtml(textWithEmptyLines);

        // Assert
        Assert.Equal("<p>Line 1</p><p><br /></p><p>Line 3</p>", htmlResult);
    }

    [Fact]
    public void GetHtml_ShouldHandleOnlyEmptyLines()
    {
        // Arrange
        var onlyEmptyLines = "\n\n";

        // Act
        var htmlResult = StringExtensions.GetHtml(onlyEmptyLines);

        // Assert
        Assert.Equal("<p><br /></p><p><br /></p><p><br /></p>", htmlResult);
    }

    [Fact]
    public void GetHtml_ShouldHandleTrailingNewLine()
    {
        // Arrange
        var trailingNewLine = "Line 1\nLine 2\n";

        // Act
        var htmlResult = StringExtensions.GetHtml(trailingNewLine);

        // Assert
        Assert.Equal("<p>Line 1</p><p>Line 2</p><p><br /></p>", htmlResult);
    }

    [Fact]
    public void GetHtml_ShouldHandleLeadingNewLine()
    {
        // Arrange
        var leadingNewLine = "\nLine 1\nLine 2";

        // Act
        var htmlResult = StringExtensions.GetHtml(leadingNewLine);

        // Assert
        Assert.Equal("<p><br /></p><p>Line 1</p><p>Line 2</p>", htmlResult);
    }
}