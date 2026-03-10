using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DottoressaIorio.Services;

public static class RichTextFormatting
{
    private static readonly Regex HtmlTagRegex = new("<[a-z!/][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BreakRegex = new(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ClosingParagraphRegex = new(@"</p\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ClosingListItemRegex = new(@"</li\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ClosingListRegex = new(@"</(ul|ol)\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private const int MaxDecodePasses = 3;

    public static string ToSupportedHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return LooksLikeHtml(value)
            ? SanitizeHtml(value)
            : ConvertPlainTextToHtml(value);
    }

    public static string NormalizeStoredValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return LooksLikeHtml(value)
            ? SanitizeHtml(value)
            : value.Trim();
    }

    public static string ToEditorValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return LooksLikeHtml(value)
            ? SanitizeHtml(value)
            : DecodeTextContent(value);
    }

    public static string ToPlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (!LooksLikeHtml(value))
        {
            return DecodeTextContent(value);
        }

        var supportedHtml = ToSupportedHtml(value);
        var withBreaks = BreakRegex.Replace(supportedHtml, "\n");
        withBreaks = ClosingParagraphRegex.Replace(withBreaks, "\n");
        withBreaks = ClosingListItemRegex.Replace(withBreaks, "\n");
        withBreaks = ClosingListRegex.Replace(withBreaks, "\n");

        var withoutTags = Regex.Replace(withBreaks, "<[^>]+>", string.Empty, RegexOptions.Singleline);
        var decoded = WebUtility.HtmlDecode(withoutTags);

        return decoded
            .Replace('\u00A0', ' ')
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static bool LooksLikeHtml(string value)
    {
        return HtmlTagRegex.IsMatch(value);
    }

    private static string ConvertPlainTextToHtml(string value)
    {
        var paragraphs = value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => string.IsNullOrEmpty(line)
                ? "<p><br /></p>"
                : $"<p>{WebUtility.HtmlEncode(DecodeTextContent(line))}</p>");

        return string.Join(string.Empty, paragraphs);
    }

    private static string SanitizeHtml(string value)
    {
        var document = new HtmlDocument();
        document.LoadHtml($"<root>{value}</root>");

        var root = document.DocumentNode.SelectSingleNode("//root");
        if (root == null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        AppendBlockNodes(root.ChildNodes, builder);
        return builder.ToString();
    }

    private static void AppendBlockNodes(HtmlNodeCollection nodes, StringBuilder builder)
    {
        var inlineBuffer = new StringBuilder();

        foreach (var node in nodes)
        {
            if (TryAppendInline(node, inlineBuffer))
            {
                continue;
            }

            FlushInlineBuffer(builder, inlineBuffer);
            AppendBlockNode(node, builder);
        }

        FlushInlineBuffer(builder, inlineBuffer);
    }

    private static void AppendBlockNode(HtmlNode node, StringBuilder builder)
    {
        if (IsIgnorable(node))
        {
            return;
        }

        switch (node.Name.ToLowerInvariant())
        {
            case "#text":
                AppendInlineParagraph(node.InnerText, builder);
                break;
            case "p":
                builder.Append("<p>")
                    .Append(RenderInlineNodes(node.ChildNodes))
                    .Append("</p>");
                break;
            case "br":
                builder.Append("<p><br /></p>");
                break;
            case "ul":
            case "ol":
                AppendList(node, builder);
                break;
            case "div":
            case "section":
            case "article":
            case "span":
            case "body":
            case "html":
            case "root":
                AppendBlockNodes(node.ChildNodes, builder);
                break;
            case "strong":
            case "b":
            case "em":
            case "i":
                builder.Append("<p>")
                    .Append(RenderInlineNode(node))
                    .Append("</p>");
                break;
            case "script":
            case "style":
                break;
            default:
                AppendBlockNodes(node.ChildNodes, builder);
                break;
        }
    }

    private static void AppendList(HtmlNode node, StringBuilder builder)
    {
        var listTag = node.Name.Equals("ol", StringComparison.OrdinalIgnoreCase) ? "ol" : "ul";
        var items = node.ChildNodes
            .Where(child => child.Name.Equals("li", StringComparison.OrdinalIgnoreCase))
            .Select(RenderListItem)
            .Where(item => !string.IsNullOrEmpty(item))
            .ToList();

        if (items.Count == 0)
        {
            return;
        }

        builder.Append('<').Append(listTag).Append('>');
        foreach (var item in items)
        {
            builder.Append(item);
        }

        builder.Append("</").Append(listTag).Append('>');
    }

    private static string RenderListItem(HtmlNode node)
    {
        var content = RenderInlineNodes(node.ChildNodes);
        if (string.IsNullOrWhiteSpace(HtmlEntity.DeEntitize(content).Replace("<br />", string.Empty, StringComparison.Ordinal)))
        {
            content = "<br />";
        }

        return $"<li>{content}</li>";
    }

    private static bool TryAppendInline(HtmlNode node, StringBuilder inlineBuffer)
    {
        if (IsIgnorable(node))
        {
            return true;
        }

        switch (node.Name.ToLowerInvariant())
        {
            case "#text":
                inlineBuffer.Append(WebUtility.HtmlEncode(DecodeTextContent(node.InnerText)));
                return true;
            case "br":
                inlineBuffer.Append("<br />");
                return true;
            case "strong":
            case "b":
            case "em":
            case "i":
            case "span":
                inlineBuffer.Append(RenderInlineNode(node));
                return true;
            default:
                return false;
        }
    }

    private static string RenderInlineNodes(HtmlNodeCollection nodes)
    {
        var builder = new StringBuilder();

        foreach (var node in nodes)
        {
            if (IsIgnorable(node))
            {
                continue;
            }

            builder.Append(RenderInlineNode(node));
        }

        return builder.Length == 0 ? "<br />" : builder.ToString();
    }

    private static string RenderInlineNode(HtmlNode node)
    {
        switch (node.Name.ToLowerInvariant())
        {
            case "#text":
                return WebUtility.HtmlEncode(DecodeTextContent(node.InnerText));
            case "br":
                return "<br />";
            case "strong":
            case "b":
                return $"<strong>{RenderInlineNodes(node.ChildNodes)}</strong>";
            case "em":
            case "i":
                return $"<em>{RenderInlineNodes(node.ChildNodes)}</em>";
            case "span":
                return RenderInlineNodes(node.ChildNodes);
            case "script":
            case "style":
                return string.Empty;
            default:
                if (!node.HasChildNodes)
                {
                    return string.Empty;
                }

                return RenderInlineNodes(node.ChildNodes);
        }
    }

    private static void AppendInlineParagraph(string text, StringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        builder.Append("<p>")
            .Append(WebUtility.HtmlEncode(DecodeTextContent(text)))
            .Append("</p>");
    }

    private static void FlushInlineBuffer(StringBuilder builder, StringBuilder inlineBuffer)
    {
        if (inlineBuffer.Length == 0)
        {
            return;
        }

        var content = inlineBuffer.ToString();
        if (string.IsNullOrWhiteSpace(HtmlEntity.DeEntitize(content).Replace("<br />", string.Empty, StringComparison.Ordinal)))
        {
            content = "<br />";
        }

        builder.Append("<p>")
            .Append(content)
            .Append("</p>");
        inlineBuffer.Clear();
    }

    private static bool IsIgnorable(HtmlNode node)
    {
        return node.NodeType == HtmlNodeType.Comment
               || node.NodeType == HtmlNodeType.Document
               || string.IsNullOrWhiteSpace(node.InnerHtml) && node.Name.Equals("#text", StringComparison.OrdinalIgnoreCase);
    }

    private static string DecodeTextContent(string value)
    {
        var decoded = value;

        for (var i = 0; i < MaxDecodePasses; i++)
        {
            var next = WebUtility.HtmlDecode(decoded);
            if (string.Equals(next, decoded, StringComparison.Ordinal))
            {
                break;
            }

            decoded = next;
        }

        return decoded
            .Replace('\u00A0', ' ')
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }
}
