using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DottoressaIorio.Services.Licensing;

public static class LocalLicenseDefaults
{
    public const string ProductName = "DottoressaIorio";
    public const string LicenseFileName = "license.dat";
    public const string LicenseTokenPrefix = "DIOL1";

    public static string LicensePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Praxiom",
            LicenseFileName);

    public const string PublicKeyPem = """
                                       -----BEGIN PUBLIC KEY-----
                                       MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqh+V/68H4elPmNOiKy4o
                                       3rB+KG7DWZ2x2dTtSO25/6XVvHguLfWS8Gc4rguWdzs99iPl5mKPzRyZYdLMrK/+
                                       F1T7qRlp0EVInRGq9cdkc+EFBdmSUVeIIAd2J6eXw/i6PRZcE8Sm6dtwbZq6JLrP
                                       vB7F3oC0SulnvH+KYZA26cFxiyViAIGf16LGA+Tsh79nS+h1r+jy5f8VFN5DIl7N
                                       UMyvJqqi/QOq9sqeshJplWeS+5T89xGNxMlkVutwbnWdgbjhtB/yQVrNFakAVNK+
                                       0V2aiPW6HRz2KWdguIF8APzOfD/a2WUNh7DmAY8VVYyeK3BRfhVCLJxWXgDNhuvr
                                       PQIDAQAB
                                       -----END PUBLIC KEY-----
                                       """;
}

public sealed class LocalLicenseSettings
{
    public string ProductName { get; init; } = LocalLicenseDefaults.ProductName;
    public string LicensePath { get; init; } = LocalLicenseDefaults.LicensePath;
    public string PublicKeyPem { get; init; } = LocalLicenseDefaults.PublicKeyPem;

    public static LocalLicenseSettings FromConfiguration(IConfiguration configuration)
    {
        var settings = configuration.GetSection("LocalLicense").Get<LocalLicenseSettings>() ?? new LocalLicenseSettings();
        var publicKeyPemBase64 = configuration["LocalLicense:PublicKeyPemBase64"];

        if (!string.IsNullOrWhiteSpace(publicKeyPemBase64))
        {
            settings = settings.WithPublicKey(Encoding.UTF8.GetString(Convert.FromBase64String(publicKeyPemBase64)));
        }

        return settings;
    }

    private LocalLicenseSettings WithPublicKey(string publicKeyPem)
    {
        return new LocalLicenseSettings
        {
            ProductName = ProductName,
            LicensePath = LicensePath,
            PublicKeyPem = publicKeyPem
        };
    }
}

public sealed record LicensePayload(string ProductName, string MachineId, string AllowedUsername);

public enum LicenseValidationFailure
{
    None = 0,
    MissingLicenseFile,
    MalformedLicenseFile,
    InvalidSignature,
    MissingMachineId,
    MissingAllowedUsername,
    WrongProduct,
    WrongMachine,
    WrongUser
}

public sealed record LicenseValidationResult(LicenseValidationFailure Failure, LicensePayload? Payload = null)
{
    public bool IsValid => Failure == LicenseValidationFailure.None;

    public static LicenseValidationResult Valid(LicensePayload payload) => new(LicenseValidationFailure.None, payload);

    public static LicenseValidationResult Invalid(LicenseValidationFailure failure) => new(failure);
}

public interface ILocalLicenseFileReader
{
    string ReadAllText(string path);
}

public sealed class LocalLicenseFileReader : ILocalLicenseFileReader
{
    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }
}

public interface IMachineIdProvider
{
    string? GetMachineId();
}

public sealed class RegistryMachineIdProvider : IMachineIdProvider
{
    private const string MachineGuidRegistryPath = @"SOFTWARE\Microsoft\Cryptography";
    private const string MachineGuidValueName = "MachineGuid";

    public string? GetMachineId()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        using var key = Registry.LocalMachine.OpenSubKey(MachineGuidRegistryPath, writable: false);
        return key?.GetValue(MachineGuidValueName)?.ToString();
    }
}

public interface ILocalLicenseService
{
    LicenseValidationResult ValidateCurrentLicense(string currentUsername);
}

public static class LocalLicenseValidator
{
    public static LicenseValidationResult ValidateToken(
        string token,
        string expectedProductName,
        string currentMachineId,
        string currentUsername,
        string publicKeyPem)
    {
        var normalizedMachineId = LicenseNormalizer.NormalizeMachineId(currentMachineId);
        if (string.IsNullOrWhiteSpace(normalizedMachineId))
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MissingMachineId);
        }

        var normalizedUsername = LicenseNormalizer.NormalizeUsername(currentUsername);
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MissingAllowedUsername);
        }

        return LocalLicenseToken.Validate(
            token,
            expectedProductName,
            normalizedMachineId,
            normalizedUsername,
            publicKeyPem);
    }

    public static LicenseValidationResult ValidateFile(
        string licensePath,
        string expectedProductName,
        string currentMachineId,
        string currentUsername,
        string publicKeyPem)
    {
        string token;
        try
        {
            token = File.ReadAllText(licensePath);
        }
        catch (FileNotFoundException)
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile);
        }
        catch (DirectoryNotFoundException)
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile);
        }
        catch (Exception)
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MalformedLicenseFile);
        }

        return ValidateToken(
            token,
            expectedProductName,
            currentMachineId,
            currentUsername,
            publicKeyPem);
    }
}

