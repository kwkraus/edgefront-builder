# Project Guidelines

## Development Workflow
- Development uses **plan-mode interactive workflow** — requirements are discussed and implemented interactively.
- If requirements are unclear or missing, ask the user for clarification before inventing behavior.
- Use custom agents and skills for specialized domain guidance (testing, observability, Graph integration, etc.).
- Keep implementation scope focused on the current task.

## Agent Routing Policy
- For any TDD, test-first, or regression-test-driven work in this repository, use the `edgefront-tdd-engineer` agent by default.
- Do not choose generic plugin-provided TDD agents such as `testing-automation:tdd-red`, `testing-automation:tdd-green`, or `testing-automation:tdd-refactor` unless the user explicitly asks for one of them by name.
- Treat plugin TDD agents as optional supporting tools, not the canonical testing path for this repository.

## Code Style
- Keep changes minimal and scoped to the task.
- Follow existing style in touched files; do not reformat unrelated code.
- Place implementation code under `src/` and tests under `tests/` unless the task explicitly requires otherwise.

## Architecture
- `src/frontend/` — Next.js 16 App Router (React 19, TypeScript, Tailwind CSS v4, Primer React v38)
- `src/backend/` — ASP.NET Core minimal API (.NET 10, EF Core, Azure SQL)
- Frontend authenticates users via Entra ID (next-auth).
- Backend validates JWT access tokens on all user/business endpoints.
- Data sync is user-initiated via delegated token on page load (no webhooks or background services).
- Microsoft Graph uses delegated-only permission model:
  - All Graph operations use OBO flow — user must be present
  - No application permissions required
- Treat frontend and backend as separate components; avoid coupling unless a task explicitly introduces shared contracts.
- Put project documentation and decisions in `docs/`.

## Build and Test
- Backend: `dotnet build` and `dotnet test` from `src/backend`
- Frontend: `npm run lint` and `npm run build` from `src/frontend`
- Frontend E2E: `npx playwright test` from `src/frontend`
- Before running commands, verify scripts exist in the relevant manifest (`package.json`, `.csproj`).

## Conventions
- Use a discovery-first workflow for each task: check `readme.md` and local manifests before implementing.
- Prefer small, reversible edits and document assumptions when repository conventions are not yet established.
- Do not introduce new frameworks, tooling, or directory structure unless requested.
- Save architecture docs, FAQs, and session research/outcome writeups only in `docs/` using clear kebab-case names.

## Instruction Ecosystem Congruency
After implementing code changes, verify that instruction files, agents, and skills remain accurate:

1. **Technology references** — Do framework names, library names, and version numbers in instruction files still match `package.json` and `.csproj` dependencies?
2. **Project structure** — Do directory paths and file organization descriptions still match the actual layout?
3. **Build and test commands** — Do referenced commands still exist in the relevant manifests?
4. **Agent skill routing** — Do agent files reference skills that still exist and are named correctly?
5. **Domain rules in skills** — If domain logic changed (schema, computation rules, Graph flows), are the corresponding skills updated?
6. **Instruction file pointers** — Do "Agent Routing" sections in instruction files still point to the correct agents?

This check applies to all files in `.github/copilot-instructions.md`, `.github/instructions/`, `.github/agents/`, and `.github/skills/`.

Run this check after any code change that adds/removes/renames dependencies, changes project structure, modifies build/test scripts, or alters domain rules. If any file is now inaccurate, update it as part of the same change.
