# Repository Process (Observed)

This document describes only behavior implemented in this repository.

## Scope and Security Context (Non-Negotiable)

1. Target environment is a single doctor's personal PC running Windows 10 or Windows 11.
2. Usage model is single-user (the doctor is the only person who uses/logs into the machine).
3. Access control is Windows login only (local or AD account on that PC).
4. The app is an internal admin tool for that single user; it is not multi-tenant and not public-facing.
5. Current authentication/authorization behavior must remain unchanged:
   - `Authentication:UseNegotiate`
   - `Authentication:AllowedUsername`
   - `SpecificUserOnly` policy
6. No new authentication complexity is in scope:
   - no new login screens
   - no user management
   - no roles
   - no external identity providers
   - no cloud authentication
7. Any security improvements beyond this baseline must be discussed and approved first.
8. Future commercialization is out of scope for current changes.

## 1. Startup Process

1. The app builds configuration and checks whether it is running as a Windows Service.
2. If running as a service on Windows, it enables `UseWindowsService()`.
3. It reads:
   - `EncryptionKey`
   - `Authentication:UseNegotiate`
   - `Authentication:AllowedUsername`
4. It configures logging via Serilog.
5. It configures authorization and optional Negotiate authentication.
6. It registers Razor Pages and Blazor Server.
7. It registers SQLite encryption connection setup, repositories, and PDF services.
8. On startup, it creates a scope and executes `dbContext.Database.Migrate()`.
9. Middleware pipeline:
   - exception handler + HSTS in non-development
   - HTTPS redirection only when not running as Windows service
   - static files, routing
   - authentication (only when Negotiate is enabled)
   - authorization
10. It maps Blazor hub and fallback page.

## 2. Authentication and Authorization Process

1. If `Authentication:UseNegotiate=true`, Negotiate auth is added.
2. Authorization policy `SpecificUserOnly` is always added:
   - if Negotiate is disabled: allows all users
   - if Negotiate is enabled: requires authenticated identity name equal to `Authentication:AllowedUsername` (case-insensitive)
3. Fallback policy:
   - if Negotiate disabled: allows requests
   - if Negotiate enabled: requires authenticated user
4. Main layout content is rendered through `AuthorizeView` with policy `SpecificUserOnly`.

## 3. Data Persistence Process

1. `ApplicationDbContext` has `DbSet`s for:
   - `Patients`
   - `Therapies`
   - `TherapyTemplates`
2. Global EF query filters exclude soft-deleted rows (`Deleted == true`) for all three entities.
3. `TherapyTemplate.Title` has a unique index filtered to non-deleted rows (`"Deleted" = 0`).
4. `SaveChanges`/`SaveChangesAsync`:
   - normalizes tracked added/modified entities
   - validates entities with DataAnnotations
   - skips normalization/validation for soft-delete-only updates (`Deleted`/`EditDate` only)

### Normalization Rules Implemented

- `Patient`
  - required strings (`FirstName`, `LastName`) are trimmed, null becomes empty string
  - optional strings are trimmed, whitespace-only becomes `null`
  - email is lowercased after optional normalization
  - phone keeps optional leading `+` and strips non-digits; empty result becomes `null`
- `Therapy`
  - `Description` normalized as required string
- `TherapyTemplate`
  - `Title` and `Description` normalized as required strings

## 4. Repository and Unit-of-Work Process

1. Generic repository behavior:
   - `AddAsync`: sets `CreatedDate = DateTime.Now`, adds entity
   - `UpdateAsync`: sets `EditDate = DateTime.Now`, marks entity modified
   - `DeleteAsync`: soft-delete only (`Deleted = true`, `EditDate = DateTime.Now`)
2. Generic `GetByIdAsync` detaches entity before returning.
3. `EfUnitOfWork`:
   - `SaveChangesAsync` catches `DbUpdateException`, detaches failed entries, rethrows
   - `ExecuteInTransactionAsync` wraps action in DB transaction, saves, commits, same detach-on-failure behavior

### Specialized Repository Behavior

- `PatientRepository`
  - ordered list by last name then first name
  - search by first name/last name/full name using lowercase token matching
  - duplicate check on normalized first name + last name + date of birth
- `PatientTherapyRepository`
  - get therapies for a patient ordered by `CreatedDate`
- `TherapyTemplateRepository`
  - ordered list by title
  - title existence check by normalized lowercase title

