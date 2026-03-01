# Contributing Guide

This document defines the maintenance and contribution workflow for `DottoressaIorio`.

For project purpose, architecture, and feature overview, start from [README.md](README.md).

## Project Constraints (Must Preserve)

The repository currently targets a strict scope:

- Single doctor, single Windows PC (Windows 10/11).
- Internal/private usage only.
- Current access model must remain based on Windows identity configuration:
  - `Authentication:UseNegotiate`
  - `Authentication:AllowedUsername`
  - `SpecificUserOnly` policy

Changes that introduce multi-user auth flows, cloud identity providers, role systems, or productization concerns require explicit maintainers' approval before implementation.

## Branching and Versioning

### Branch Naming

Use descriptive branch names:

- `feature/<short-topic>` for new features.
- `fix/<short-topic>` for bug fixes.
- `chore/<short-topic>` for maintenance/docs/infra.
- `release/<version>` for release stabilization.

Examples:

- `feature/template-search-improvements`
- `fix/pdf-header-overflow`
- `chore/update-ci-cache`

### Version Discipline

This repository uses release branches (for example `release/version-2.0.0`).

When preparing changes, explicitly assess whether the work impacts:

- app behavior,
- database schema/migrations,
- installer scripts,
- deployment/service behavior,
- backup/restore compatibility.

Document impact in the PR summary even when the answer is "no impact".

## Approved Workflow

1. Branch from the active release or target branch.
2. Make focused changes with clear scope.
3. Run required validation commands locally.
4. Commit in logical units.
5. Open PR to the correct target branch.
6. Include impact assessment and testing evidence.

## Quality Gates and Testing

### Required Commands

Run these at minimum before opening a PR:

```bash
dotnet restore DottoressaIorio.slnx
dotnet build DottoressaIorio.slnx --configuration Release
dotnet test DottoressaIorio.slnx --configuration Release
```

If your change affects installer, packaging, or deployment scripts, also validate:

```bat
Setup\build-installer.bat
```

### Test Scope Expectations

- Repository/data layer changes: update/add repository tests.
- Service/PDF/encryption behavior changes: update/add service tests.
- UI navigation/critical user flows changes: verify E2E tests and add where needed.
- Migration-related changes: verify startup migration path and compatibility with existing DB backups.

## Impact Assessment Checklist (PR)

Every PR should include a short section like this:

- Behavior change: `yes/no` and what changed.
- DB schema/migration change: `yes/no` and migration name.
- Installer/deployment impact: `yes/no` and affected scripts.
- Config changes required: `yes/no` and keys.
- Security/auth scope impact: `yes/no` and rationale.
- Backward compatibility notes: brief statement.

## Commit and PR Conventions

### Commits

- Keep commits small and purposeful.
- Use imperative, scope-oriented messages.
- Prefer one topic per commit.

Examples:

- `docs: restructure README for portfolio overview`
- `test: add coverage for therapy title duplicate validation`
- `fix: preserve edit timestamp on soft delete`

### Pull Requests

PR descriptions should contain:

- Summary of changes.
- Why the change is needed.
- Impact assessment checklist.
- Evidence of tests run (command list and pass/fail outcome).
- Screenshots if UI-visible behavior changed.

## Review Expectations

Reviewers should verify:

- Scope alignment with project constraints.
- Correctness and regressions, especially around patient/therapy persistence.
- Authentication/authorization behavior remains within approved model.
- Migration and installer safety when touched.
- Adequate automated test coverage.
- Clear rollback path for risky operational changes.

## Security and Privacy Notes

- Avoid introducing unnecessary data exposure in logs.
- Preserve current protected deployment assumptions.
- Discuss any security model expansions before coding them.

## Documentation Maintenance

When behavior changes, update:

- [README.md](README.md) for user-facing or architectural understanding.
- this file (`CONTRIBUTING.md`) for workflow/maintenance policy changes.
- setup or operations docs/scripts when deployment behavior changes.