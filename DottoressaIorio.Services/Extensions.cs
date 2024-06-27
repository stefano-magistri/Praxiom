using DottoressaIorio.Models;

namespace DottoressaIorio.Services;

public static class TherapyExtensions
{
    public static string GetHtmlDescription(this Therapy therapy)
    {
        return StringExtensions.GetHtml(therapy.Description);
    }
}

public static class PatientExtensions
{
    public static string GetHtmlNotes(this Patient patient)
    {
        return StringExtensions.GetHtml(patient.Notes ?? "");
    }
}

public static class StringExtensions
{
    public static string GetHtml(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var paragraphs = value
            .Split("\n")
            .Select(par => $"<p>{par}</p>");
        return string.Join("", paragraphs);
    }
}