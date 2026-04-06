using System.Text.RegularExpressions;

namespace DottoressaIorio.App.Tests;

public class BackupTriggerConfigurationTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();

    [Fact]
    public void Program_KeepsScheduledTaskMode()
    {
        var programSource = ReadRepoFile("DottoressaIorio.App", "Program.cs");

        Assert.Contains("argument.Equals(\"--online-backup\", StringComparison.OrdinalIgnoreCase)", programSource);
        Assert.Contains("TriggerBackupAsync(\"scheduled-task\")", programSource);
    }

    [Fact]
    public void Program_KeepsShutdownBackup_AndRemovesStartupBackup()
    {
        var programSource = ReadRepoFile("DottoressaIorio.App", "Program.cs");

        Assert.Contains("TriggerBackupAsync(", programSource);
        Assert.Contains("\"application-stopping\"", programSource);
        Assert.DoesNotContain("TriggerBackupAsync(\"application-startup\"", programSource);
    }

    [Theory]
    [InlineData("DottoressaIorio.App", "Middleware", "PdfEndpointsExtensions.cs")]
    [InlineData("DottoressaIorio.App", "Pages", "Settings", "Index.razor")]
    [InlineData("DottoressaIorio.App", "Pages", "Patients", "Create.razor")]
    [InlineData("DottoressaIorio.App", "Pages", "Patients", "Edit.razor")]
    [InlineData("DottoressaIorio.App", "Pages", "Patients", "Therapies", "Create.razor")]
    [InlineData("DottoressaIorio.App", "Pages", "Patients", "Therapies", "Edit.razor")]
    [InlineData("DottoressaIorio.App", "Pages", "TherapyTemplates", "Create.razor")]
    [InlineData("DottoressaIorio.App", "Pages", "TherapyTemplates", "Edit.razor")]
    public void NonDisasterRecoveryTriggers_NoLongerRequestBackups(params string[] relativePath)
    {
        var source = ReadRepoFile(relativePath);

        Assert.DoesNotContain("TriggerBackupAsync(", source);
    }

    [Fact]
    public void Installer_CreatesDailyBackupTask()
    {
        var installScript = ReadRepoFile("Setup", "install.bat");

        Assert.Contains('/' + "SC DAILY", installScript);
        Assert.Contains('/' + "ST 20:00", installScript);
        Assert.DoesNotContain('/' + "SC HOURLY /MO 2", Regex.Replace(installScript, @"\s+", " "));
    }

    private static string ReadRepoFile(params string[] relativePath)
    {
        return File.ReadAllText(Path.Combine(RepoRoot, Path.Combine(relativePath)));
    }

    private static string ResolveRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DottoressaIorio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
