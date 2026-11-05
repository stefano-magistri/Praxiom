using System.Text;
using DottoressaIorio.Models;
using DottoressaIorio.Services.Patients;

namespace DottoressaIorio.Services.Tests;

public class PatientCsvExportServiceTests
{
    private readonly PatientCsvExportService _service = new();

    [Fact]
    public void GenerateCsv_ShouldExportTitleLineCorrectly()
    {
        var csv = GetCsvText(_service.GenerateCsv([]));
        var lines = SplitLines(csv);

        Assert.Equal($";;;;;;;Export pazienti e terapie - {DateTime.Today:dd/MM/yyyy}", lines[0]);
    }

    [Fact]
    public void GenerateCsv_ShouldExportHeaderWithItalianColumnNames()
    {
        var csv = GetCsvText(_service.GenerateCsv([]));
        var lines = SplitLines(csv);

        Assert.Equal("Nome;Cognome;DataNascita;Eta;DataTerapia;DescrizioneTerapia;DataCreazione", lines[2]);
    }

    [Fact]
    public void GenerateCsv_ShouldExportOneRowPerPatientTherapyAssociation()
    {
        var patient = CreatePatient(
            "Mario",
            "Rossi",
            new DateTime(1980, 2, 1),
            new DateTime(2024, 1, 15),
            new Therapy
            {
                TherapyId = 101,
                Description = "Terapia 1",
                CreatedDate = new DateTime(2025, 2, 3)
            },
            new Therapy
            {
                TherapyId = 102,
                Description = "Terapia 2",
                CreatedDate = new DateTime(2025, 2, 4)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));
        var lines = SplitLines(csv);

        Assert.Equal(5, lines.Length);
        Assert.Contains(lines, line => line.Contains(";04/02/2025;Terapia 2;15/01/2024"));
        Assert.Contains(lines, line => line.Contains(";03/02/2025;Terapia 1;15/01/2024"));
    }

    [Fact]
    public void GenerateCsv_ShouldUseSemicolonSeparator()
    {
        var patient = CreatePatient(
            "Mario",
            "Rossi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                Description = "Test",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));
        var row = SplitLines(csv)[3];

        Assert.Equal(6, row.Count(ch => ch == ';'));
    }

    [Fact]
    public void GenerateCsv_ShouldEscapeValuesContainingSemicolons()
    {
        var patient = CreatePatient(
            "Mario",
            "Rossi;Verdi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
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
            "Mario",
            "Rossi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
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
            "Mario",
            "Rossi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                Description = "riga1\r\nriga2",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));

        Assert.Contains("\"riga1\nriga2\"", csv);
    }

    [Fact]
    public void GenerateCsv_ShouldStripHtmlTagsFromDescription()
    {
        var patient = CreatePatient(
            "Mario",
            "Rossi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                Description = "<p><strong>Mattina</strong></p><ul><li>Acqua</li><li>Pane</li></ul>",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));
        var dataLine = SplitLines(csv)[3];

        Assert.DoesNotContain("<p>", dataLine);
        Assert.DoesNotContain("<strong>", dataLine);
        Assert.Contains("Mattina", dataLine);
        Assert.Contains("Acqua", dataLine);
        Assert.Contains("Pane", dataLine);
    }

    [Fact]
    public void GenerateCsv_ShouldPreservePlainTextContainingAngleBrackets()
    {
        var patient = CreatePatient(
            "Mario",
            "Rossi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                Description = "Dose <bassa> dopo pranzo",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));
        var dataLine = SplitLines(csv)[3];

        Assert.Contains("Dose <bassa> dopo pranzo", dataLine);
    }

    [Fact]
    public void GenerateCsv_ShouldEmitUtf8Content()
    {
        var patient = CreatePatient(
            "Già",
            "Perché",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                Description = "Terapia è",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var bytes = _service.GenerateCsv([patient]);
        var content = Encoding.UTF8.GetString(bytes);

        Assert.True(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
        Assert.Contains("Già", content);
        Assert.Contains("Perché", content);
        Assert.Contains("Terapia è", content);
    }

    [Fact]
    public void GenerateCsv_ShouldInsertBlankRowBetweenDifferentPatients()
    {
        var firstPatient = CreatePatient(
            "Mario",
            "Bianchi",
            new DateTime(1980, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 1,
                Description = "Prima",
                CreatedDate = new DateTime(2025, 1, 2)
            });
        var secondPatient = CreatePatient(
            "Anna",
            "Rossi",
            new DateTime(1990, 1, 1),
            new DateTime(2024, 1, 1),
            new Therapy
            {
                TherapyId = 2,
                Description = "Seconda",
                CreatedDate = new DateTime(2025, 1, 1)
            });

        var csv = GetCsvText(_service.GenerateCsv([secondPatient, firstPatient]));
        var lines = SplitLines(csv);

        Assert.StartsWith("Mario;Bianchi", lines[3]);
        Assert.Equal(string.Empty, lines[4]);
    }

    [Fact]
    public void GenerateCsv_ShouldFormatDatesAsDdMmYyyy()
    {
        var patient = CreatePatient(
            "Mario",
            "Rossi",
            new DateTime(1980, 2, 1),
            new DateTime(2024, 1, 15),
            new Therapy
            {
                TherapyId = 2,
                Description = "Test",
                CreatedDate = new DateTime(2025, 2, 4)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));
        var dataLine = SplitLines(csv)[3];

        Assert.Contains("01/02/1980", dataLine);
        Assert.Contains("04/02/2025", dataLine);
        Assert.Contains("15/01/2024", dataLine);
    }

    [Fact]
    public void GenerateCsv_ShouldIncludeCalculatedAgeCorrectly()
    {
        var dateOfBirth = DateTime.Today.AddYears(-40);
        if (dateOfBirth > DateTime.Today)
        {
            dateOfBirth = dateOfBirth.AddDays(-1);
        }

        var patient = CreatePatient(
            "Mario",
            "Rossi",
            dateOfBirth,
            new DateTime(2024, 1, 15),
            new Therapy
            {
                TherapyId = 2,
                Description = "Test",
                CreatedDate = new DateTime(2025, 2, 4)
            });

        var csv = GetCsvText(_service.GenerateCsv([patient]));
        var dataLine = SplitLines(csv)[3];
        var expectedAge = PatientAgeCalculator.CalculateAge(dateOfBirth);

        Assert.Contains($";{expectedAge};", dataLine);
    }

    private static string GetCsvText(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    private static string[] SplitLines(string csv)
    {
        var normalized = csv.TrimEnd().TrimStart('\uFEFF');
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var insideQuotes = false;

        for (var i = 0; i < normalized.Length; i++)
        {
            var ch = normalized[i];

            if (ch == '"')
            {
                if (insideQuotes && i + 1 < normalized.Length && normalized[i + 1] == '"')
                {
                    currentLine.Append("\"\"");
                    i++;
                    continue;
                }

                insideQuotes = !insideQuotes;
                currentLine.Append(ch);
                continue;
            }

            if (!insideQuotes && (ch == '\r' || ch == '\n'))
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();

                if (ch == '\r' && i + 1 < normalized.Length && normalized[i + 1] == '\n')
                {
                    i++;
                }

                continue;
            }

            currentLine.Append(ch);
        }

        lines.Add(currentLine.ToString());
        return [.. lines];
    }

    private static Patient CreatePatient(
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        DateTime createdDate,
        params Therapy[] therapies)
    {
        return new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            CreatedDate = createdDate,
            Therapies = therapies.ToList()
        };
    }
}