public sealed class LocalLicenseService(
    ILocalLicenseFileReader fileReader,
    IMachineIdProvider machineIdProvider,
    LocalLicenseSettings settings,
    ILogger<LocalLicenseService> logger) : ILocalLicenseService
{
    public LicenseValidationResult ValidateCurrentLicense(string currentUsername)
    {
        var normalizedMachineId = LicenseNormalizer.NormalizeMachineId(machineIdProvider.GetMachineId());
        if (string.IsNullOrWhiteSpace(normalizedMachineId))
        {
            logger.LogError("License validation failed: machine identifier is unavailable.");
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MissingMachineId);
        }

        var normalizedUsername = LicenseNormalizer.NormalizeUsername(currentUsername);
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            logger.LogError("License validation failed: configured licensed Windows user is unavailable.");
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MissingAllowedUsername);
        }

        string token;
        try
        {
            token = fileReader.ReadAllText(settings.LicensePath);
        }
        catch (FileNotFoundException)
        {
            logger.LogError("License validation failed: license file is missing.");
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile);
        }
        catch (DirectoryNotFoundException)
        {
            logger.LogError("License validation failed: license directory is missing.");
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "License validation failed while reading the license file.");
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MalformedLicenseFile);
        }

        var parseResult = LocalLicenseValidator.ValidateToken(
            token,
            settings.ProductName,
            normalizedMachineId,
            normalizedUsername,
            settings.PublicKeyPem);

        if (parseResult.IsValid)
        {
            logger.LogInformation("License validation succeeded.");
            return parseResult;
        }

        logger.LogError("License validation failed: {Failure}.", parseResult.Failure);
        return parseResult;
    }
}

public static class LocalLicenseServiceCollectionExtensions
{
    public static IServiceCollection AddLocalLicenseServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(LocalLicenseSettings.FromConfiguration(configuration));
        services.AddSingleton(new LicenseLaunchOverride());
        services.AddSingleton<ILocalLicenseFileReader, LocalLicenseFileReader>();
        services.AddSingleton<IMachineIdProvider, RegistryMachineIdProvider>();
        services.AddSingleton<ILocalLicenseService, LocalLicenseService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ILicenseService, LicenseService>();
        return services;
    }
}

public static class LicenseNormalizer
{
    public static string NormalizeMachineId(string? machineId)
    {
        return Normalize(machineId);
    }

    public static string NormalizeUsername(string? username)
    {
        return Normalize(username);
    }

    public static string NormalizeProductName(string? productName)
    {
        return Normalize(productName);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }
}

public static class LocalLicenseToken
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string CreateSignedLicense(LicensePayload payload, string privateKeyPem)
    {
        var normalizedPayload = new LicensePayload(
            LicenseNormalizer.NormalizeProductName(payload.ProductName),
            LicenseNormalizer.NormalizeMachineId(payload.MachineId),
            LicenseNormalizer.NormalizeUsername(payload.AllowedUsername));

        var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalizedPayload, JsonOptions));
        var payloadToken = Base64UrlEncode(payloadBytes);
        var signedData = Encoding.UTF8.GetBytes($"{LocalLicenseDefaults.LicenseTokenPrefix}.{payloadToken}");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var signature = rsa.SignData(signedData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{LocalLicenseDefaults.LicenseTokenPrefix}.{payloadToken}.{Base64UrlEncode(signature)}";
    }

    public static LicenseValidationResult Validate(
        string token,
        string expectedProductName,
        string currentMachineId,
        string currentUsername,
        string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MalformedLicenseFile);
        }

        var parts = token.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || !parts[0].Equals(LocalLicenseDefaults.LicenseTokenPrefix, StringComparison.Ordinal))
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MalformedLicenseFile);
        }

        byte[] payloadBytes;
        byte[] signatureBytes;
        try
        {
            payloadBytes = Base64UrlDecode(parts[1]);
            signatureBytes = Base64UrlDecode(parts[2]);
        }
        catch (FormatException)
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MalformedLicenseFile);
        }

        var signedData = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
        using var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(publicKeyPem);
        }
        catch (Exception)
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.InvalidSignature);
        }

        var isSignatureValid = rsa.VerifyData(
            signedData,
            signatureBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        if (!isSignatureValid)
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.InvalidSignature);
        }

        LicensePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<LicensePayload>(payloadBytes, JsonOptions);
        }
        catch (JsonException)
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MalformedLicenseFile);
        }

        if (payload is null)
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.MalformedLicenseFile);
        }

        var normalizedPayload = new LicensePayload(
            LicenseNormalizer.NormalizeProductName(payload.ProductName),
            LicenseNormalizer.NormalizeMachineId(payload.MachineId),
            LicenseNormalizer.NormalizeUsername(payload.AllowedUsername));

        if (!normalizedPayload.ProductName.Equals(
                LicenseNormalizer.NormalizeProductName(expectedProductName),
                StringComparison.Ordinal))
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.WrongProduct);
        }

        if (!normalizedPayload.MachineId.Equals(
                LicenseNormalizer.NormalizeMachineId(currentMachineId),
                StringComparison.Ordinal))
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.WrongMachine);
        }

        if (!normalizedPayload.AllowedUsername.Equals(
                LicenseNormalizer.NormalizeUsername(currentUsername),
                StringComparison.Ordinal))
        {
            return LicenseValidationResult.Invalid(LicenseValidationFailure.WrongUser);
        }

        return LicenseValidationResult.Valid(normalizedPayload);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value
            .Replace('-', '+')
            .Replace('_', '/');

        var padding = normalized.Length % 4;
        if (padding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');
        }

        return Convert.FromBase64String(normalized);
    }
}
