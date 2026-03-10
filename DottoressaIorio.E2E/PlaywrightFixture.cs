using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Net;
using System.Net.Sockets;
using DottoressaIorio.Services.Licensing;
using DottoressaIorio.Services.SqlLiteEncryption;
using Microsoft.Data.Sqlite;
using Microsoft.Playwright;
using Microsoft.Win32;
using SQLitePCL;

namespace DottoressaIorio.E2E;

[CollectionDefinition(Name)]
public sealed class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "playwright";
}

public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IBrowser? _browser;
    private Process? _appProcess;
    private string? _tempDirectory;
    private readonly StringBuilder _appStdout = new();
    private readonly StringBuilder _appStderr = new();

    public IPlaywright Playwright { get; private set; } = null!;

    public E2eSettings Settings { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        raw.SetProvider(new SQLite3Provider_e_sqlcipher());
        Batteries_V2.Init();
        Settings = ResolveRuntimeSettings(E2eSettings.Load());
        await StartApplicationAsync();
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await LaunchBrowserWithInstallFallbackAsync();
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();

        await StopApplicationAsync();
        Playwright.Dispose();
    }

    public async Task<IBrowserContext> NewContextAsync()
    {
        if (_browser is null) throw new InvalidOperationException("Browser is not initialized.");

        return await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Settings.BaseUrl,
            IgnoreHTTPSErrors = true,
            AcceptDownloads = true
        });
    }

    public async Task<IReadOnlyList<int>> SeedPatientsAsync(
        IEnumerable<(string FirstName, string LastName, string Email, DateTime DateOfBirth)> patients)
    {
        using var connection = CreateTestDatabaseConnection();
        using var transaction = connection.BeginTransaction();
        var patientIds = new List<int>();

        foreach (var patient in patients)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                                  INSERT INTO "Patients"
                                  ("Title", "FirstName", "LastName", "DateOfBirth", "PlaceOfBirth", "Gender", "PhoneNumber",
                                   "Email", "Notes", "CreatedDate", "EditDate", "Deleted")
                                  VALUES
                                  (NULL, $firstName, $lastName, $dateOfBirth, NULL, NULL, NULL,
                                   $email, NULL, $createdDate, NULL, 0);
                                  SELECT last_insert_rowid();
                                  """;
            command.Parameters.AddWithValue("$firstName", patient.FirstName);
            command.Parameters.AddWithValue("$lastName", patient.LastName);
            command.Parameters.AddWithValue("$dateOfBirth", patient.DateOfBirth);
            command.Parameters.AddWithValue("$email", patient.Email);
            command.Parameters.AddWithValue("$createdDate", DateTime.UtcNow);

            patientIds.Add(Convert.ToInt32((long)(await command.ExecuteScalarAsync()
                                              ?? throw new InvalidOperationException("Failed to seed patient."))));
        }

        await transaction.CommitAsync();
        return patientIds;
    }

    public async Task SeedTherapyTemplatesAsync(IEnumerable<(string Title, string Description)> templates)
    {
        using var connection = CreateTestDatabaseConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var template in templates)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                                  INSERT INTO "TherapyTemplates"
                                  ("Title", "Description", "CreatedDate", "EditDate", "Deleted")
                                  VALUES
                                  ($title, $description, $createdDate, NULL, 0);
                                  """;
            command.Parameters.AddWithValue("$title", template.Title);
            command.Parameters.AddWithValue("$description", template.Description);
            command.Parameters.AddWithValue("$createdDate", DateTime.UtcNow);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    public async Task SeedTherapiesAsync(int patientId, IEnumerable<string> descriptions)
    {
        using var connection = CreateTestDatabaseConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var description in descriptions)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                                  INSERT INTO "Therapies"
                                  ("Description", "PatientId", "CreatedDate", "EditDate", "Deleted")
                                  VALUES
                                  ($description, $patientId, $createdDate, NULL, 0);
                                  """;
            command.Parameters.AddWithValue("$description", description);
            command.Parameters.AddWithValue("$patientId", patientId);
            command.Parameters.AddWithValue("$createdDate", DateTime.UtcNow);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private async Task<IBrowser> LaunchBrowserWithInstallFallbackAsync()
    {
        try
        {
            return await LaunchBrowserAsync();
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist",
                                                 StringComparison.OrdinalIgnoreCase))
        {
            await InstallPlaywrightBrowsersAsync();
            return await LaunchBrowserAsync();
        }
    }

    private Task<IBrowser> LaunchBrowserAsync()
    {
        return Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Settings.Headless
        });
    }

    private static async Task InstallPlaywrightBrowsersAsync()
    {
        var baseDir = AppContext.BaseDirectory;
        string fileName;
        string arguments;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fileName = "pwsh";
            arguments = $"\"{Path.Combine(baseDir, "playwright.ps1")}\" install chromium";
        }
        else
        {
            fileName = "bash";
            arguments = $"\"{Path.Combine(baseDir, "playwright.sh")}\" install chromium";
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException(
                $"Failed to install Playwright browsers. Exit code: {process.ExitCode}. {error}".Trim());
        }
    }

    private async Task StartApplicationAsync()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "DottoressaIorio.E2E", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        var currentUsername = GetCurrentWindowsIdentityName();
        var machineId = ReadMachineGuid();
        if (string.IsNullOrWhiteSpace(currentUsername) || string.IsNullOrWhiteSpace(machineId))
        {
            throw new InvalidOperationException("Unable to determine the current Windows identity or machine id for E2E license generation.");
        }

        using var rsa = RSA.Create(2048);
        var licensePath = Path.Combine(_tempDirectory, LocalLicenseDefaults.LicenseFileName);
        var token = LocalLicenseToken.CreateSignedLicense(
            new LicensePayload(LocalLicenseDefaults.ProductName, machineId, currentUsername),
            rsa.ExportPkcs8PrivateKeyPem());
        await File.WriteAllTextAsync(licensePath, token);

        var repoRoot = ResolveRepositoryRoot();
        SeedDatabaseIfAvailable(repoRoot.FullName, _tempDirectory);
        var appAssemblyPath = ResolveAppAssemblyPath(repoRoot.FullName);

        _appProcess = new Process
        {
            StartInfo = BuildAppStartInfo(
                appAssemblyPath,
                currentUsername,
                licensePath,
                rsa.ExportSubjectPublicKeyInfoPem())
        };

        _appStdout.Clear();
        _appStderr.Clear();
        _appProcess.OutputDataReceived += (_, args) => AppendLine(_appStdout, args.Data);
        _appProcess.ErrorDataReceived += (_, args) => AppendLine(_appStderr, args.Data);
        _appProcess.Start();
        _appProcess.BeginOutputReadLine();
        _appProcess.BeginErrorReadLine();
        await WaitForApplicationReadyAsync();
    }

    private async Task StopApplicationAsync()
    {
        if (_appProcess is not null && !_appProcess.HasExited)
        {
            _appProcess.Kill(entireProcessTree: true);
            await _appProcess.WaitForExitAsync();
        }

        _appProcess?.Dispose();
        _appProcess = null;

        if (!string.IsNullOrWhiteSpace(_tempDirectory) && Directory.Exists(_tempDirectory))
        {
            await DeleteDirectoryWithRetryAsync(_tempDirectory);
        }

        _tempDirectory = null;
    }

    private async Task WaitForApplicationReadyAsync()
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (_appProcess is not null && _appProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"E2E app host exited before becoming ready. Stdout: {GetBufferedOutput(_appStdout)} Stderr: {GetBufferedOutput(_appStderr)}"
                        .Trim());
            }

            try
            {
                using var response = await client.GetAsync(Settings.BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"Timed out waiting for the app to become reachable at {Settings.BaseUrl}. Stdout: {GetBufferedOutput(_appStdout)} Stderr: {GetBufferedOutput(_appStderr)}"
                .Trim());
    }

    private static E2eSettings ResolveRuntimeSettings(E2eSettings settings)
    {
        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var configuredBaseUrl))
        {
            throw new InvalidOperationException($"Invalid E2E base URL: '{settings.BaseUrl}'.");
        }

        if (configuredBaseUrl.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                $"Unsupported E2E base URL scheme '{configuredBaseUrl.Scheme}'. Use http or https.");
        }

        if (configuredBaseUrl.Port != 0)
        {
            return new E2eSettings
            {
                BaseUrl = configuredBaseUrl.GetLeftPart(UriPartial.Authority),
                Headless = settings.Headless,
                Login = settings.Login
            };
        }

        var builder = new UriBuilder(configuredBaseUrl)
        {
            Host = IPAddress.Loopback.ToString(),
            Port = GetFreeTcpPort()
        };

        return new E2eSettings
        {
            BaseUrl = builder.Uri.GetLeftPart(UriPartial.Authority),
            Headless = settings.Headless,
            Login = settings.Login
        };
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static string GetCurrentWindowsIdentityName()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("E2E license provisioning requires Windows.");
        }

        return WindowsIdentity.GetCurrent().Name;
    }

    private static string ReadMachineGuid()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("E2E license provisioning requires Windows.");
        }

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography", writable: false);
        return key?.GetValue("MachineGuid")?.ToString() ?? string.Empty;
    }

    private static void SeedDatabaseIfAvailable(string repoRootPath, string targetDirectory)
    {
        var demoDbPath = Path.Combine(repoRootPath, "Demo", "app_unencrypted.db");
        if (!File.Exists(demoDbPath))
        {
            return;
        }

        File.Copy(demoDbPath, Path.Combine(targetDirectory, "app_unencrypted.db"), overwrite: true);
    }

    private SqliteConnection CreateTestDatabaseConnection()
    {
        if (string.IsNullOrWhiteSpace(_tempDirectory))
        {
            throw new InvalidOperationException("E2E temporary database directory is not initialized.");
        }

        return new SqlLiteDbEncryptionService(string.Empty, _tempDirectory).CreateConnection();
    }

    private ProcessStartInfo BuildAppStartInfo(
        string appAssemblyPath,
        string currentUsername,
        string licensePath,
        string publicKeyPem)
    {
        var workingDirectory = Path.GetDirectoryName(appAssemblyPath)
                               ?? throw new InvalidOperationException("Unable to resolve app working directory.");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{appAssemblyPath}\" --urls \"{Settings.BaseUrl}\"",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["Authentication__UseNegotiate"] = "false";
        startInfo.Environment["Authentication__AllowedUsername"] = currentUsername;
        startInfo.Environment["DatabaseRootPath"] = _tempDirectory;
        startInfo.Environment["EncryptionKey"] = string.Empty;
        startInfo.Environment["LocalLicense__LicensePath"] = licensePath;
        startInfo.Environment["LocalLicense__PublicKeyPemBase64"] =
            Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem));

        return startInfo;
    }

    private static string ResolveAppAssemblyPath(string repoRootPath)
    {
        var appBinDirectory = Path.Combine(repoRootPath, "DottoressaIorio.App", "bin");
        if (!Directory.Exists(appBinDirectory))
        {
            throw new DirectoryNotFoundException($"App build output directory not found: {appBinDirectory}");
        }

        var candidates = Directory
            .EnumerateFiles(appBinDirectory, "DottoressaIorio.App.dll", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}net10.0{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new FileNotFoundException(
                "Built app assembly not found for E2E startup. Build DottoressaIorio.App before running E2E tests.",
                Path.Combine(appBinDirectory, "**", "net10.0", "DottoressaIorio.App.dll"));
        }

        return candidates[0].FullName;
    }

    private static void AppendLine(StringBuilder builder, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (builder)
        {
            builder.AppendLine(line);
        }
    }

    private static string GetBufferedOutput(StringBuilder builder)
    {
        lock (builder)
        {
            return builder.ToString();
        }
    }

    private static async Task DeleteDirectoryWithRetryAsync(string directoryPath)
    {
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Directory.Delete(directoryPath, recursive: true);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                await Task.Delay(500);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                await Task.Delay(500);
            }
        }

        Directory.Delete(directoryPath, recursive: true);
    }

    private static DirectoryInfo ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "DottoressaIorio.slnx")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root for E2E app startup.");
    }
}
