using DottoressaIorio.App.Models;

namespace DottoressaIorio.App.Services;

public static class HtmlExtensions
{
    public static string GetHtmlDescription(this Therapy therapy)
    {
        return GetHtml(therapy.Description);
    }

    public static string GetHtmlNotes(this Patient patient)
    {
        return GetHtml(patient.Notes ?? "");
    }

    public static string GetHtml(string value)
    {
        var paragraphs = value
            .Split("\n")
            .Select(par => $"<p>{par}</p>");
        return string.Join("", paragraphs);
    }
}