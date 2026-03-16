using System.Globalization;

namespace DottoressaIorio.Models;

/// <summary>
/// Provides shared patient-name formatting for UI, PDF, and export paths.
/// </summary>
public static class PatientNameFormatter
{
    /// <summary>
    /// Formats a full name using title casing and tolerant null handling.
    /// </summary>
    public static string FormatFullName(string? firstName, string? lastName)
    {
        var fullName = $"{firstName ?? string.Empty} {lastName ?? string.Empty}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
            fullName.ToLower(CultureInfo.CurrentCulture));
    }

    /// <summary>
    /// Formats a display name with an optional title prefix.
    /// </summary>
    public static string FormatDisplayName(string? title, string? firstName, string? lastName)
    {
        var fullName = FormatFullName(firstName, lastName);
        if (string.IsNullOrWhiteSpace(title))
        {
            return fullName;
        }

        return string.IsNullOrWhiteSpace(fullName) ? title : $"{title} {fullName}";
    }

    /// <summary>
    /// Formats a display name from a patient instance.
    /// </summary>
    public static string FormatDisplayName(Patient patient)
    {
        return FormatDisplayName(patient.Title, patient.FirstName, patient.LastName);
    }
}
