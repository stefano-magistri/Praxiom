using DottoressaIorio.Models;
using DottoressaIorio.Services.Pdf;

namespace DottoressaIorio.Services.Tests;

public class PdfHtmlBuilderTests
{
    [Fact]
    public void BuildHtml_ShouldGenerateCorrectHtml()
    {
        var builder = new PdfHtmlBuilder();

        var therapy = new Therapy
        {
            Patient = new Patient
            {
                Title = "Dr.",
                FirstName = "John",
                LastName = "Doe"
            },
            CreatedDate = new DateTime(2023, 1, 1),
            Description = "Therapy description"
        };

        var html = builder.BuildHtml(therapy);

        Assert.Contains("Dr. John Doe", html);
        Assert.Contains("Therapy description", html);
    }

    [Fact]
    public void BuildHeaderHtml_ShouldReplacePlaceholders()
    {
        var builder = new PdfHtmlBuilder();
        var therapy = new Therapy { CreatedDate = new DateTime(2023, 1, 1) };
        var settings = new ClinicSettings { DoctorName = "Doctor Test" };

        string template = "{{doctorName}} Data terapia: {{therapyDate}}";
        var header = builder.BuildHeaderHtml(template, therapy, settings);

        Assert.Equal("Doctor Test Data terapia: 01/01/2023", header);
    }

    [Fact]
    public void BuildFooterHtml_ShouldReplacePlaceholders()
    {
        var builder = new PdfHtmlBuilder();
        var settings = new ClinicSettings { FooterText = "Footer test" };

        var therapy = new Therapy
        {
            CreatedDate = new DateTime(2023, 1, 1),
            Patient = new Patient { Title = "Dr.", FirstName = "John", LastName = "Doe" }
        };

        string template = "{{footerText}} Terapia {{therapyDate}} - {{patient}}";
        var footer = builder.BuildFooterHtml(template, therapy, settings);

        Assert.Equal("Footer test Terapia 01/01/2023 - Dr. John Doe", footer);
    }
}
