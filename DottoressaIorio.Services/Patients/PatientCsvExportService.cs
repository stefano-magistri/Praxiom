using System.Text;
using DottoressaIorio.Models;

namespace DottoressaIorio.Services.Patients;

public sealed class PatientCsvExportService
{
    private const string Separator = ";";
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
            .Where(group => group.Therapies.Count > 0)
            .OrderBy(group => group.Patient.LastName)
            .ThenBy(group => group.Patient.FirstName)
            .ToList();

        for (var patientIndex = 0; patientIndex < patientGroups.Count; patientIndex++)
        {
            var patient = patientGroups[patientIndex].Patient;
            var age = PatientAgeCalculator.CalculateAge(patient.DateOfBirth);

            foreach (var therapy in patientGroups[patientIndex].Therapies)
            {
                builder.AppendJoin(Separator,
                    Escape(patient.FirstName),
                    Escape(patient.LastName),
                    FormatDate(patient.DateOfBirth),
                    age?.ToString() ?? string.Empty,
                    FormatDate(therapy.CreatedDate),
                    Escape(RichTextFormatting.ToPlainText(therapy.Description)),
                    FormatDate(patient.CreatedDate));
                builder.AppendLine();
            }

            if (patientIndex < patientGroups.Count - 1)
            {
                builder.AppendLine();
            }
        }

        var contentBytes = Encoding.UTF8.GetBytes(builder.ToString());
        return [.. Encoding.UTF8.Preamble, .. contentBytes];
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
