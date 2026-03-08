using DottoressaIorio.Services.Licensing;

var options = LicenseGeneratorOptions.Parse(args);
if (options is null)
{
    PrintUsage();
    return 1;
}

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

static void PrintUsage()
{
    Console.WriteLine("""
                      Usage:
                        dotnet run --project DottoressaIorio.LicenseGenerator -- ^
                          --machine-id "<MachineGuid>" ^
                          --allowed-username "<MACHINE\\username>" ^
                          --private-key "<path-to-private-key.pem>" ^
                          --output "<path-to-license.dat>" ^
                          [--product-name "DottoressaIorio"]
                      """);
}

internal sealed record LicenseGeneratorOptions(
    string MachineId,
    string AllowedUsername,
    string PrivateKeyPath,
    string OutputPath,
    string ProductName)
{
    public static LicenseGeneratorOptions? Parse(string[] args)
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

        return new LicenseGeneratorOptions(machineId, allowedUsername, privateKeyPath, outputPath, productName);
    }

    private static string? ReadValue(string[] args, ref int index)
    {
        index++;
        return index < args.Length ? args[index] : null;
    }
}
