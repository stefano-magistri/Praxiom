using DinkToPdf;
using DinkToPdf.Contracts;
using DottoressaIorio.Models;
using Moq;

namespace DottoressaIorio.Services.Tests;

/// <summary>
/// Contains unit tests for the PdfService class.
/// </summary>
public class PdfServiceTests
{
    /// <summary>
    /// Tests that the GenerateTherapyPdf method correctly formats the PDF document with the therapy details,
    /// including patient information, therapy description, and proper header/footer settings.
    /// </summary>
    [Fact]
    public void GenerateTherapyPdf_ShouldCallConverterWithCorrectSettings()
    {
        // Arrange
        var mockConverter = new Mock<IConverter>();
        var pdfService = new PdfService(mockConverter.Object);

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

        mockConverter.Setup(m => m.Convert(It.IsAny<HtmlToPdfDocument>())).Returns(Array.Empty<byte>());

        // Act
        var result = pdfService.GenerateTherapyPdf(therapy);

        // Assert 
        mockConverter.Verify(m => m.Convert(It.Is<HtmlToPdfDocument>(doc =>
            //doc.GlobalSettings.PaperSize.ToString() == PaperKind.A4.ToString() &&
            doc.Objects[0].HtmlContent.Contains("Dr. John Doe") &&
            doc.Objects[0].HtmlContent.Contains("Therapy description") &&
            doc.Objects[0].HeaderSettings.Right == "Data terapia: 01/01/2023" &&
            doc.Objects[0].FooterSettings.Left.Contains("Dr. John Doe")
        )), Times.Once);
    }
}