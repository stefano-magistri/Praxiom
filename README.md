# Praxiom Medical Management System
**Developer portfolio:** [Stefano Magistri](https://stefano-magistri-dotnet.topjaguar.chatgpt.site/)

Praxiom is a private Windows-first medical management application for a single-doctor workflow. The solution manages patients, therapies, reusable therapy templates, printable therapy PDFs, local licensing, and installer-driven deployment.


## Application screenshots

These are the four application screenshots featured in my [professional portfolio](https://stefano-magistri-dotnet.topjaguar.chatgpt.site/).

### Patient management

![Patient management screen with search and patient records](https://stefano-magistri-dotnet.topjaguar.chatgpt.site/assets/praxiom-patients.png)

### Patient details and therapies

![Patient information and associated therapies](https://stefano-magistri-dotnet.topjaguar.chatgpt.site/assets/praxiom-details.png)

### Therapy editor

![Rich-text therapy editor](https://stefano-magistri-dotnet.topjaguar.chatgpt.site/assets/praxiom-editor.png)

### PDF document generation

![Generated therapy PDF in a document viewer](https://stefano-magistri-dotnet.topjaguar.chatgpt.site/assets/praxiom-pdf.png)

## Solution structure

- `DottoressaIorio.App`: Blazor Server UI, startup wiring, auth/authz, HTTP endpoints, and app host.
- `DottoressaIorio.Models`: shared domain entities and validation attributes.
- `DottoressaIorio.Data`: EF Core `ApplicationDbContext`, entity rules, and migrations.
- `DottoressaIorio.Repositories`: persistence access and unit-of-work boundary.
- `DottoressaIorio.Services`: encryption, licensing, PDF generation, backup, CSV export, and rich-text handling.
- `DottoressaIorio.E2E`: Playwright end-to-end tests.
- `DottoressaIorio.Repositories.Tests`, `DottoressaIorio.Services.Tests`, `DottoressaIorio.App.Tests`: unit and integration coverage.
- `Setup`: publish, installer, uninstall, backup, and restore flow.

## Prerequisites

- Windows for normal local development, installer work, and E2E runs
- .NET SDK 10.x
- Playwright Chromium for PDF generation and E2E tests
- Inno Setup 6 to build the installer

## Local development

Restore, build, and test the full solution:

```powershell
dotnet restore DottoressaIorio.slnx
dotnet build DottoressaIorio.slnx
dotnet test DottoressaIorio.slnx
```

Run the app locally:

```powershell
dotnet run --project DottoressaIorio.App
```

Useful startup flags:

- `--demo`: copies `Demo/app_old.db` into the active database root before startup
- `--online-backup`: runs one online backup and exits
- `--trial-active`: forces active trial mode
- `--trial-expired`: forces expired trial mode

## Configuration and auth behavior

Primary config files:

- `DottoressaIorio.App/appsettings.json`
- `DottoressaIorio.App/appsettings.Development.json`
- `DottoressaIorio.App/appsettings.Production.json`

Relevant auth keys:

```json
{
  "Authentication": {
    "UseNegotiate": true,
    "AllowedUsername": "DOMAIN\\username"
  }
}
```

Current behavior is exactly:

- `Authentication:UseNegotiate=true`: Negotiate authentication is registered and `SpecificUserOnly` allows only the configured `Authentication:AllowedUsername`.
- `Authentication:UseNegotiate=false`: authentication middleware is not registered, `SpecificUserOnly` returns `true`, and the fallback policy also allows access.
- `Authentication:AllowedUsername`: used both by authorization and by local license validation.

This repository intentionally preserves the current limited auth/authz model. There is no separate application login screen, no roles, and no external identity provider flow.

## Database behavior

- The app uses SQLite through EF Core.
- Database files live under `DatabaseRootPath` when configured; otherwise the root is `AppContext.BaseDirectory` for Windows service runs and the app content root for normal runs.
- `dbContext.Database.Migrate()` runs automatically during startup before license initialization.
- `ApplicationDbContext` applies global soft-delete filters for `Patient`, `Therapy`, and `TherapyTemplate`.
- `TherapyTemplate.Title` is unique among non-deleted rows.
- `ClinicSettings` is treated as a singleton row and seeded with defaults.
- Entity normalization and validation run inside `SaveChanges` and `SaveChangesAsync`.

## Encryption key behavior

- Encryption is handled by `SqlLiteDbEncryptionService` plus `EncryptionKeyResolver`.
- In `Development`, the app accepts `EncryptionKey` from local configuration if present.
- Outside `Development`, the app requires an installed local key file.
- Default installed key path:

```text
C:\ProgramData\Praxiom\secrets\encryption-key.json
```

- The installed secret can be plain JSON with `EncryptionKey`, or a DPAPI LocalMachine-wrapped payload written by the setup helper.
- With a non-empty key the app uses `app.db` and applies `PRAGMA key`.
- Without a key the app uses `app_unencrypted.db`.

## Licensing and trial behavior

- Runtime license validation uses `Authentication:AllowedUsername` plus the local machine identifier.
- Default installed license path:

```text
C:\ProgramData\Praxiom\license.dat
```

- In plain `Development`, with no launch override, the app behaves as licensed.
- Outside that case, startup validates the local license file.
- If validation fails, the app falls back to trial mode and stores `TrialStartUtc` in `ClinicSettings`.
- Trial rules currently implemented:
  - 7-day trial
  - max 10 patients
  - max 10 therapy templates
  - therapy creation blocked only when the trial has expired

## PDF generation

- Therapy PDFs are generated with Playwright Chromium.
- Header and footer templates are loaded from `DottoressaIorio.App/_pdf_therapy`.
- In Production on Windows, if `PLAYWRIGHT_BROWSERS_PATH` is not already set, the app uses:

```text
C:\ProgramData\Praxiom\ms-playwright
```

- Non-Production runs attempt Playwright browser installation on demand if Chromium is missing.

## Backup and restore

- Online backup runs through `OnlineBackupService`, which uses SQLite online backup rather than a stop-service cold copy.
- Backup triggers exist for:
  - scheduled task mode (`--online-backup`)
  - application startup
  - application shutdown
  - selected app actions such as PDF generation and data updates
- Local backup folder:

```text
C:\ProgramData\Praxiom\_db_backup
```

- Backup retention, debounce windows, and fallback recommendation logic are controlled by `OnlineBackupOptions`.
- When available, backups are also mirrored to OneDrive under `Praxiom\_db_backup`.
- `Setup/backup_script.ps1` remains the manual cold-copy fallback path.
- `Setup/restore_script.ps1` stops the service, restores the selected backup into the active DB path, removes WAL/SHM files, and creates a pre-restore snapshot.

## Tests

Main test commands:

```powershell
dotnet test DottoressaIorio.slnx
dotnet test DottoressaIorio.slnx --configuration Release --no-build --filter "Category!=Integration"
```

Project-specific examples:

```powershell
dotnet test DottoressaIorio.Repositories.Tests
dotnet test DottoressaIorio.Services.Tests
dotnet test DottoressaIorio.App.Tests
dotnet test DottoressaIorio.E2E
```

E2E tests:

- start the built app from its DLL output
- generate a temporary signed local license
- force `Authentication__UseNegotiate=false`
- use a temporary database root
- require Windows because machine identity and registry `MachineGuid` are part of the license flow

## Publish and installer overview

Build the published app package:

```powershell
.\DottoressaIorio.App\publish.bat
```

Build the installer:

```powershell
.\Setup\build-installer.bat
```

Current installer flow:

1. Publish the app as self-contained `win-x64`.
2. Publish the license helper.
3. Build the Inno Setup installer.
4. Installer collects the current Windows user and machine id.
5. Installer optionally validates a provided `license.dat`.
6. Installer writes the encryption key secret into `C:\ProgramData\Praxiom\secrets`.
7. Installer deploys files, recreates the Windows service, installs Playwright Chromium, and creates the scheduled online-backup task.
8. If database restore is requested, the restore script runs after install setup completes.

## Troubleshooting

- Missing encryption key outside `Development`: verify `C:\ProgramData\Praxiom\secrets\encryption-key.json`.
- Trial mode unexpectedly active: verify `Authentication:AllowedUsername`, the installed `license.dat`, and the current Windows machine/user match.
- PDF generation fails on installed environments: verify Chromium exists under `C:\ProgramData\Praxiom\ms-playwright` or rerun Playwright install.
- Service start failures during install: inspect `Setup\install.log` and Windows event logs collected by the installer diagnostics step.
- E2E startup failures: build `DottoressaIorio.App` first and ensure Windows identity/registry access is available.

## Additional project notes

- [DottoressaIorio.App/README.md](c:\repos\DottoressaIorio\DottoressaIorio.App\README.md)
- [DottoressaIorio.Data/README.md](c:\repos\DottoressaIorio\DottoressaIorio.Data\README.md)
- [DottoressaIorio.Repositories/README.md](c:\repos\DottoressaIorio\DottoressaIorio.Repositories\README.md)
- [DottoressaIorio.Services/README.md](c:\repos\DottoressaIorio\DottoressaIorio.Services\README.md)
- [DottoressaIorio.E2E/README.md](c:\repos\DottoressaIorio\DottoressaIorio.E2E\README.md)
- [Setup/README.md](c:\repos\DottoressaIorio\Setup\README.md)
