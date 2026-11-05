# DottoressaIorio.Data

## Purpose

`DottoressaIorio.Data` contains the EF Core database context, migration history, and model-level persistence rules.

## Current rules

- `ApplicationDbContext` applies global soft-delete filters.
- `TherapyTemplate.Title` is unique for non-deleted rows.
- `ClinicSettings` is seeded as a singleton default row.
- Entity normalization and validation happen during `SaveChanges` and `SaveChangesAsync`.
- Soft-delete-only updates bypass full validation so legacy-invalid rows can still be deleted.

## Key entry points

- `ApplicationDbContext.cs`
- `ApplicationDbContextFactory.cs`
- `Migrations/`

## Notes for contributors

- Startup migrations are triggered by `DottoressaIorio.App`.
- Keep model rules here aligned with repository and service assumptions.
- This project does not own installer or encryption-key resolution behavior; that lives in `DottoressaIorio.Services` and `Setup`.
