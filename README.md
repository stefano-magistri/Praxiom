# Dottoressa Iorio Medical Management System

[![CI](https://github.com/stefano-magistri/DottoressaIorio/actions/workflows/ci.yml/badge.svg)](https://github.com/stefano-magistri/DottoressaIorio/actions/workflows/ci.yml)

A private medical management application built for a single doctor workflow on Windows.

It helps Dr. Iorio manage patients, therapies, and reusable therapy templates, then generate printable therapy PDFs directly from the same interface. The project exists to keep day-to-day clinical administration fast, local-first, and maintainable without introducing unnecessary platform complexity.

## Quick Commands

```bash
dotnet restore DottoressaIorio.slnx
dotnet build DottoressaIorio.slnx --configuration Release
dotnet test DottoressaIorio.slnx --configuration Release

# Run app locally
dotnet run --project DottoressaIorio.App

# Build installer (Windows)
Setup\build-installer.bat
```

## Highlights

- Single-user workflow optimized for one doctor on one Windows machine.
- Fast CRUD and search flows for patients, therapies, and therapy templates.
- Therapy PDF generation through Playwright Chromium for consistent printing output.
- Optional encrypted SQLite storage (`EncryptionKey`) with unencrypted fallback.
- Windows service + installer scripts for repeatable deployment and upgrades.
- Soft-delete model to preserve records while keeping UI lists clean.

## Architecture Overview

The application follows a layered structure:

`Blazor UI -> Services -> Repositories -> EF Core DbContext -> SQLite`

- **UI (`DottoressaIorio.App`)**: Blazor Server pages/components, routing, middleware, and startup wiring.
- **Services (`DottoressaIorio.Services`)**: cross-cutting application logic (PDF generation, SQLite encryption wiring).
- **Repositories (`DottoressaIorio.Repositories`)**: persistence access and transaction workflows.
- **Data (`DottoressaIorio.Data`)**: EF Core model configuration, migrations, and `ApplicationDbContext`.
- **Models (`DottoressaIorio.Models`)**: domain entities shared across layers.

Supporting projects:

- `DottoressaIorio.Repositories.Tests`, `DottoressaIorio.Services.Tests`: unit/integration coverage.
- `DottoressaIorio.E2E`: Playwright + xUnit end-to-end test coverage.
- `Setup`: publish/install/uninstall/backup automation.

## Engineering Decisions

### Why Blazor Server

- Fits a private LAN/local-hosted internal tool.
- Keeps deployment simple (single .NET app) and reduces frontend stack overhead.
- Enables rapid form-heavy admin UI development in C#.

### Why SQLite

- Operationally lightweight for a single-user desktop/server deployment.
- Easy backup/restore and simple installer integration.
- Works well with EF Core migrations and local file-based storage.

### Why Playwright for PDF

- Uses Chromium rendering for predictable layout of print-ready medical therapy documents.
- Reuses HTML/CSS templates instead of maintaining separate document rendering technology.
- Also supports E2E tests in the same ecosystem.

## Usage Flow

1. Open patient list (`/`) and use search/pagination.
2. Create or edit patient records.
3. Manage therapies for each patient.
4. Optionally apply a therapy template to speed authoring.
5. Export therapy to PDF and open in a browser tab for print/save.

### Screenshots (Placeholders)

- `Patients list`: add image at `docs/screenshots/patients-list.png`
- `Patient therapy detail`: add image at `docs/screenshots/patient-therapy.png`
- `Therapy PDF preview`: add image at `docs/screenshots/pdf-preview.png`

## Scope and Security Context

This repository intentionally targets a constrained scenario:

- Single doctor, single Windows PC (Windows 10/11).
- Access model based on Windows login only.
- Existing auth/authz behavior is intentionally limited to:
  - `Authentication:UseNegotiate`
  - `Authentication:AllowedUsername`
  - `SpecificUserOnly` policy

Out of scope for this project baseline:

- New app login screens.
- Multi-user role management.
- External identity providers or cloud authentication flows.

## Configuration

Main files:

- `DottoressaIorio.App/appsettings.json`
- `DottoressaIorio.App/appsettings.Development.json`
- `DottoressaIorio.App/appsettings.Production.json`

Relevant keys:

```json
{
  "EncryptionKey": "<key>",
  "Authentication": {
    "UseNegotiate": true,
    "AllowedUsername": "DOMAIN\\username"
  }
}
```

Behavior summary:

- `Authentication:UseNegotiate=false`: Negotiate auth middleware is disabled.
- `Authentication:UseNegotiate=true`: Windows-authenticated identity is required.
- `Authentication:AllowedUsername`: enforced by `SpecificUserOnly` policy.

## Data and Storage

- Database files are created under app base directory (`AppContext.BaseDirectory`).
- With `EncryptionKey`: app uses `app.db` and applies `PRAGMA key`.
- Without `EncryptionKey`: app uses `app_unencrypted.db`.
- Soft-delete is used for entities (`Deleted=true`) with global EF filters.
- `TherapyTemplate.Title` is unique among non-deleted rows.

## Build, Test, and CI

Run all tests:

```bash
dotnet test DottoressaIorio.slnx
```

Run test projects directly:

```bash
dotnet test DottoressaIorio.Repositories.Tests
dotnet test DottoressaIorio.Services.Tests
dotnet test DottoressaIorio.E2E
```

CI workflow:

- [`.github/workflows/ci.yml`](.github/workflows/ci.yml)

## Installer Workflow

Key scripts:

- `DottoressaIorio.App/publish.bat`
- `Setup/build-installer.bat`
- `Setup/install.bat`
- `Setup/uninstall.bat`
- `Setup/backup_script.ps1`

Scripted installer behavior includes:

- self-contained `win-x64` publish,
- installer executable creation,
- deployment to `C:\ProgramData\DottoressaIorio`,
- Windows service setup/start,
- Playwright Chromium installation attempt,
- scheduled online backup task setup (`DottoressaIorio.App.exe --online-backup`).

Backup modes:

- Routine mode: online SQLite backup through the scheduled task and app-triggered actions (service stays running).
- Fallback mode: `Setup/backup_script.ps1` performs stop-service cold copy and is retained for manual/emergency use only.

## Contributing and Maintenance

Contribution and maintenance workflow is documented in [CONTRIBUTING.md](CONTRIBUTING.md).

## License

See [LICENSE](LICENSE).
