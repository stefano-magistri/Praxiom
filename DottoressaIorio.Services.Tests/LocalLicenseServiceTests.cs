using System.Security.Cryptography;
using DottoressaIorio.Services.Licensing;
using Microsoft.Extensions.Logging.Abstractions;

namespace DottoressaIorio.Services.Tests;

public class LocalLicenseServiceTests
{
    [Fact]
    public void Validate_WhenMachineAndUserMatch_ReturnsValid()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "machine-a", "HOST\\doctor");
        var service = CreateService(token, "machine-a", rsa.ExportSubjectPublicKeyInfoPem());

        var result = service.ValidateCurrentLicense("HOST\\doctor");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenMachineDoesNotMatch_ReturnsWrongMachine()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "machine-a", "HOST\\doctor");
        var service = CreateService(token, "machine-b", rsa.ExportSubjectPublicKeyInfoPem());

        var result = service.ValidateCurrentLicense("HOST\\doctor");

        Assert.Equal(LicenseValidationFailure.WrongMachine, result.Failure);
    }

    [Fact]
    public void Validate_WhenUserDoesNotMatch_ReturnsWrongUser()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "machine-a", "HOST\\doctor");
        var service = CreateService(token, "machine-a", rsa.ExportSubjectPublicKeyInfoPem());

        var result = service.ValidateCurrentLicense("HOST\\other");

        Assert.Equal(LicenseValidationFailure.WrongUser, result.Failure);
    }

    [Fact]
    public void Validate_WhenMachineAndUserDoNotMatch_ReturnsWrongMachine()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "machine-a", "HOST\\doctor");
        var service = CreateService(token, "machine-b", rsa.ExportSubjectPublicKeyInfoPem());

        var result = service.ValidateCurrentLicense("HOST\\other");

        Assert.Equal(LicenseValidationFailure.WrongMachine, result.Failure);
    }

    [Fact]
    public void Validate_WhenLicenseFileIsMissing_ReturnsMissingLicenseFile()
    {
        using var rsa = RSA.Create(2048);
        var service = new LocalLicenseService(
            new ThrowingLicenseFileReader(new FileNotFoundException()),
            new FakeMachineIdProvider("machine-a"),
            new LocalLicenseSettings { PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem() },
            NullLogger<LocalLicenseService>.Instance);

        var result = service.ValidateCurrentLicense("HOST\\doctor");

        Assert.Equal(LicenseValidationFailure.MissingLicenseFile, result.Failure);
    }

    [Fact]
    public void Validate_WhenLicenseFileIsMalformed_ReturnsMalformedLicenseFile()
    {
        using var rsa = RSA.Create(2048);
        var service = CreateService("not-a-license", "machine-a", rsa.ExportSubjectPublicKeyInfoPem());

        var result = service.ValidateCurrentLicense("HOST\\doctor");

        Assert.Equal(LicenseValidationFailure.MalformedLicenseFile, result.Failure);
    }

    [Fact]
    public void Validate_WhenLicenseIsTampered_ReturnsInvalidSignature()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "machine-a", "HOST\\doctor") + "tampered";
        var service = CreateService(token, "machine-a", rsa.ExportSubjectPublicKeyInfoPem());

        var result = service.ValidateCurrentLicense("HOST\\doctor");

        Assert.Equal(LicenseValidationFailure.InvalidSignature, result.Failure);
    }

    [Fact]
    public void Validate_WhenProductDoesNotMatch_ReturnsWrongProduct()
    {
        using var rsa = RSA.Create(2048);
        var token = LocalLicenseToken.CreateSignedLicense(
            new LicensePayload("AnotherProduct", "machine-a", "HOST\\doctor"),
            rsa.ExportPkcs8PrivateKeyPem());
        var service = CreateService(token, "machine-a", rsa.ExportSubjectPublicKeyInfoPem());

        var result = service.ValidateCurrentLicense("HOST\\doctor");

        Assert.Equal(LicenseValidationFailure.WrongProduct, result.Failure);
    }

    [Fact]
    public void Validate_NormalizesMachineUserAndProductValues()
    {
        using var rsa = RSA.Create(2048);
        var token = LocalLicenseToken.CreateSignedLicense(
            new LicensePayload(" dottoressaiorio ", " machine-a ", " host\\doctor "),
            rsa.ExportPkcs8PrivateKeyPem());
        var result = LocalLicenseToken.Validate(
            token,
            "DOTTORESSAIORIO",
            " MACHINE-A ",
            " HOST\\DOCTOR ",
            rsa.ExportSubjectPublicKeyInfoPem());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_CopiedValidLicenseOnDifferentMachine_ReturnsWrongMachine()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "machine-a", "HOST\\doctor");

        var result = LocalLicenseToken.Validate(
            token,
            LocalLicenseDefaults.ProductName,
            "machine-b",
            "HOST\\doctor",
            rsa.ExportSubjectPublicKeyInfoPem());

        Assert.Equal(LicenseValidationFailure.WrongMachine, result.Failure);
    }

    [Fact]
    public void Validate_CopiedValidLicenseForDifferentUserOnSameMachine_ReturnsWrongUser()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "machine-a", "HOST\\doctor");

        var result = LocalLicenseToken.Validate(
            token,
            LocalLicenseDefaults.ProductName,
            "machine-a",
            "HOST\\other",
            rsa.ExportSubjectPublicKeyInfoPem());

        Assert.Equal(LicenseValidationFailure.WrongUser, result.Failure);
    }

    private static LocalLicenseService CreateService(string token, string machineId, string publicKeyPem)
    {
        return new LocalLicenseService(
            new FixedLicenseFileReader(token),
            new FakeMachineIdProvider(machineId),
            new LocalLicenseSettings { PublicKeyPem = publicKeyPem },
            NullLogger<LocalLicenseService>.Instance);
    }

    private static string CreateToken(RSA rsa, string machineId, string allowedUsername)
    {
        return LocalLicenseToken.CreateSignedLicense(
            new LicensePayload(LocalLicenseDefaults.ProductName, machineId, allowedUsername),
            rsa.ExportPkcs8PrivateKeyPem());
    }

    private sealed class FixedLicenseFileReader(string content) : ILocalLicenseFileReader
    {
        public string ReadAllText(string path)
        {
            return content;
        }
    }

    private sealed class ThrowingLicenseFileReader(Exception exception) : ILocalLicenseFileReader
    {
        public string ReadAllText(string path)
        {
            throw exception;
        }
    }

    private sealed class FakeMachineIdProvider(string machineId) : IMachineIdProvider
    {
        public string? GetMachineId()
        {
            return machineId;
        }
    }
}
