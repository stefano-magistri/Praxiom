using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Patients;

namespace DottoressaIorio.App.Middleware;

public static class PatientExportEndpointsExtensions
{
    public static WebApplication MapDottoressaPatientExportEndpoints(this WebApplication app)
    {
        app.MapGet("/patients/export-csv", async (
            string? search,
            PatientRepository patientRepository,
            PatientCsvExportService patientCsvExportService,
            HttpContext httpContext) =>
        {
            var patients = await patientRepository.SearchPatientsWithTherapiesAsync(search ?? string.Empty);
            var csvBytes = patientCsvExportService.GenerateCsv(patients);
            var hasActiveFilters = !string.IsNullOrWhiteSpace(search);
            var filename =
                $"{(hasActiveFilters ? "pazienti-filtrati" : "pazienti-completo")}-{DateTime.Today:yyyy-MM-dd}.csv";
            var quotedFilename = filename.Replace("\\", "_").Replace("\"", "_");

            httpContext.Response.Headers.ContentDisposition =
                $"attachment; filename=\"{quotedFilename}\"; filename*=UTF-8''{Uri.EscapeDataString(filename)}";

            return Results.File(csvBytes, "text/csv; charset=utf-8");
        });

        return app;
    }
}
