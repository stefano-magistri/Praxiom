using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Pdf;

namespace DottoressaIorio.App.Middleware;

public static class PdfEndpointsExtensions
{
    public static WebApplication MapDottoressaPdfEndpoints(this WebApplication app)
    {
        app.MapGet("/pdf/therapy/{therapyId:int}/{displayName?}", async (
            int therapyId,
            PatientTherapyRepository therapyRepository,
            PatientRepository patientRepository,
            PdfService pdfService,
            HttpContext httpContext) =>
        {
            var therapy = await therapyRepository.GetByIdAsync(therapyId);
            if (therapy is null)
            {
                return Results.NotFound();
            }

            var patient = await patientRepository.GetByIdAsync(therapy.PatientId);
            if (patient is null)
            {
                return Results.NotFound();
            }

            therapy.Patient = patient;

            var pdfBytes = await pdfService.GenerateTherapyPdfAsync(therapy);
            var filename =
                $"{patient.FirstName}_{patient.LastName}_{therapy.CreatedDate:yy_MM_dd}_{DateTime.Now.ToFileTimeUtc()}.pdf";
            var quotedFilename = filename.Replace("\\", "_").Replace("\"", "_");

            httpContext.Response.Headers.ContentDisposition =
                $"inline; filename=\"{quotedFilename}\"; filename*=UTF-8''{Uri.EscapeDataString(filename)}";

            return Results.File(pdfBytes, "application/pdf");
        });

        return app;
    }
}

