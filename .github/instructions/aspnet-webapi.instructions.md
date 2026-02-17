---
description: Guidance for the ASP.NET Core Web API backend in src/backend
applyTo: "src/backend/**"
---

# ASP.NET Core Web API Instructions

These instructions apply to the backend project under `src/backend`.

## Instruction Consistency
- After making backend changes, review this file and update it if guidance is no longer accurate.
- Keep the instruction set aligned with the current architecture and dependencies.

## Architecture
- Use a minimal API style unless the feature requires controllers.
- Keep endpoints thin: delegate logic to domain and application services.
- Organize code by feature (vertical slices), not by technical layer.
- Avoid cross-cutting dependencies between features; share via interfaces.

## Project Structure
- `Program.cs` for startup and DI registration.
- `Features/<FeatureName>/` for endpoints, DTOs, handlers, validators.
- `Domain/` for core entities, value objects, domain rules.
- `Infrastructure/` for data access, external integrations, providers.
- `Common/` for shared primitives, errors, and result types.
- Tests live in `tests/backend/` mirroring feature folders.

## Core Dependencies
- Target framework: .NET 10 (`net10.0`).
- Use built-in DI, configuration, and logging.
- Prefer `Microsoft.Extensions.*` abstractions over concrete libs.
- If data access is needed, prefer EF Core with explicit migrations.

## API Design
- Use resource-oriented routes: `/api/<resource>`.
- Use proper HTTP verbs and status codes.
- Validate inputs and return problem details on validation errors.
- Use DTOs for request/response; never expose entities directly.
- Prefer async APIs and cancellation tokens for I/O.

## Security and Configuration
- Keep secrets in user secrets or environment variables.
- Avoid committing `appsettings.*.json` for local overrides.
- Validate configuration with options binding and `ValidateOnStart`.

## Error Handling and Logging
- Centralize error handling via middleware or minimal API filters.
- Log with structured logging and event ids for key operations.
- Do not log secrets or PII.

## Test-Driven Development (TDD)
- Write tests first: red -> green -> refactor.
- Prefer xUnit and fluent assertions for readability.
- Use minimal integration tests for endpoints and contracts.
- Mock external systems; avoid mocking internal logic.

## Best Practices
- Keep methods small and single-purpose.
- Prefer immutable records for DTOs where possible.
- Avoid static state; favor DI for time, randomness, and environment.
- Document endpoints with OpenAPI metadata.

## Build and Tooling
- `dotnet build` must remain clean and warning-free.
- `dotnet test` should be added for new functionality.
- Keep formatting consistent with `dotnet format` defaults.
