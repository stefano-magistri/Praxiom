using Serilog;
using Serilog.Events;

namespace DottoressaIorio.App.Middleware;

public static class LoggingExtensions
{
    public static (LogEventLevel MinimumLevel, LogEventLevel MicrosoftLevel, string LogFilePath)
        GetLoggingSettings(IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            var developmentLogFilePath = Path.Combine(
                environment.ContentRootPath,
                "logs",
                "DottoressaIorio.App.log");
            return (LogEventLevel.Debug, LogEventLevel.Information, developmentLogFilePath);
        }

        var productionLogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DottoressaIorio",
            "logs",
            "DottoressaIorio.App.log");
        return (LogEventLevel.Information, LogEventLevel.Warning, productionLogFilePath);
    }

    public static IHostApplicationBuilder AddDottoressaLogging(this WebApplicationBuilder builder)
    {
        var (minimumLevel, microsoftLevel, logFilePath) = GetLoggingSettings(builder.Environment);
        var logsDirectory = Path.GetDirectoryName(logFilePath)
            ?? throw new InvalidOperationException("Unable to resolve log directory path.");
        Directory.CreateDirectory(logsDirectory);

        builder.Logging.ClearProviders();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", microsoftLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, shared: true)
            .CreateLogger();
        builder.Host.UseSerilog(Log.Logger, dispose: true);
        Log.Information("Serilog initialized. File sink path: {LogFilePath}", logFilePath);

        return builder;
    }
}
