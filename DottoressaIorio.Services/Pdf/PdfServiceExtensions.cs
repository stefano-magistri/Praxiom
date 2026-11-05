using Microsoft.Extensions.DependencyInjection;

namespace DottoressaIorio.Services.Pdf;

/// <summary>
/// Registers the services required for therapy PDF generation.
/// </summary>
public static class PdfServiceExtensions
{
    /// <summary>
    /// Adds the PDF HTML builder and request-scoped PDF service.
    /// </summary>
    public static IServiceCollection AddPdfServices(this IServiceCollection services)
    {
        services.AddSingleton<PdfHtmlBuilder>();
        services.AddScoped<PdfService>();
        return services;
    }
}
