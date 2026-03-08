using DottoressaIorio.Services.Licensing;
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
            _ => CreateLicenseCommand.Parse(args)
        };
    }
}

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
