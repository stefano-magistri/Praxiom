using System.Text;
using DottoressaIorio.Models;

namespace DottoressaIorio.Services.Patients;

public sealed class PatientCsvExportService
{
    private const string Separator = ";";
    private static readonly string[] HeaderColumns =
    [
        "IdPaziente",
        "Nome",
        "Cognome",
        "DataDiNascita",
        "IdTerapia",
        "DataTerapia",
        "DescrizioneTerapia",
        "DataCreazione"
    ];

    public byte[] GenerateCsv(IList<Patient> patients)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(Separator, HeaderColumns));

        foreach (var patient in patients)
        {
            foreach (var therapy in (patient.Therapies ?? [])
                         .OrderBy(t => t.CreatedDate)
                         .ThenBy(t => t.TherapyId))
            {
                builder.AppendJoin(Separator,
                    patient.PatientId.ToString(),
                    Escape(patient.FirstName),
                    Escape(patient.LastName),
                    FormatDate(patient.DateOfBirth),
                    therapy.TherapyId.ToString(),
                    FormatDate(therapy.CreatedDate),
                    Escape(therapy.Description),
                    FormatDate(patient.CreatedDate));
                builder.AppendLine();
            }
        }

        var contentBytes = Encoding.UTF8.GetBytes(builder.ToString());
        return [.. Encoding.UTF8.Preamble, .. contentBytes];
    }

    private static string FormatDate(DateTime? value)
    {
        return value?.ToString("yyyy-MM-dd") ?? string.Empty;
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
