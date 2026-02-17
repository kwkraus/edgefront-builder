# Project Guidelines

## Code Style
- Keep changes minimal and scoped to the task.
- Follow existing style in touched files; do not reformat unrelated code.
- Place implementation code under `src/` and tests under `tests/` unless the task explicitly requires otherwise.

## Architecture
- This repository is currently a scaffold with separate roots for frontend and backend work:
  - `src/frontend/` for UI/client code
  - `src/backend/` for API/service code
- Treat frontend and backend as separate components; avoid coupling unless a task explicitly introduces shared contracts.
- Put project documentation and decisions in `docs/`.
- Save architecture docs, FAQs, and session research/outcome writeups only in `docs/` using clear kebab-case names (for example: `docs/system-architecture.md`, `docs/faq.md`, `docs/session-2026-02-17-observability-outcomes.md`).

## Build and Test
- No canonical build/test/lint/format commands are currently defined in `readme.md` or tool manifests.
- Before running commands, discover scripts from the relevant manifest (for example `package.json`, `pyproject.toml`, or project files) once they are added.
- If no commands are defined, state that explicitly and proceed with file-level validation where possible.

## Conventions
- Use a discovery-first workflow for each task: check `readme.md`, `docs/`, and local manifests before implementing.
- Prefer small, reversible edits and document assumptions when repository conventions are not yet established.
- Do not introduce new frameworks, tooling, or directory structure unless requested.
