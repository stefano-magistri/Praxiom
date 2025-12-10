using Serilog;

namespace DottoressaIorio.App.Middleware;

public static class LoggingExtensions
{
    public static IHostApplicationBuilder AddDottoressaLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/DottoressaIorio.App.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        builder.Host.UseSerilog();

        return builder;
    }
}