using System.Text;
using DottoressaIorio.Models;
using DottoressaIorio.Services.Patients;

namespace DottoressaIorio.Services.Tests;

public class PatientCsvExportServiceTests
{
    private readonly PatientCsvExportService _service = new();

    [Fact]
    public void GenerateCsv_ShouldExportHeaderWithItalianColumnNames()
    {
        var csv = GetCsvText(_service.GenerateCsv([]));

        Assert.StartsWith("IdPaziente;Nome;Cognome;DataDiNascita;IdTerapia;DataTerapia;DescrizioneTerapia;DataCreazione", csv);
    }

    [Fact]
    public void GenerateCsv_ShouldExportOneRowPerPatientTherapyAssociation()
    {
        var patient = CreatePatient(
            10,
            "Mario",
            "Rossi",
            new DateTime(1980, 2, 1),
            new DateTime(2024, 1, 15),
            new Therapy
            {
                TherapyId = 101,
                PatientId = 10,
                Description = "Terapia 1",
                CreatedDate = new DateTime(2025, 2, 3)
            },
            new Therapy
            {
                TherapyId = 102,
                PatientId = 10,
                Description = "Terapia 2",
                CreatedDate = new DateTime(2025, 2, 4)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));
        var lines = csv.TrimEnd().Split(Environment.NewLine);

        Assert.Equal(3, lines.Length);
        Assert.Contains("10;Mario;Rossi;1980-02-01;101;2025-02-03;Terapia 1;2024-01-15", lines);
        Assert.Contains("10;Mario;Rossi;1980-02-01;102;2025-02-04;Terapia 2;2024-01-15", lines);
    }

    [Fact]
    public void GenerateCsv_ShouldUseSemicolonSeparator()
    {
        var patient = CreatePatient(
            1,
            "Mario",
            "Rossi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                PatientId = 1,
                Description = "Test",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));
        var row = csv.TrimEnd().Split(Environment.NewLine)[1];

        Assert.Equal(7, row.Count(ch => ch == ';'));
    }

    [Fact]
    public void GenerateCsv_ShouldEscapeValuesContainingSemicolons()
    {
        var patient = CreatePatient(
            1,
            "Mario",
            "Rossi;Verdi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                PatientId = 1,
                Description = "Dose; mattina",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));

        Assert.Contains("\"Rossi;Verdi\"", csv);
        Assert.Contains("\"Dose; mattina\"", csv);
    }

    [Fact]
    public void GenerateCsv_ShouldEscapeValuesContainingDoubleQuotes()
    {
        var patient = CreatePatient(
            1,
            "Mario",
            "Rossi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                PatientId = 1,
                Description = "Terapia \"test\"",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));

        Assert.Contains("\"Terapia \"\"test\"\"\"", csv);
    }

    [Fact]
    public void GenerateCsv_ShouldEscapeValuesContainingLineBreaks()
    {
        var patient = CreatePatient(
            1,
            "Mario",
            "Rossi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                PatientId = 1,
                Description = "riga1\r\nriga2",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));

        Assert.Contains("\"riga1\r\nriga2\"", csv);
    }

    [Fact]
    public void GenerateCsv_ShouldEmitUtf8Content()
    {
        var patient = CreatePatient(
            1,
            "Già",
            "Perché",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                PatientId = 1,
                Description = "Terapia è",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var bytes = _service.GenerateCsv([patient]);

        Assert.True(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
        Assert.Contains("Già", Encoding.UTF8.GetString(bytes));
        Assert.Contains("Perché", Encoding.UTF8.GetString(bytes));
        Assert.Contains("Terapia è", Encoding.UTF8.GetString(bytes));
    }

    private static string GetCsvText(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    private static Patient CreatePatient(
        int patientId,
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        DateTime createdDate,
        params Therapy[] therapies)
    {
        return new Patient
        {
            PatientId = patientId,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            CreatedDate = createdDate,
            Therapies = therapies.ToList()
        };
    }
}