## 5. UI Navigation and CRUD Process

### Main routes implemented

- `/` patients list
- `/create-patient`
- `/patient/{id}/edit/`
- `/patient/{id}/delete/`
- `/patient/{patientId}/`
- `/patient/{id}/create-therapy`
- `/patient/{id}/edit-therapy`
- `/patient/{id}/delete-therapy`
- `/therapy-templates`
- `/create-therapy-template`
- `/therapy-template/{id}/edit`
- `/therapy-template/{id}/delete`
- `/guide`

### Patients process

1. Patients list supports search and pagination.
2. Create patient:
   - warns if patient already exists (same normalized first/last name + DOB)
   - on save success navigates to patient detail page
3. Edit patient updates entity and saves.
4. Delete patient runs a transaction:
   - soft-deletes all therapies of patient
   - soft-deletes patient
   - navigates back to `/`

### Therapies process

1. Patient detail page loads patient + therapies.
2. Therapies list supports search and pagination.
3. Create therapy:
   - optional precompiled template selection fills description
   - saves therapy linked to patient
4. Edit therapy updates description.
5. Delete therapy soft-deletes therapy.

### Therapy templates process

1. Template list supports search and pagination.
2. Create template checks for duplicate title before save.
3. Edit template updates values.
4. Delete template soft-deletes template.

## 6. PDF Generation Process

1. Patient therapy page can request PDF download.
2. JS prepares a new browser tab (`jsPreparePdfTab`).
3. Backend PDF service:
   - creates Playwright Chromium browser (headless)
   - builds therapy HTML body
   - loads header/footer templates from `_pdf_therapy/header.html` and `_pdf_therapy/footer.html`
   - renders A4 PDF with configured margins and header/footer
4. Frontend JS injects PDF data URL into iframe in target tab (`jsSaveAsFile`).
5. On failure, prepared tab is closed (`jsClosePreparedPdfTab`).

## 7. SQLite Connection/Encryption Process

1. App registers `SqlLiteDbEncryptionService` as singleton.
2. DB connection behavior:
   - if `EncryptionKey` is empty/null -> uses `app_unencrypted.db`
   - if `EncryptionKey` is set -> uses `app.db` and executes `PRAGMA key = '<key>'`
3. DB files are placed under app base directory (`AppContext.BaseDirectory`).

## 8. Logging Process

1. Logging providers are cleared.
2. Serilog writes:
   - console output
   - rolling daily file logs at `C:\ProgramData\DottoressaIorio\logs\DottoressaIorio.App.log`
3. Minimum levels:
   - default `Information`
   - `Microsoft` overridden to `Warning`

## 9. Validation Error Message Process

1. `ValidationException` -> returns validation message.
2. SQLite unique constraint failure on `TherapyTemplates.Title` -> `"Il titolo gia esiste."`
3. Other exceptions -> generic save failure message.

## 10. Build, Install, and Uninstall Process

### Publish/package

1. `DottoressaIorio.App/publish.bat`:
   - runs self-contained publish (`win-x64`)
   - reads executable file version
   - writes version file
   - builds `Setup/tmp/Service.7z`
2. `Setup/build-installer.bat`:
   - invokes publish script
   - resolves version
   - compiles Inno Setup script
   - outputs installer executable

### Install (`Setup/install.bat`)

1. Requires admin rights.
2. Parses required args including allowed username and app URL.
3. Stops/removes existing Windows service if present.
4. Backs up existing DB files.
5. Extracts `Service.7z` into `C:\ProgramData\DottoressaIorio`.
6. Restores DB backup files.
7. Copies backup script.
8. Sets/creates Playwright browser path under ProgramData and attempts Chromium install.
9. Updates `appsettings.Production.json` with provided allowed username.
10. Creates and starts Windows service `DottoressaIorio.App`.
11. Attempts to create scheduled backup task.

### Uninstall (`Setup/uninstall.bat`)

1. Requires admin rights.
2. Removes scheduled backup task.
3. Stops and deletes service if present.
4. By default preserves DB files in `_db_backup`.
5. Optional flag `--remove-all-data` removes all app data and installation directory.

### Backup script (`Setup/backup_script.ps1`)

1. Requires elevated PowerShell.
2. Stops service if running.
3. Copies `app.db` to local backup folder with timestamp.
4. Keeps last 360 local backups.
5. If OneDrive is available, also writes backup there and applies same retention.
6. Restarts service if it was running.
