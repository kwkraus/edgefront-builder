---
description: Guidance for the ASP.NET Core Web API backend in src/backend
applyTo: "src/backend/**"
---

# ASP.NET Core Web API Instructions

These instructions apply to the backend project under `src/backend`.

## Instruction Consistency
- After making backend changes, review this file and any agents/skills that reference the backend stack to ensure they still match the code.
- See the "Instruction Ecosystem Congruency" section in `copilot-instructions.md` for the full checklist.

## Agent Routing
- Testing (TDD, test strategy) → `edgefront-tdd-engineer` agent
- Logging and observability → `observability-sre` agent
- Graph/Teams integration → `graph-teams-integration` agent
- Schema and migrations → use `data-schema-migration` skill via `aspnet-api-expert`
- API contract design → use `api-contract-design` skill via `aspnet-api-expert`
- Do not use generic plugin TDD agents for backend work unless the user explicitly asks for them by name.
- If requirements are unclear or missing, ask the user for clarification before inventing behavior.

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
- `Metrics/` for recompute orchestrator.
- `Common/` for shared primitives, errors, and result types.
- Tests live in `tests/backend/` mirroring feature folders.

## Core Dependencies
- Target framework: .NET 10 (`net10.0`).
- Use built-in DI, configuration, and logging.
- Prefer `Microsoft.Extensions.*` abstractions over concrete libs.
- EF Core with explicit migrations.
- Microsoft.Identity.Web for Entra ID JWT validation and OBO token acquisition.
- Microsoft.Graph SDK for Teams webinar operations.

## API Design
- Base path: `/api/v1`.
- Use resource-oriented routes.
- Use proper HTTP verbs and status codes.
- Validate inputs and return problem details.
- Use DTOs for request/response; never expose domain entities directly.
- Prefer async APIs and cancellation tokens for I/O.
- No pagination in V1 — list endpoints return all results for authenticated user.

## Microsoft Graph Integration
- Delegated-only permission model — all Graph operations use OBO flow.
- For detailed Graph integration guidance, use the `graph-teams-integration` agent.

## Data Schema
- UUID primary keys generated client-side.
- All datetime columns are datetime2 stored in UTC.
- EF Core with explicit migrations only — no auto-migration.
- For detailed schema definitions, constraints, and indexes, use the `data-schema-migration` skill.

## Security and Configuration
- Keep secrets in user secrets or environment variables.
- Avoid committing `appsettings.*.json` for local overrides.
- Validate configuration with options binding and `ValidateOnStart`.
- Required config: Entra client id/tenant id/secret, DB connection string, internal domain list.

## Error Handling
- Centralize error handling via middleware or minimal API filters.
- Do not log secrets or PII.
- For detailed logging policy and observability guidance, use the `observability-sre` agent.

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
