using DottoressaIorio.Services.Pdf;
using Microsoft.Extensions.DependencyInjection;

public static class PdfServiceExtensions
{
    public static IServiceCollection AddPdfServices(this IServiceCollection services)
    {
        services.AddSingleton<PdfHtmlBuilder>();
        services.AddScoped<PdfService>();
        return services;
    }
}