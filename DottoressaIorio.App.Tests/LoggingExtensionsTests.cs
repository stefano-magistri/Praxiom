using DottoressaIorio.App.Middleware;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
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
        var configuration = new ConfigurationBuilder().Build();

        var result = LoggingExtensions.GetLoggingSettings(environment, configuration);

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
        var configuration = new ConfigurationBuilder().Build();

        var result = LoggingExtensions.GetLoggingSettings(environment, configuration);

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

    [Fact]
    public void GetLoggingSettings_WhenAzureWithHome_UsesHomeLogFilesPath()
    {
        var environment = new TestHostEnvironment
        {
            EnvironmentName = AppHostingEnvironmentExtensions.AzureEnvironmentName,
            ContentRootPath = @"C:\repo\DottoressaIorio.App"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["HOME"] = @"D:\home" })
            .Build();

        var result = LoggingExtensions.GetLoggingSettings(environment, configuration);

        Assert.Equal(LogEventLevel.Information, result.MinimumLevel);
        Assert.Equal(LogEventLevel.Warning, result.MicrosoftLevel);
        Assert.Equal(
            Path.Combine(
                @"D:\home",
                "LogFiles",
                "Application",
                "Praxiom",
                "DottoressaIorio.App.log"),
            result.LogFilePath);
    }

    [Fact]
    public void GetLoggingSettings_WhenConfigured_UsesConfiguredFilePath()
    {
        var environment = new TestHostEnvironment
        {
            EnvironmentName = Environments.Production,
            ContentRootPath = @"C:\repo\DottoressaIorio.App"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Logging:FilePath"] = @"D:\logs\praxiom.log" })
            .Build();

        var result = LoggingExtensions.GetLoggingSettings(environment, configuration);

        Assert.Equal(@"D:\logs\praxiom.log", result.LogFilePath);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
