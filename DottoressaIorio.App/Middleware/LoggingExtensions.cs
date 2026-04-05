using Serilog;
using Serilog.Events;

namespace DottoressaIorio.App.Middleware;

public static class LoggingExtensions
{
    public static (LogEventLevel MinimumLevel, LogEventLevel MicrosoftLevel, string LogFilePath)
        GetLoggingSettings(IHostEnvironment environment, IConfiguration configuration)
    {
        var configuredLogFilePath = configuration["Logging:FilePath"];
        if (!string.IsNullOrWhiteSpace(configuredLogFilePath))
        {
            return (LogEventLevel.Information, LogEventLevel.Warning, configuredLogFilePath);
        }

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
            "Praxiom",
            "logs",
            "DottoressaIorio.App.log");

        if (environment.IsAzureEnvironment())
        {
            var homePath = configuration["HOME"];
            if (!string.IsNullOrWhiteSpace(homePath))
            {
                productionLogFilePath = Path.Combine(
                    homePath,
                    "LogFiles",
                    "Application",
                    "Praxiom",
                    "DottoressaIorio.App.log");
            }
            else
            {
                productionLogFilePath = Path.Combine(
                    environment.ContentRootPath,
                    "logs",
                    "DottoressaIorio.App.log");
            }
        }

        return (LogEventLevel.Information, LogEventLevel.Warning, productionLogFilePath);
    }

    public static void AddDottoressaLogging(this WebApplicationBuilder builder)
    {
        var (minimumLevel, microsoftLevel, logFilePath) = GetLoggingSettings(builder.Environment, builder.Configuration);
        var logsDirectory = Path.GetDirectoryName(logFilePath)
                            ?? throw new InvalidOperationException("Unable to resolve log directory path.");
        try
        {
            Directory.CreateDirectory(logsDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Unable to create log directory '{logsDirectory}'. Configure Logging:FilePath to a writable location.",
                ex);
        }

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
    }
}
