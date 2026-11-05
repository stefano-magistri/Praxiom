using DottoressaIorio.Services.Licensing;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

var command = LicenseToolCommand.Parse(args);
if (command is null)
{
    PrintUsage();
    return 1;
}

switch (command)
{
    case CreateLicenseCommand create:
        return RunCreate(create);
    case ValidateLicenseCommand validate:
        return RunValidate(validate);
    case WriteSecretCommand writeSecret:
        return RunWriteSecret(writeSecret);
    case ReadSecretCommand readSecret:
        return RunReadSecret(readSecret);
    default:
        PrintUsage();
        return 1;
}

static int RunCreate(CreateLicenseCommand options)
{
    var privateKeyPem = File.ReadAllText(options.PrivateKeyPath);
    var payload = new LicensePayload(
        options.ProductName,
        options.MachineId,
        options.AllowedUsername);

    var token = LocalLicenseToken.CreateSignedLicense(payload, privateKeyPem);

    var outputDirectory = Path.GetDirectoryName(options.OutputPath);
    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    File.WriteAllText(options.OutputPath, token);
    Console.WriteLine($"License file written to {options.OutputPath}");
    return 0;
}

static int RunValidate(ValidateLicenseCommand options)
{
    var machineId = options.MachineId ?? GetMachineGuid();
    var result = LocalLicenseValidator.ValidateFile(
        options.LicensePath,
        options.ProductName,
        machineId,
        options.AllowedUsername,
        LocalLicenseDefaults.PublicKeyPem);

    Console.WriteLine(result.Failure);
    return result.Failure switch
    {
        LicenseValidationFailure.None => 0,
        LicenseValidationFailure.MissingLicenseFile => 2,
        LicenseValidationFailure.WrongMachine => 3,
        LicenseValidationFailure.WrongUser => 4,
        LicenseValidationFailure.WrongProduct => 5,
        LicenseValidationFailure.InvalidSignature => 6,
        LicenseValidationFailure.MalformedLicenseFile => 6,
        LicenseValidationFailure.MissingMachineId => 7,
        LicenseValidationFailure.MissingAllowedUsername => 8,
        _ => 9
    };
}

static int RunWriteSecret(WriteSecretCommand options)
{
    if (!OperatingSystem.IsWindows())
    {
        Console.Error.WriteLine("DPAPI local-machine protection is supported only on Windows.");
        return 10;
    }

    var outputDirectory = Path.GetDirectoryName(options.SecretPath);
    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    var rawBytes = Encoding.UTF8.GetBytes(options.SecretValue);
    var protectedBytes = ProtectedData.Protect(rawBytes, null, DataProtectionScope.LocalMachine);
    var payload = new ProtectedSecretPayload("v1", "DPAPI-LocalMachine", Convert.ToBase64String(protectedBytes));

    File.WriteAllText(options.SecretPath, JsonSerializer.Serialize(payload));
    Console.WriteLine($"Secret file written to {options.SecretPath}");
    return 0;
}

static int RunReadSecret(ReadSecretCommand options)
{
    if (!OperatingSystem.IsWindows())
    {
        Console.Error.WriteLine("DPAPI local-machine protection is supported only on Windows.");
        return 10;
    }

    if (!File.Exists(options.SecretPath))
    {
        Console.Error.WriteLine("Secret file not found.");
        return 11;
    }

    try
    {
        var json = File.ReadAllText(options.SecretPath);
        var payload = JsonSerializer.Deserialize<ProtectedSecretPayload>(json);
        if (payload is null ||
            !string.Equals(payload.Protection, "DPAPI-LocalMachine", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(payload.Payload))
        {
            Console.Error.WriteLine("Secret file format is invalid.");
            return 12;
        }

        var protectedBytes = Convert.FromBase64String(payload.Payload);
        var rawBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.LocalMachine);
        var secretValue = Encoding.UTF8.GetString(rawBytes);
        if (string.IsNullOrWhiteSpace(secretValue))
        {
            Console.Error.WriteLine("Secret value is empty.");
            return 13;
        }

        Console.Write(secretValue);
        return 0;
    }
    catch (Exception ex) when (ex is JsonException or FormatException or CryptographicException or IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine(ex.Message);
        return 14;
    }
}

static void PrintUsage()
{
    Console.WriteLine("""
                      Usage:
                        dotnet run --project DottoressaIorio.LicenseGenerator -- create ^
                          --machine-id "<MachineGuid>" ^
                          --allowed-username "<MACHINE\\username>" ^
                          --private-key "<path-to-private-key.pem>" ^
                          --output "<path-to-license.dat>" ^
                          [--product-name "DottoressaIorio"]

                        dotnet run --project DottoressaIorio.LicenseGenerator -- validate ^
                          --license-file "<path-to-license.dat>" ^
                          --allowed-username "<MACHINE\\username>" ^
                          [--machine-id "<MachineGuid>"] ^
                          [--product-name "DottoressaIorio"]

                        dotnet run --project DottoressaIorio.LicenseGenerator -- write-secret ^
                          --secret-file "<path-to-secret.json>" ^
                          --value "<secret-value>"

                        dotnet run --project DottoressaIorio.LicenseGenerator -- read-secret ^
                          --secret-file "<path-to-secret.json>"
                      """);
}

