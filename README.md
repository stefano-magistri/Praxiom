# Dottoressa Iorio Medical Management System

A medical practice management application built with **ASP.NET Core Blazor Server** for managing patients, therapies, templates, and PDF exports.

## Overview

This repository is organized as a multi-project .NET solution:

- `DottoressaIorio.App` – Blazor Server application (UI + hosting)
- `DottoressaIorio.Models` – domain entities
- `DottoressaIorio.Data` – EF Core `DbContext` + migrations
- `DottoressaIorio.Repositories` – data access repositories
- `DottoressaIorio.Services` – business/service layer (PDF + encryption)
- `DottoressaIorio.Repositories.Tests` – repository tests
- `DottoressaIorio.Services.Tests` – service tests

## Tech Stack

- .NET `net10.0`
- ASP.NET Core Blazor Server
- Entity Framework Core + SQLite (SQLCipher)
- Windows Authentication (Negotiate), configurable by environment
- Serilog logging
- xUnit / Moq for tests

## Getting Started

### Prerequisites

- .NET SDK supporting `net10.0`
- A supported OS/runtime for ASP.NET Core and SQLite

### Run the app

```bash
dotnet restore DottoressaIorio.slnx
dotnet run --project DottoressaIorio.App
```

## Configuration

Main settings are under:

- `DottoressaIorio.App/appsettings.json`
- `DottoressaIorio.App/appsettings.Development.json`
- `DottoressaIorio.App/appsettings.Production.json`

Example keys:

```json
{
  "Authentication": {
    "UseNegotiate": true,
    "AllowedUsername": "DOMAIN\\username"
  },
  "EncryptionKey": "your-encryption-key"
}
```

## Database

On startup, the app applies migrations (`Database.Migrate()`) and uses the configured SQLite database.

## Testing

Run all tests from repository root:

```bash
dotnet test DottoressaIorio.slnx
```

Or run test projects individually:

```bash
dotnet test DottoressaIorio.Repositories.Tests
dotnet test DottoressaIorio.Services.Tests
```

Continuous Integration: tests are also executed automatically in GitHub Actions on every pull request (`.github/workflows/ci.yml`).

## Deployment Notes

This repository includes helper scripts and binaries in:

- `Setup/`
- `Utility/`

Review and adapt these scripts for your target environment before production use.

### User-Friendly Installer (In Progress)

An Inno Setup script is available at `Setup/DottoressaIorioInstaller.iss`.

Build the installer with:

```bat
Setup\build-installer.bat 1.0.0
```

This will:

- Publish a self-contained `win-x64` service package
- Generate `Setup\Service.7z`
- Build `Setup\Output\DottoressaIorioInstaller.exe` (requires Inno Setup 6 / `ISCC.exe`)
- Prompt in the installer wizard for:
  - Allowed Username (`DOMAIN\username`)
  - Service URL (for example `http://localhost:5000`)

## Contributing

1. Create a feature/fix branch
2. Make and validate changes
3. Open a pull request with a clear summary and test results

## License

See `LICENSE`.
