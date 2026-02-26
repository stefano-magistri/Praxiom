# Dottoressa Iorio Medical Management System

Blazor Server application for managing patients, therapies, precompiled therapy templates, and therapy PDF exports.

## Solution Structure

- `DottoressaIorio.App`: Blazor Server host/UI, middleware, static assets.
- `DottoressaIorio.Models`: domain entities (`Patient`, `Therapy`, `TherapyTemplate`).
- `DottoressaIorio.Data`: EF Core `ApplicationDbContext` + migrations.
- `DottoressaIorio.Repositories`: repository/data-access layer.
- `DottoressaIorio.Services`: business services (PDF generation, SQLite encryption helper).
- `DottoressaIorio.Repositories.Tests`: repository test suite.
- `DottoressaIorio.Services.Tests`: service test suite.
- `DottoressaIorio.E2E`: end-to-end test suite (Playwright + xUnit).
- `Setup`: Windows installer, service install/uninstall, backup scripts.
- `Selenium IDE`: Selenium IDE `.side` UI test flows.

## Tech Stack

- .NET `net10.0`
- ASP.NET Core Blazor Server
- Entity Framework Core + SQLite
- Optional Negotiate (Windows) authentication
- Serilog
- xUnit + Moq
- Microsoft Playwright (PDF rendering path)

## Prerequisites

- .NET SDK 10.x
- For Windows service packaging/installation: Windows x64 + Administrator permissions
- For installer build: Inno Setup 6 (`ISCC.exe`)

## Run Locally

```bash
dotnet restore DottoressaIorio.slnx
dotnet run --project DottoressaIorio.App
```

At startup the app applies EF migrations automatically (`Database.Migrate()`).

## Configuration

Configuration files:

- `DottoressaIorio.App/appsettings.json`
- `DottoressaIorio.App/appsettings.Development.json`
- `DottoressaIorio.App/appsettings.Production.json`

Important keys:

```json
{
  "EncryptionKey": "<hex-or-password-key>",
  "Authentication": {
    "UseNegotiate": true,
    "AllowedUsername": "DOMAIN\\username"
  }
}
```

Notes:

- `Authentication:UseNegotiate=false` disables Windows auth and allows all users.
- `Authentication:UseNegotiate=true` requires authenticated Windows identity.
- `Authentication:AllowedUsername` is enforced by the `SpecificUserOnly` policy used in layout.

## Database and Files

- Database files are created in the app base directory:
  - Encrypted path: `app.db`
  - Unencrypted path (when no encryption key): `app_unencrypted.db`
- Windows installer deploys app files under `C:\ProgramData\DottoressaIorio`.

## Testing

Run all tests:

```bash
dotnet test DottoressaIorio.slnx
```

Run non-integration tests only (same filter used in CI):

```bash
dotnet test DottoressaIorio.slnx --configuration Release --no-build --filter "Category!=Integration"
```

Run per test project:

```bash
dotnet test DottoressaIorio.Repositories.Tests
dotnet test DottoressaIorio.Services.Tests
dotnet test DottoressaIorio.E2E
```

CI workflow: `.github/workflows/ci.yml`

## Windows Installer Workflow

Main files:

- `Setup/build-installer.bat`
- `Setup/DottoressaIorioInstaller.iss`
- `DottoressaIorio.App/publish.bat`

Build installer:

```bat
Setup\build-installer.bat
```

What this does:

- Publishes self-contained `win-x64` app package.
- Creates `Setup\tmp\Service.7z` staging archive.
- Compiles Inno Setup installer to `Setup\Output\DottoressaIorioInstaller.exe`.
- Installer prompts for:
  - Allowed Username (`DOMAIN\username`)
  - Service URL (`http://host:port` or `https://host:port`)

Install-time behavior (from scripts):

- Installs app as Windows service `DottoressaIorio.App`.
- Writes configured username into `appsettings.Production.json`.
- Attempts Playwright Chromium install into `C:\ProgramData\DottoressaIorio\ms-playwright`.
- Creates scheduled backup task (`DottoressaIorio Daily Backup`) when possible.

## License

See `LICENSE`.
