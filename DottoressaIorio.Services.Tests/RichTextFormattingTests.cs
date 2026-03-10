namespace DottoressaIorio.Services.Tests;

public class RichTextFormattingTests
{
    [Fact]
    public void ToSupportedHtml_ShouldConvertPlainTextToParagraphs()
    {
        var html = RichTextFormatting.ToSupportedHtml("Linea 1\nLinea 2");

        Assert.Equal("<p>Linea 1</p><p>Linea 2</p>", html);
    }

    [Fact]
    public void ToSupportedHtml_ShouldPreserveSupportedMarkup()
    {
        var html = RichTextFormatting.ToSupportedHtml("<p><strong>Mattina</strong></p><ul><li><em>Acqua</em></li></ul>");

        Assert.Equal("<p><strong>Mattina</strong></p><ul><li><em>Acqua</em></li></ul>", html);
    }

    [Fact]
    public void ToSupportedHtml_ShouldStripUnsupportedMarkup()
    {
        var html = RichTextFormatting.ToSupportedHtml(
            "<p>Test<script>alert('x')</script><a href='https://example.test'>link</a><img src='x' /></p>");

        Assert.Equal("<p>Testlink</p>", html);
    }

    [Fact]
    public void NormalizeStoredValue_ShouldKeepPlainTextRecordsCompatible()
    {
        var normalized = RichTextFormatting.NormalizeStoredValue("Terapia esistente");

        Assert.Equal("Terapia esistente", normalized);
    }

    [Fact]
    public void ToPlainText_ShouldDecodeSupportedHtmlForListsAndParagraphs()
    {
        var text = RichTextFormatting.ToPlainText("<p>Prima</p><ol><li>Uno</li><li>Due</li></ol>");

        Assert.Equal("Prima\nUno\nDue", text);
    }

    [Fact]
    public void NormalizeStoredValue_ShouldBeIdempotentForPlainText()
    {
        const string original = "Terapia esistente";

        var once = RichTextFormatting.NormalizeStoredValue(original);
        var twice = RichTextFormatting.NormalizeStoredValue(once);

        Assert.Equal(original, once);
        Assert.Equal(once, twice);
    }

    [Fact]
    public void NormalizeStoredValue_ShouldNotDoubleEncodeHtmlEntities()
    {
        const string original = "<p>test0&amp;nbsp;test0</p>";

        var normalized = RichTextFormatting.NormalizeStoredValue(original);

        Assert.Equal("<p>test0 test0</p>", normalized);
    }

    [Fact]
    public void NormalizeStoredValue_ShouldBeIdempotentForEditorHtml()
    {
        const string original = "<p><strong>Mattina</strong></p><ul><li>Acqua</li></ul>";

        var once = RichTextFormatting.NormalizeStoredValue(original);
        var twice = RichTextFormatting.NormalizeStoredValue(once);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void ToPlainText_ShouldStripTagsAndDecodeNestedEntities()
    {
        var text = RichTextFormatting.ToPlainText("<p>test0&amp;amp;nbsp;test0</p>");

        Assert.Equal("test0 test0", text);
    }

    [Fact]
    public void ToSupportedHtml_ShouldKeepExistingPlainTextValuesReadableInPdfPath()
    {
        var html = RichTextFormatting.ToSupportedHtml("Legacy&nbsp;text");

        Assert.Equal("<p>Legacy text</p>", html);
    }

    [Fact]
    public void ToSupportedHtml_ShouldPreserveSpacesBetweenInlineElements()
    {
        var html = RichTextFormatting.ToSupportedHtml("<p><strong>Mattina</strong> <em>sera</em></p>");

        Assert.Equal("<p><strong>Mattina</strong> <em>sera</em></p>", html);
    }

    [Fact]
    public void ToPlainText_ShouldPreserveSpacesBetweenInlineElements()
    {
        var text = RichTextFormatting.ToPlainText("<p><strong>Mattina</strong> <em>sera</em></p>");

        Assert.Equal("Mattina sera", text);
    }
}
