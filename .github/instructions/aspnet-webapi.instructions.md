---
description: Guidance for the ASP.NET Core Web API backend in src/backend
applyTo: "src/backend/**"
---

# ASP.NET Core Web API Instructions

These instructions apply to the backend project under `src/backend`.

## Instruction Consistency
- After making backend changes, review this file and update it if guidance is no longer accurate.
- Keep the instruction set aligned with the current architecture and dependencies.

## Spec Authority
- All implementation must reference and conform to the authoritative specs in `docs/specs/`.
- SPEC-010 defines domain model, identity rules, and computation logic.
- SPEC-110 defines API surface, endpoints, and DTO contracts.
- SPEC-120 defines database schema, column types, constraints, and indexes.
- SPEC-200 defines Teams/Graph integration, delegated data sync, and drift detection.
- SPEC-300 defines metrics engine computation and transaction boundaries.
- If a required rule is missing in a spec, add `TODO-SPEC` comment and stop.

## Architecture
- Use a minimal API style unless the feature requires controllers.
- Keep endpoints thin: delegate logic to domain and application services.
- Organize code by feature (vertical slices), not by technical layer.
- Avoid cross-cutting dependencies between features; share via interfaces.

## Project Structure
- `Program.cs` for startup and DI registration.
- `Features/<FeatureName>/` for endpoints, DTOs, handlers, validators.
- `Domain/` for core entities, value objects, domain rules (identity, normalization, warm/influence logic).
- `Infrastructure/` for data access, external integrations, providers.
- `Infrastructure/Graph/` for TeamsGraphClient, OBO token service.
- `Metrics/` for recompute orchestrator per SPEC-300.
- `Common/` for shared primitives, errors, and result types.
- Tests live in `tests/backend/` mirroring feature folders.

## Core Dependencies
- Target framework: .NET 10 (`net10.0`).
- Use built-in DI, configuration, and logging.
- Prefer `Microsoft.Extensions.*` abstractions over concrete libs.
- EF Core with explicit migrations per SPEC-120.
- Microsoft.Identity.Web for Entra ID JWT validation and OBO token acquisition.
- Microsoft.Graph SDK for Teams webinar operations.

## API Design
- Base path: `/api/v1` per SPEC-110.
- Use resource-oriented routes per SPEC-110 endpoint definitions.
- Use proper HTTP verbs and status codes per SPEC-110 DTO contracts.
- Validate inputs and return problem details (SPEC-110 error envelope).
- Use DTOs for request/response; never expose domain entities directly.
- Prefer async APIs and cancellation tokens for I/O.
- No pagination in V1 — list endpoints return all results for authenticated user.

## Microsoft Graph Integration
- Delegated-only permission model per SPEC-200:
  - All Graph operations use OBO flow — `VirtualEvent.ReadWrite` (delegated)
  - No application permissions required
- Centralized token acquisition service (TeamsGraphClient).
- OBO tokens used for all Graph operations — user must be authenticated.
- Data sync (registrations, attendance) triggered on page load via delegated token.

## Data Schema
- Follow SPEC-120 for all table definitions, column types, constraints, indexes.
- UUID primary keys, UTC datetime2, EF Core migrations only.
- JSON columns (nvarchar(max)) for warm account lists per SPEC-120/SPEC-300.
- Session.status: Draft | Published (not Reconciled).
- Session.reconcileStatus: Synced | Reconciling.
- Domain normalization: eTLD+1 / public-suffix-aware registrable domain parsing per SPEC-010.
- Internal domain exclusion list: sourced from validated environment/config setting.

## Security and Configuration
- Keep secrets in user secrets or environment variables.
- Avoid committing `appsettings.*.json` for local overrides.
- Validate configuration with options binding and `ValidateOnStart`.
- Required config: Entra client id/tenant id/secret, DB connection string, internal domain list.

## Error Handling and Logging
- Centralize error handling via middleware or minimal API filters.
- Log with structured logging and correlation IDs for key operations per SPEC-000.
- Do not log secrets or PII.
- Teams licensing errors: surface clear "webinar license required" message, do not retry.

## Test-Driven Development (TDD)
- Write tests first: red -> green -> refactor.
- Prefer xUnit and fluent assertions for readability.
- Spec acceptance tests (SPEC-010 §3, SPEC-300 §7/§8) must be implemented as unit + integration tests.
- Mock external systems (Graph API); avoid mocking internal logic.
- Metrics engine: 100% unit coverage for computation rules.

## Best Practices
- Keep methods small and single-purpose.
- Prefer immutable records for DTOs where possible.
- Avoid static state; favor DI for time, randomness, and environment.
- Document endpoints with OpenAPI metadata.
- Sync processing must be idempotent.
- Metrics recompute must be atomic with normalized data writes.

## Build and Tooling
- `dotnet build` must remain clean and warning-free.
- `dotnet test` should be added for new functionality.
- Keep formatting consistent with `dotnet format` defaults.
