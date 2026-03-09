# DottoressaIorio.E2E

Playwright + xUnit end-to-end smoke tests for the Dottoressa Iorio app.

## Prerequisites

- .NET SDK 10.x
- Windows
- App build output available locally

These tests provision a temporary local license automatically. They do not require a manually installed `license.dat`.

## Install Playwright browsers

From repository root:

```powershell
dotnet restore DottoressaIorio.E2E/DottoressaIorio.E2E.csproj
dotnet build DottoressaIorio.E2E/DottoressaIorio.E2E.csproj -c Release
pwsh DottoressaIorio.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
```

Note: tests auto-install Chromium on first run if binaries are missing.

## Configuration

`DottoressaIorio.E2E/appsettings.json` contains defaults.

Environment variables override appsettings:

- `E2E__BaseUrl` (default: `https://localhost:7013`)
- `E2E__Headless` (default: `true`)
- `E2E__Login__Username` (optional; used only if a login form exists)
- `E2E__Login__Password` (optional; used only if a login form exists)

Fixture behavior:

- generates an ephemeral RSA keypair and a matching signed local license
- reads the current Windows identity and `MachineGuid`
- starts `DottoressaIorio.App` from the built DLL
- sets:
  - `Authentication__UseNegotiate=false`
  - `Authentication__AllowedUsername=<current Windows user>`
  - `LocalLicense__LicensePath=<temp license path>`
  - `LocalLicense__PublicKeyPemBase64=<temp public key>`
  - `DatabaseRootPath=<temp folder>`

Because license provisioning reads the real Windows identity and registry machine GUID, these tests currently require Windows.

## Run locally

1. Build the app and tests:

```powershell
dotnet build DottoressaIorio.slnx -c Release
```

2. Run E2E tests:

```powershell
dotnet test DottoressaIorio.E2E/DottoressaIorio.E2E.csproj -c Release --logger "trx;LogFileName=e2e.trx"
```

To run headed:

```powershell
$env:E2E__Headless = "false"
dotnet test DottoressaIorio.E2E/DottoressaIorio.E2E.csproj -c Release
```

## CI run

Typical CI sequence:

1. Build app + tests.
2. Install Playwright browsers with `playwright.ps1 install --with-deps chromium`.
3. Run `dotnet test DottoressaIorio.E2E/DottoressaIorio.E2E.csproj`.
