# DottoressaIorio.Services

## Purpose

`DottoressaIorio.Services` contains the application behaviors that sit above repositories and below the UI.

## Main areas

- `Licensing/`: local license validation, trial state, creation limits
- `SqlLiteEncryption/`: encryption-key resolution and SQLite connection setup
- `Pdf/`: therapy HTML rendering and Playwright PDF generation
- `Backup/`: online backup scheduling, retention, and fallback recommendation logic
- `Patients/`: CSV export and patient age calculation
- `RichTextFormatting.cs`: supported rich-text normalization and sanitization
- `Therapies/`: therapy draft/template application helpers

## Notes for contributors

- Development and non-Development encryption behavior are intentionally different.
- PDF generation depends on Playwright Chromium and installed template files under `_pdf_therapy`.
- Trial state is stored through `ClinicSettings`, so license logic depends on the DB being migrated first.
- Online backup is the routine path; the cold-copy script in `Setup` is fallback-only.
