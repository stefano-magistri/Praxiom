using Serilog;
using Serilog.Events;

namespace DottoressaIorio.App.Middleware;

public static class LoggingExtensions
{
    public static IHostApplicationBuilder AddDottoressaLogging(this WebApplicationBuilder builder)
    {
        var logsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DottoressaIorio",
            "logs");
        Directory.CreateDirectory(logsDirectory);

        var logFilePath = Path.Combine(logsDirectory, "DottoressaIorio.App.log");

        builder.Logging.ClearProviders();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, shared: true)
            .CreateLogger();
        builder.Host.UseSerilog(Log.Logger, dispose: true);
        Log.Information("Serilog initialized. File sink path: {LogFilePath}", logFilePath);

        return builder;
    }
}
