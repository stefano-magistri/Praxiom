# DottoressaIorio.App

## Purpose

`DottoressaIorio.App` is the Blazor Server host for the application. It owns startup, environment-sensitive wiring, auth/authz registration, endpoint mapping, and UI pages/components.

## Startup flow

1. Initialize SQLCipher support.
2. Build the host and resolve startup flags.
3. Resolve the encryption key and auth configuration.
4. Register logging, auth/authz, repositories, services, PDF generation, licensing, and backup services.
5. Optionally load the demo database.
6. Build the app.
7. If `--online-backup` is supplied, run one backup and exit.
8. Run automatic EF migrations.
9. Initialize the license state.
10. Configure lifetime backup hooks and the HTTP pipeline.

## Key configuration and entry points

- `Program.cs`: main startup and runtime ordering
- `Middleware/AuthenticationExtensions.cs`: conditional Negotiate registration
- `Middleware/AuthorizationExtensions.cs`: `SpecificUserOnly` and fallback policy behavior
- `Middleware/PdfEndpointsExtensions.cs`: PDF endpoint
- `Middleware/PatientExportEndpointsExtensions.cs`: CSV export endpoint
- `Middleware/DemoDatabaseExtensions.cs`: `--demo` behavior
- `appsettings*.json`: environment configuration

## Notes for contributors

- Keep auth/authz behavior exactly aligned with the current `UseNegotiate`, `AllowedUsername`, and `SpecificUserOnly` implementation.
- Migrations run automatically at startup before license initialization.
- Do not move installer, backup, or PDF assumptions out of sync with the service and setup flow without updating `Setup`.
