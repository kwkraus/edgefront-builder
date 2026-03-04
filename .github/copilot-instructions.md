# Project Guidelines

## Spec-Driven Development
- Authoritative specifications live in `docs/specs/edge_front_builder_master_spec_index.md` and individual `docs/specs/spec_*.md` files.
- Non-authoritative docs (playbooks, planning docs) may guide process but cannot override SPEC behavior.
- Every implementation task must reference the SPEC ID being implemented.
- Implementation scope must not exceed the spec's defined scope.
- If conflict exists between specs, precedence follows dependency order (SPEC-000 > SPEC-010 > downstream).
- If a required rule is missing or ambiguous in a spec, add `TODO-SPEC` and stop — do not invent behavior.

## Code Style
- Keep changes minimal and scoped to the task.
- Follow existing style in touched files; do not reformat unrelated code.
- Place implementation code under `src/` and tests under `tests/` unless the task explicitly requires otherwise.

## Architecture
- `src/frontend/` — Next.js 16 App Router (React 19, TypeScript, Tailwind CSS)
- `src/backend/` — ASP.NET Core minimal API (.NET 10, EF Core, Azure SQL)
- Frontend authenticates users via Entra ID (MSAL.js / next-auth).
- Backend validates JWT access tokens on all user/business endpoints.
- Data sync is user-initiated via delegated token on page load (no webhooks or background services).
- Microsoft Graph uses delegated-only permission model per SPEC-200:
  - All Graph operations use OBO flow — user must be present
  - No application permissions required
- Treat frontend and backend as separate components; avoid coupling unless a task explicitly introduces shared contracts.
- Put project documentation and decisions in `docs/`.

## Build and Test
- Backend: `dotnet build` and `dotnet test` from `src/backend`
- Frontend: `npm run lint` and `npm run build` from `src/frontend`
- Before running commands, verify scripts exist in the relevant manifest (`package.json`, `.csproj`).
- All specs require comprehensive unit and integration tests per their Definition of Done.

## Conventions
- Use a discovery-first workflow for each task: check `docs/specs/`, `readme.md`, and local manifests before implementing.
- Prefer small, reversible edits and document assumptions when repository conventions are not yet established.
- Do not introduce new frameworks, tooling, or directory structure unless requested.
- Save architecture docs, FAQs, and session research/outcome writeups only in `docs/` using clear kebab-case names.
