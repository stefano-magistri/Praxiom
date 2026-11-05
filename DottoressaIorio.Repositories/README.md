# DottoressaIorio.Repositories

## Purpose

`DottoressaIorio.Repositories` provides the persistence boundary between services/UI code and `ApplicationDbContext`.

## Responsibilities

- Generic CRUD and soft-delete operations
- Patient, therapy, and therapy-template queries
- Singleton clinic-settings access
- Unit-of-work save and transaction coordination

## Key entry points

- `GenericRepository.cs`
- `PatientRepository.cs`
- `PatientTherapyRepository.cs`
- `TherapyTemplateRepository.cs`
- `ClinicSettingsRepository.cs`
- `EfUnitOfWork.cs`
- `RepositoryExtensions.cs`

## Notes for contributors

- `GenericRepository.DeleteAsync` uses a key-only soft-delete path so records can be deleted without revalidating all fields.
- `GetByIdAsync` returns detached entities.
- `EfUnitOfWork` detaches failed entries after `DbUpdateException` so later saves on the same context can proceed.
