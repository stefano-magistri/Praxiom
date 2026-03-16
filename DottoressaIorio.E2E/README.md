# DottoressaIorio.E2E

Playwright + xUnit end-to-end coverage for the running application.

## What the fixture does

- loads settings from `appsettings.json` plus environment variables
- finds the built `DottoressaIorio.App.dll`
- creates a temporary database root
- seeds `Demo/app_unencrypted.db` when available
- generates a temporary signed local license tied to the current Windows machine/user
- starts the app with:
  - `ASPNETCORE_ENVIRONMENT=Development`
  - `Authentication__UseNegotiate=false`
  - `Authentication__AllowedUsername=<current Windows user>`
  - `DatabaseRootPath=<temp folder>`
  - `LocalLicense__LicensePath=<temp license path>`
  - `LocalLicense__PublicKeyPemBase64=<temp public key>`
- launches Playwright Chromium, installing it on demand if missing

## Requirements

- Windows
- .NET SDK 10.x
- built app output available locally

Windows is required because the temporary license uses the real Windows identity and registry `MachineGuid`.

## Configuration

Defaults live in `DottoressaIorio.E2E/appsettings.json`.

Environment variables can override them:

- `E2E__BaseUrl`
- `E2E__Headless`
- `E2E__Login__Username`
- `E2E__Login__Password`

If `E2E__BaseUrl` uses port `0`, the fixture replaces it with a free loopback port.

## Run locally

```powershell
dotnet restore DottoressaIorio.slnx
dotnet build DottoressaIorio.slnx -c Release
dotnet test DottoressaIorio.E2E/DottoressaIorio.E2E.csproj -c Release
```

Install Chromium explicitly if needed:

```powershell
pwsh DottoressaIorio.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
```

Run headed:

```powershell
$env:E2E__Headless = "false"
dotnet test DottoressaIorio.E2E/DottoressaIorio.E2E.csproj -c Release
```
