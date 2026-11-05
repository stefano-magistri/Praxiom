# Setup

## Purpose

The `Setup` folder contains the publish, installer, uninstall, backup, and restore assets for the Windows deployment flow.

## Files

- `build-installer.bat`: builds the published app package, publishes the license helper, and invokes Inno Setup
- `install.bat`: installs or upgrades the app into `C:\ProgramData\Praxiom`
- `uninstall.bat`: removes the service and deployed files while preserving backups/logs
- `backup_script.ps1`: manual cold-copy backup fallback
- `restore_script.ps1`: restore helper for a selected backup file
- `PraxiomInstaller.iss`: Inno Setup wizard and orchestration

## Current behavior

- Installer writes the encryption key through the bundled helper into the installed secret file.
- Installer can validate `license.dat` before copying it.
- Installer recreates the Windows service and schedules `DottoressaIorio.App.exe --online-backup`.
- Installer restore runs after installation only when the user explicitly selects a backup file.
- Uninstall preserves the local backup folder and also attempts an extra OneDrive copy when available.

## Notes for contributors

- Treat these files as behavior-sensitive deployment assets.
- Do not change argument names, exit-code meanings, or the service/install order casually; the Inno Setup script and helper tooling depend on them.
