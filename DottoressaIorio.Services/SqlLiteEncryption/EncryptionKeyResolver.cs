using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace DottoressaIorio.Services.SqlLiteEncryption;

public sealed class EncryptionKeyResolver(IConfiguration configuration, string environmentName, string? localKeyFilePath = null)
{
    private const string DevelopmentEnvironmentName = "Development";
    private const string ConfigKeyName = "EncryptionKey";
    private const string DpapiProtectionName = "DPAPI-LocalMachine";

    public static string DefaultLocalKeyFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Praxiom",
            "secrets",
            "encryption-key.json");

    public string Resolve()
    {
        var configuredKey = configuration[ConfigKeyName];
        if (IsDevelopmentEnvironment() && configuredKey is not null)
            return configuredKey;

        var localKeyFilePath = GetLocalKeyFilePath();
        if (TryReadLocalKey(localKeyFilePath, out var encryptionKey, out var localStoreFailure))
            return encryptionKey;

        if (IsDevelopmentEnvironment())
            throw new InvalidOperationException(
                $"Encryption key was not found. Checked Development configuration key '{ConfigKeyName}' and local store '{localKeyFilePath}'. {localStoreFailure}");

        throw new InvalidOperationException(
            $"Encryption key local store is required outside Development. Expected '{localKeyFilePath}'. {localStoreFailure}");
    }

    public string GetLocalKeyFilePath()
    {
        return string.IsNullOrWhiteSpace(localKeyFilePath) ? DefaultLocalKeyFilePath : localKeyFilePath;
    }

    private bool IsDevelopmentEnvironment()
    {
        return string.Equals(environmentName, DevelopmentEnvironmentName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadLocalKey(string keyFilePath, out string encryptionKey, out string failureReason)
    {
        encryptionKey = string.Empty;

        if (!File.Exists(keyFilePath))
        {
            failureReason = "The local encryption key file does not exist.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(keyFilePath));
            if (!document.RootElement.TryGetProperty(ConfigKeyName, out var encryptionKeyElement))
                return TryReadDpapiWrappedKey(document.RootElement, out encryptionKey, out failureReason);

            encryptionKey = encryptionKeyElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                failureReason = "The local encryption key file contains an empty 'EncryptionKey' value.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            failureReason = $"Failed to read the local encryption key file: {ex.Message}";
            return false;
        }
    }

    private static bool TryReadDpapiWrappedKey(JsonElement rootElement, out string encryptionKey, out string failureReason)
    {
        encryptionKey = string.Empty;

        if (!rootElement.TryGetProperty("Protection", out var protectionElement) ||
            !rootElement.TryGetProperty("Payload", out var payloadElement))
        {
            failureReason = "The local encryption key file does not contain a supported secret payload.";
            return false;
        }

        if (!string.Equals(protectionElement.GetString(), DpapiProtectionName, StringComparison.Ordinal))
        {
            failureReason = "The local encryption key file uses an unsupported protection scheme.";
            return false;
        }

        if (!OperatingSystem.IsWindows())
        {
            failureReason = "DPAPI-protected encryption keys are only supported on Windows.";
            return false;
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(payloadElement.GetString() ?? string.Empty);
            var unprotectedBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.LocalMachine);
            encryptionKey = System.Text.Encoding.UTF8.GetString(unprotectedBytes);

            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                failureReason = "The DPAPI-wrapped encryption key payload is empty.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            failureReason = $"Failed to read the DPAPI-wrapped encryption key payload: {ex.Message}";
            return false;
        }
    }
}
