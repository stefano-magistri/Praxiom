# DottoressaIorio.E2E

Playwright + xUnit end-to-end smoke tests for the Dottoressa Iorio app.

## Prerequisites

- .NET SDK 10.x
- App running locally (or reachable URL)

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

For this app, run with `Authentication__UseNegotiate=false` in automated environments to avoid Windows auth dependency.

## Run locally

1. Start the app:

```powershell
$env:Authentication__UseNegotiate = "false"
dotnet run --project DottoressaIorio.App
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
3. Start app with `Authentication__UseNegotiate=false`.
4. Run `dotnet test DottoressaIorio.E2E/DottoressaIorio.E2E.csproj`.
