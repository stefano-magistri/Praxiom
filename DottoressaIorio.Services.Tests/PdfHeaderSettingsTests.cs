using DottoressaIorio.Models;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Pdf;
using Moq;

namespace DottoressaIorio.Services.Tests;

public class PdfHeaderSettingsTests
{
    [Fact]
    public async Task BuildHeaderHtmlAsync_ShouldUseClinicSettingsRepositoryValues()
    {
        var repository = new Mock<IClinicSettingsRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(new ClinicSettings
        {
            DoctorName = "Dott. Test Repo",
            DoctorTitle = "Medico Test",
            Specialization = "Specialista Test",
            ClinicName = "Studio Test",
            Address = "Via Test 123",
            Phone = "55512345",
            Email = "repo.test@example.test",
            FooterText = "Footer Test"
        });

        var service = new PdfService(new PdfHtmlBuilder(), repository.Object);
        var therapy = new Therapy
        {
            Patient = new Patient
            {
                FirstName = "Mario",
                LastName = "Rossi"
            },
            CreatedDate = new DateTime(2026, 3, 8),
            Description = "Terapia"
        };

        var templateRoot = Path.Combine(AppContext.BaseDirectory, "_pdf_therapy");

        var headerHtml = await service.BuildHeaderHtmlAsync(therapy, templateRoot);

        Assert.Contains("Dott. Test Repo", headerHtml, StringComparison.Ordinal);
        Assert.Contains("Medico Test", headerHtml, StringComparison.Ordinal);
        Assert.Contains("Specialista Test", headerHtml, StringComparison.Ordinal);
        Assert.Contains("Studio Test", headerHtml, StringComparison.Ordinal);
        Assert.Contains("Via Test 123", headerHtml, StringComparison.Ordinal);
        Assert.Contains("55512345", headerHtml, StringComparison.Ordinal);
        Assert.Contains("repo.test@example.test", headerHtml, StringComparison.Ordinal);
    }
}
