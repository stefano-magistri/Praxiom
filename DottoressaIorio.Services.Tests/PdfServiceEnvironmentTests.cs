using DottoressaIorio.Services.Pdf;

namespace DottoressaIorio.Services.Tests;

public class PdfServiceEnvironmentTests
{
    [Fact]
    public void GetDefaultPlaywrightBrowsersPath_WhenAzureAndHomeIsSet_ReturnsHomeScopedPath()
    {
        var originalAspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotNetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var originalHome = Environment.GetEnvironmentVariable("HOME");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Azure");
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("HOME", @"C:\home");

            var path = PdfService.GetDefaultPlaywrightBrowsersPath();

            Assert.Equal(@"C:\home\ms-playwright", path);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCoreEnvironment);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotNetEnvironment);
            Environment.SetEnvironmentVariable("HOME", originalHome);
        }
    }

    [Fact]
    public void GetDefaultPlaywrightBrowsersPath_WhenProduction_ReturnsProgramDataPath()
    {
        var originalAspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotNetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var originalHome = Environment.GetEnvironmentVariable("HOME");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("HOME", null);

            var path = PdfService.GetDefaultPlaywrightBrowsersPath();

            Assert.Equal(@"C:\ProgramData\Praxiom\ms-playwright", path);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCoreEnvironment);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotNetEnvironment);
            Environment.SetEnvironmentVariable("HOME", originalHome);
        }
    }

    [Fact]
    public void GetDefaultPlaywrightBrowsersPath_WhenDevelopment_ReturnsNull()
    {
        var originalAspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotNetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var originalHome = Environment.GetEnvironmentVariable("HOME");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("HOME", @"C:\home");

            var path = PdfService.GetDefaultPlaywrightBrowsersPath();

            Assert.Null(path);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCoreEnvironment);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotNetEnvironment);
            Environment.SetEnvironmentVariable("HOME", originalHome);
        }
    }

    [Fact]
    public void GetPlaywrightInstallCommand_WhenPwshMissing_UsesWindowsPowerShell()
    {
        var originalPath = Environment.GetEnvironmentVariable("PATH");

        try
        {
            Environment.SetEnvironmentVariable("PATH", @"C:\Windows\System32");

            var command = PdfService.GetPlaywrightInstallCommand(@"C:\app");

            Assert.Equal("powershell", command.FileName);
            Assert.Contains(@"-File ""C:\app\playwright.ps1"" install chromium", command.Arguments, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }
}
