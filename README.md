# Dottoressa Iorio Medical Management System

Blazor Server app used to manage patients, therapies, therapy templates, and therapy PDF export.

## Scope and Security Context

- Target environment: one doctor's personal Windows PC (Windows 10/11).
- Usage model: single user.
- Access control: Windows login only.
- Auth/authz in this app must remain:
  - `Authentication:UseNegotiate`
  - `Authentication:AllowedUsername`
  - `SpecificUserOnly` policy
- Out of scope: new app login screens, user management, roles, external identity providers, cloud auth.

## Solution Structure

- `DottoressaIorio.App`: Blazor Server host/UI, middleware, static assets.
- `DottoressaIorio.Models`: domain entities (`Patient`, `Therapy`, `TherapyTemplate`).
- `DottoressaIorio.Data`: EF Core `ApplicationDbContext` + migrations.
- `DottoressaIorio.Repositories`: repository/data-access layer.
- `DottoressaIorio.Services`: PDF generation and SQLite encryption helper.
- `DottoressaIorio.Repositories.Tests`: repository tests.
- `DottoressaIorio.Services.Tests`: service tests.
- `DottoressaIorio.E2E`: Playwright + xUnit end-to-end tests.
- `Setup`: installer/publish/install/uninstall/backup scripts.

## Tech Stack

- .NET `net10.0`
- ASP.NET Core Blazor Server
- Entity Framework Core + SQLite
- Negotiate authentication (configurable)
- Serilog
- Playwright (PDF rendering and E2E)
- xUnit + Moq

## Run Locally

```bash
dotnet restore DottoressaIorio.slnx
dotnet run --project DottoressaIorio.App
```

At startup the app applies EF migrations automatically (`Database.Migrate()`).

## Configuration

Files:

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

Behavior:

- `Authentication:UseNegotiate=false`: Negotiate auth is not enabled; policy permits access.
- `Authentication:UseNegotiate=true`: authenticated Windows identity is required.
- `Authentication:AllowedUsername`: enforced by `SpecificUserOnly`.

## Data and Storage

- DB file path is under app base directory.
- With `EncryptionKey`: `app.db` and `PRAGMA key` is applied.
- Without `EncryptionKey`: `app_unencrypted.db`.
- Entities are soft-deleted (`Deleted=true`), and query filters hide deleted rows.
- `TherapyTemplate.Title` is unique among non-deleted rows.

## Functional Areas

- Patients: create, edit, soft-delete, search, pagination.
- Therapies (per patient): create, edit, soft-delete, search, pagination.
- Therapy templates: create, edit, soft-delete, search, pagination.
- PDF export: therapy PDF generated with Playwright Chromium and rendered in a browser tab.

## Testing

Run all tests:

```bash
dotnet test DottoressaIorio.slnx
```

Run per project:

```bash
dotnet test DottoressaIorio.Repositories.Tests
dotnet test DottoressaIorio.Services.Tests
dotnet test DottoressaIorio.E2E
```

CI workflow: `.github/workflows/ci.yml`

## Windows Installer Workflow

Main files:

- `DottoressaIorio.App/publish.bat`
- `Setup/build-installer.bat`
- `Setup/install.bat`
- `Setup/uninstall.bat`
- `Setup/backup_script.ps1`

Build installer:

```bat
Setup\build-installer.bat
```

Current scripted behavior:

- Publishes self-contained `win-x64` output.
- Creates `Setup\tmp\Service.7z`.
- Builds `Setup\Output\DottoressaIorioInstaller.exe`.
- Installer flow sets allowed username and service URL.
- Installation deploys to `C:\ProgramData\DottoressaIorio`.
- Creates/starts service `DottoressaIorio.App`.
- Attempts Playwright Chromium install to `C:\ProgramData\DottoressaIorio\ms-playwright`.
- Attempts scheduled backup task creation (`DottoressaIorio Daily Backup`).

## Detailed Process Reference

For implementation-level process documentation, see `REPO_PROCESS.md`.

## License

See `LICENSE`.
