using DottoressaIorio.App.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DottoressaIorio.App.Tests;

public class HostingEnvironmentExtensionsTests
{
    [Fact]
    public void IsAzureEnvironment_WhenEnvironmentIsAzure_ReturnsTrue()
    {
        var environment = new TestHostEnvironment
        {
            EnvironmentName = AppHostingEnvironmentExtensions.AzureEnvironmentName
        };

        var result = environment.IsAzureEnvironment();

        Assert.True(result);
    }

    [Fact]
    public void ResolveDatabaseRootPath_WhenConfigured_UsesConfiguredPath()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DatabaseRootPath"] = @"D:\custom-data" })
            .Build();
        var environment = new TestHostEnvironment
        {
            EnvironmentName = AppHostingEnvironmentExtensions.AzureEnvironmentName,
            ContentRootPath = @"C:\repo\DottoressaIorio.App"
        };

        var result = AppHostingEnvironmentExtensions.ResolveDatabaseRootPath(configuration, environment, isWindowsService: false);

        Assert.Equal(@"D:\custom-data", result);
    }

    [Fact]
    public void ResolveDatabaseRootPath_WhenAzureWithHome_UsesHomeDataFolder()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["HOME"] = @"D:\home" })
            .Build();
        var environment = new TestHostEnvironment
        {
            EnvironmentName = AppHostingEnvironmentExtensions.AzureEnvironmentName,
            ContentRootPath = @"C:\repo\DottoressaIorio.App"
        };

        var result = AppHostingEnvironmentExtensions.ResolveDatabaseRootPath(configuration, environment, isWindowsService: false);

        Assert.Equal(Path.Combine(@"D:\home", "data", "Praxiom"), result);
    }

    [Theory]
    [InlineData(true, "app.db")]
    [InlineData(false, "app_unencrypted.db")]
    public void GetDemoDatabaseTargetFileName_ReturnsExpectedName(bool useEncryption, string expectedFileName)
    {
        var result = DemoDatabaseExtensions.GetDemoDatabaseTargetFileName(useEncryption);

        Assert.Equal(expectedFileName, result);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
