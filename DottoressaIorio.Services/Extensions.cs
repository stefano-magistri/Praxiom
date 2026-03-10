using DottoressaIorio.Models;

namespace DottoressaIorio.Services;

/// <summary>
///     Extension methods for the Therapy class.
/// </summary>
public static class TherapyExtensions
{
    /// <summary>
    ///     Converts the therapy description to HTML format.
    /// </summary>
    /// <param name="therapy">The therapy instance.</param>
    /// <returns>The therapy description formatted as HTML.</returns>
    public static string GetHtmlDescription(this Therapy therapy)
    {
        return RichTextFormatting.ToSupportedHtml(therapy.Description);
    }
}

/// <summary>
///     Extension methods for the Patient class.
/// </summary>
public static class PatientExtensions
{
    /// <summary>
    ///     Converts the patient notes to HTML format.
    /// </summary>
    /// <param name="patient">The patient instance.</param>
    /// <returns>The patient notes formatted as HTML.</returns>
    public static string GetHtmlNotes(this Patient patient)
    {
        return StringExtensions.GetHtml(patient.Notes ?? "");
    }
}

/// <summary>
///     Extension methods for string manipulation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     Converts a plain text string to HTML format by wrapping paragraphs in p tags.
    /// </summary>
    /// <param name="value">The string to convert to HTML.</param>
    /// <returns>The input string formatted as HTML paragraphs.</returns>
    public static string GetHtml(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var paragraphs = value
            .Split("\n")
            .Select(par => $"<p>{par}</p>");

        var res = string.Join("", paragraphs);

        res = res.Replace("<p></p>", "<p><br /></p>");

        return res;
    }

    public static string ToPlainText(string? value)
    {
        return RichTextFormatting.ToPlainText(value);
    }

    public static string NormalizeStoredRichText(string? value)
    {
        return RichTextFormatting.NormalizeStoredValue(value);
    }

    public static string ToEditorValue(string? value)
    {
        return RichTextFormatting.ToEditorValue(value);
    }
}
