using DottoressaIorio.Models;
using DottoressaIorio.Services.Pdf;
using Xunit.Abstractions;

namespace DottoressaIorio.Services.Tests;

public class PdfServiceIntegrationTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task GenerateRealPdf_ShouldCreatePdfFile()
    {
        // Arrange
        var builder = new PdfHtmlBuilder();
        var service = new PdfService(builder);

        var therapy = new Therapy
        {
            Patient = new Patient
            {
                Title = "Dr.",
                FirstName = "John",
                LastName = "Doe"
            },
            CreatedDate = new DateTime(2024, 1, 1),
            Description = "Sample therapy description"
        };

        string outputPath = Path.Combine(Path.GetTempPath(), "test_output.pdf");

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        // Act
        var pdfBytes = await service.GenerateTherapyPdfAsync(therapy);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "PDF should not be empty or too small");

        // Write PDF to inspect manually
        await File.WriteAllBytesAsync(outputPath, pdfBytes);

        Assert.True(File.Exists(outputPath), "PDF file was not created");
        Assert.True(new FileInfo(outputPath).Length > 1000, "Generated PDF file is unexpectedly small");

        // For debugging
        testOutputHelper.WriteLine($"PDF generated at: {outputPath}");
    }
}