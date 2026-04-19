using System.Text;
using System.Text.RegularExpressions;
using DottoressaIorio.Models;

namespace DottoressaIorio.Services.Patients;

/// <summary>
/// Exports patients and their therapies as a UTF-8 CSV file.
/// </summary>
public sealed class PatientCsvExportService
{
    private const string Separator = ";";
    private static readonly Regex SupportedRichTextTagRegex = new(
        @"</?(p|br|ul|ol|li|strong|b|em|i|div|section|article|span)\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] HeaderColumns =
    [
        "Nome",
        "Cognome",
        "DataNascita",
        "Eta",
        "DataTerapia",
        "DescrizioneTerapia",
        "DataCreazione"
    ];

    /// <summary>
    /// Generates the CSV payload for the supplied patient list.
    /// </summary>
    public byte[] GenerateCsv(IList<Patient> patients)
    {
        var builder = new StringBuilder();
        builder.AppendLine($";;;;;;;Export pazienti e terapie - {DateTime.Today:dd/MM/yyyy}");
        builder.AppendLine();
        builder.AppendLine(string.Join(Separator, HeaderColumns));

        var patientGroups = patients
            .Select(patient => new
            {
                Patient = patient,
                Therapies = (patient.Therapies ?? [])
                    .OrderByDescending(t => t.CreatedDate)
                    .ThenByDescending(t => t.TherapyId)
                    .ToList()
            })
            .OrderBy(group => group.Patient.LastName)
            .ThenBy(group => group.Patient.FirstName)
            .ToList();

        for (var patientIndex = 0; patientIndex < patientGroups.Count; patientIndex++)
        {
            var group = patientGroups[patientIndex];
            var patient = group.Patient;
            var therapies = group.Therapies;
            var age = PatientAgeCalculator.CalculateAge(patient.DateOfBirth);

            if (therapies.Count == 0)
            {
                AppendPatientRow(
                    builder,
                    patient,
                    age,
                    therapyCreatedDate: null,
                    therapyDescription: null);
            }
            else
            {
                foreach (var therapy in therapies)
                {
                    AppendPatientRow(
                        builder,
                        patient,
                        age,
                        therapy.CreatedDate,
                        FormatDescription(therapy.Description));
                }
            }

            if (patientIndex < patientGroups.Count - 1)
            {
                builder.AppendLine();
            }
        }

        var contentBytes = Encoding.UTF8.GetBytes(builder.ToString());
        return [.. Encoding.UTF8.Preamble, .. contentBytes];
    }

    private static void AppendPatientRow(
        StringBuilder builder,
        Patient patient,
        int? age,
        DateTime? therapyCreatedDate,
        string? therapyDescription)
    {
        builder.AppendJoin(Separator,
            Escape(patient.FirstName),
            Escape(patient.LastName),
            FormatDate(patient.DateOfBirth),
            age?.ToString() ?? string.Empty,
            FormatDate(therapyCreatedDate),
            Escape(therapyDescription),
            FormatDate(patient.CreatedDate));
        builder.AppendLine();
    }

    private static string FormatDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Rich-text descriptions are flattened only when they contain the supported stored markup set.
        if (!value.Contains('<', StringComparison.Ordinal))
        {
            return RichTextFormatting.ToPlainText(value);
        }

        return SupportedRichTextTagRegex.IsMatch(value)
            ? RichTextFormatting.ToPlainText(value)
            : value;
    }

    private static string FormatDate(DateTime? value)
    {
        return value?.ToString("dd/MM/yyyy") ?? string.Empty;
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escapedValue = value.Replace("\"", "\"\"");
        return escapedValue.IndexOfAny([';', '"', '\r', '\n']) >= 0
            ? $"\"{escapedValue}\""
            : escapedValue;
    }
}
