using DinkToPdf;
using DinkToPdf.Contracts;
using DottoressaIorio.Models;

namespace DottoressaIorio.Services;

/// <summary>
/// Service class for generating PDF documents from therapy data.
/// </summary>
public class PdfService
{
    private readonly IConverter _converter;

    /// <summary>
    /// Initializes a new instance of the PdfService class.
    /// </summary>
    /// <param name="converter">The PDF converter instance to use for document generation.</param>
    public PdfService(IConverter converter)
    {
        _converter = converter;
    }

    /// <summary>
    /// Generates a PDF document from a therapy record.
    /// </summary>
    /// <param name="therapy">The therapy record to generate the PDF from.</param>
    /// <returns>A byte array containing the generated PDF document.</returns>
    public byte[] GenerateTherapyPdf(Therapy therapy)
    { 
        var htmlContent = $@"
        <html>
            <head>
                <style>
                    body {{ 
                        font-family: Calibri, sans-serif; 
                    }} 

                    .h1 {{
                        font-size: 20px;
                        font-weight: bold;
                    }}

                    p {{
                        font-size: 18px;
                    }}
                </style>
            </head>
            <body>
                 <p class=""h1"">{therapy.Patient.Title} {therapy.Patient.FirstName} {therapy.Patient.LastName},</p>
                {therapy.GetHtmlDescription()}
            </body>
        </html>";

        var globalSettings = new GlobalSettings
        {
            ColorMode = ColorMode.Color,
            Orientation = Orientation.Portrait,
            PaperSize = PaperKind.A4
        };

        var objectSettings = new ObjectSettings
        {
            PagesCount = true,
            HtmlContent = htmlContent,
            WebSettings = { DefaultEncoding = "utf-8" },
            HeaderSettings = { FontName = "Calibri", FontSize = 12, Spacing = 15,
                Right = $"Data terapia: {therapy.CreatedDate:dd/MM/yyyy}",
                HtmUrl = "_pdf_therapy/header.html"
            },
            FooterSettings = { FontName = "Calibri", FontSize = 8, Spacing = 10,
                Left = $"Terapia Paziente: {therapy.CreatedDate:dd/MM/yyyy} - {therapy.Patient.Title} {therapy.Patient.FirstName} {therapy.Patient.LastName}",
                Right = "Pagina [page] di [toPage]",
                HtmUrl = "_pdf_therapy/footer.html",  }
        };

        var pdf = _converter.Convert(new HtmlToPdfDocument
        {
            GlobalSettings = globalSettings,
            Objects = { objectSettings }
        });

        return pdf;
    }
}