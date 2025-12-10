using Microsoft.Extensions.DependencyInjection;

namespace DottoressaIorio.Services.Pdf;

public static class PdfServiceExtensions
{
    public static IServiceCollection AddPdfServices(this IServiceCollection services)
    {
        services.AddSingleton<PdfHtmlBuilder>();
        services.AddScoped<PdfService>();
        return services;
    }
}