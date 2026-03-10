namespace DottoressaIorio.Services.Tests;

/// <summary>
/// Contains unit tests for the StringExtensions class, specifically testing HTML conversion functionality.
/// </summary>
public class HtmlExtensionsTests
{
    /// <summary>
    /// Tests that the GetHtml method correctly converts a multi-line text into HTML paragraphs.
    /// </summary>
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

    /// <summary>
    /// Tests that the GetHtml method returns an empty string when given an empty input.
    /// </summary>
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

    /// <summary>
    /// Tests that the GetHtml method returns an empty string when given a null input.
    /// </summary>
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

    /// <summary>
    /// Tests that the GetHtml method correctly converts a single line of text into an HTML paragraph.
    /// </summary>
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

    /// <summary>
    /// Tests that the GetHtml method correctly handles multiple empty lines by converting them to paragraph breaks.
    /// </summary>
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

    /// <summary>
    /// Tests that the GetHtml method correctly handles a text containing only empty lines.
    /// </summary>
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

    /// <summary>
    /// Tests that the GetHtml method correctly handles a trailing newline character.
    /// </summary>
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

    /// <summary>
    /// Tests that the GetHtml method correctly handles a leading newline character.
    /// </summary>
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