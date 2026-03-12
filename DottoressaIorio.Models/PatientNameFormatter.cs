using System.Globalization;

namespace DottoressaIorio.Models;

public static class PatientNameFormatter
{
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

    public static string FormatDisplayName(string? title, string? firstName, string? lastName)
    {
        var fullName = FormatFullName(firstName, lastName);
        if (string.IsNullOrWhiteSpace(title))
        {
            return fullName;
        }

        return string.IsNullOrWhiteSpace(fullName) ? title : $"{title} {fullName}";
    }

    public static string FormatDisplayName(Patient patient)
    {
        return FormatDisplayName(patient.Title, patient.FirstName, patient.LastName);
    }
}