static string GetMachineGuid()
{
    if (!OperatingSystem.IsWindows())
    {
        return string.Empty;
    }

    using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography", writable: false);
    return key?.GetValue("MachineGuid")?.ToString() ?? string.Empty;
}

internal abstract record LicenseToolCommand
{
    public static LicenseToolCommand? Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        return args[0] switch
        {
            "create" => CreateLicenseCommand.Parse(args[1..]),
            "validate" => ValidateLicenseCommand.Parse(args[1..]),
            "write-secret" => WriteSecretCommand.Parse(args[1..]),
            "read-secret" => ReadSecretCommand.Parse(args[1..]),
            _ => CreateLicenseCommand.Parse(args)
        };
    }
}

internal sealed record ProtectedSecretPayload(string Format, string Protection, string Payload);

internal sealed record CreateLicenseCommand(
    string MachineId,
    string AllowedUsername,
    string PrivateKeyPath,
    string OutputPath,
    string ProductName) : LicenseToolCommand
{
    public new static CreateLicenseCommand? Parse(string[] args)
    {
        string? machineId = null;
        string? allowedUsername = null;
        string? privateKeyPath = null;
        string? outputPath = null;
        var productName = LocalLicenseDefaults.ProductName;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--machine-id":
                    machineId = ReadValue(args, ref i);
                    break;
                case "--allowed-username":
                    allowedUsername = ReadValue(args, ref i);
                    break;
                case "--private-key":
                    privateKeyPath = ReadValue(args, ref i);
                    break;
                case "--output":
                    outputPath = ReadValue(args, ref i);
                    break;
                case "--product-name":
                    productName = ReadValue(args, ref i);
                    break;
                default:
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(machineId) ||
            string.IsNullOrWhiteSpace(allowedUsername) ||
            string.IsNullOrWhiteSpace(privateKeyPath) ||
            string.IsNullOrWhiteSpace(outputPath) ||
            string.IsNullOrWhiteSpace(productName))
        {
            return null;
        }

        return new CreateLicenseCommand(machineId, allowedUsername, privateKeyPath, outputPath, productName);
    }

    private static string? ReadValue(string[] args, ref int index)
    {
        index++;
        return index < args.Length ? args[index] : null;
    }
}

internal sealed record ValidateLicenseCommand(
    string LicensePath,
    string AllowedUsername,
    string ProductName,
    string? MachineId) : LicenseToolCommand
{
    public new static ValidateLicenseCommand? Parse(string[] args)
    {
        string? licensePath = null;
        string? allowedUsername = null;
        string? machineId = null;
        var productName = LocalLicenseDefaults.ProductName;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--license-file":
                    licensePath = ReadValue(args, ref i);
                    break;
                case "--allowed-username":
                    allowedUsername = ReadValue(args, ref i);
                    break;
                case "--machine-id":
                    machineId = ReadValue(args, ref i);
                    break;
                case "--product-name":
                    productName = ReadValue(args, ref i);
                    break;
                default:
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(licensePath) ||
            string.IsNullOrWhiteSpace(allowedUsername) ||
            string.IsNullOrWhiteSpace(productName))
        {
            return null;
        }

        return new ValidateLicenseCommand(licensePath, allowedUsername, productName, machineId);
    }

    private static string? ReadValue(string[] args, ref int index)
    {
        index++;
        return index < args.Length ? args[index] : null;
    }
}

internal sealed record WriteSecretCommand(string SecretPath, string SecretValue) : LicenseToolCommand
{
    public new static WriteSecretCommand? Parse(string[] args)
    {
        string? secretPath = null;
        string? secretValue = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--secret-file":
                    secretPath = ReadValue(args, ref i);
                    break;
                case "--value":
                    secretValue = ReadValue(args, ref i);
                    break;
                default:
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(secretPath) || secretValue is null)
        {
            return null;
        }

        return new WriteSecretCommand(secretPath, secretValue);
    }

    private static string? ReadValue(string[] args, ref int index)
    {
        index++;
        return index < args.Length ? args[index] : null;
    }
}

internal sealed record ReadSecretCommand(string SecretPath) : LicenseToolCommand
{
    public new static ReadSecretCommand? Parse(string[] args)
    {
        string? secretPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--secret-file":
                    secretPath = ReadValue(args, ref i);
                    break;
                default:
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(secretPath))
        {
            return null;
        }

        return new ReadSecretCommand(secretPath);
    }

    private static string? ReadValue(string[] args, ref int index)
    {
        index++;
        return index < args.Length ? args[index] : null;
    }
}
