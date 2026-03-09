using DottoressaIorio.App.Middleware;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Serilog.Events;

namespace DottoressaIorio.App.Tests;

public class LoggingExtensionsTests
{
    [Fact]
    public void GetLoggingSettings_WhenDevelopment_UsesDebugAndContentRootLogsPath()
    {
        var environment = new TestHostEnvironment
        {
            EnvironmentName = Environments.Development,
            ContentRootPath = @"C:\repo\DottoressaIorio.App"
        };

        var result = LoggingExtensions.GetLoggingSettings(environment);

        Assert.Equal(LogEventLevel.Debug, result.MinimumLevel);
        Assert.Equal(LogEventLevel.Information, result.MicrosoftLevel);
        Assert.Equal(
            Path.Combine(environment.ContentRootPath, "logs", "DottoressaIorio.App.log"),
            result.LogFilePath);
    }

    [Fact]
    public void GetLoggingSettings_WhenProduction_UsesInformationAndProgramDataLogsPath()
    {
        var environment = new TestHostEnvironment
        {
            EnvironmentName = Environments.Production,
            ContentRootPath = @"C:\repo\DottoressaIorio.App"
        };

        var result = LoggingExtensions.GetLoggingSettings(environment);

        Assert.Equal(LogEventLevel.Information, result.MinimumLevel);
        Assert.Equal(LogEventLevel.Warning, result.MicrosoftLevel);
        Assert.Equal(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Praxiom",
                "logs",
                "DottoressaIorio.App.log"),
            result.LogFilePath);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
