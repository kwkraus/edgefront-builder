<!--
SYNC IMPACT REPORT
==================
Version change: [TEMPLATE / unversioned] → 1.0.0
Bump type: MAJOR — initial ratification; all content populated from repository context

Modified principles (placeholder → concrete name):
  [PRINCIPLE_1_NAME] → I. Spec-Driven Development
  [PRINCIPLE_2_NAME] → II. Test-First / TDD
  [PRINCIPLE_3_NAME] → III. Vertical-Slice Architecture
  [PRINCIPLE_4_NAME] → IV. Metrics Persisted on Write
  [PRINCIPLE_5_NAME] → V. Security by Default
  (additional)       → VI. Structured Observability

Replaced generic sections:
  [SECTION_2_NAME / SECTION_2_CONTENT] → Tech Stack & Constraints
  [SECTION_3_NAME / SECTION_3_CONTENT] → Development Workflow
  [GOVERNANCE_RULES]                    → full governance text

Added sections:
  - "VI. Structured Observability" (sixth principle beyond template's five)
  - "Tech Stack & Constraints" (pinned stack, V1 constraints)
  - "Development Workflow" (agent routing, quality gates, amendment procedure)

Removed sections:
  - None (no previously-ratified sections existed)

Templates requiring updates:
  ✅ .specify/templates/plan-template.md     — "Constitution Check" gate uses runtime
     placeholder [Gates determined based on constitution file]; no structural edit needed.
  ✅ .specify/templates/spec-template.md     — no constitution-specific structural changes.
  ✅ .specify/templates/tasks-template.md    — task phases align with principles; no edits.
  ✅ .specify/templates/checklist-template.md — no constitution-specific changes needed.
  ✅ .github/agents/speckit.constitution.agent.md — no outdated agent-specific references.
  ✅ .github/copilot-instructions.md          — consistent with all six principles; no edits.
  ✅ .github/instructions/aspnet-webapi.instructions.md — consistent; no edits needed.
  ✅ .github/instructions/nextjs.instructions.md        — consistent; no edits needed.
  ✅ docs/spec-driven-development-process.md  — consistent with Principle I; no edits needed.

Follow-up TODOs:
  - None. All placeholders resolved from repository context.
-->

# EdgeFront Builder Constitution

## Core Principles

### I. Spec-Driven Development

Every feature MUST start as an approved functional specification before implementation begins.
Functional specs are authored as an Epic → Feature → User Story hierarchy in Azure DevOps.
No implementation work may begin until the Epic is in `Active` state with a recorded approval
comment. Technical specifications are generated from approved functional specs and published as
wiki pages under `/Tech-Specs/[Epic-ID]-[Slugified-Title]`. A functional spec that materially
changes after technical spec generation MUST receive a `techspec:stale` flag and trigger spec
regeneration before implementation continues.

**Rationale**: Unconstrained implementation without agreed-upon requirements produces rework.
Spec-first ensures alignment between stakeholders and implementers before any code is written.

### II. Test-First / TDD (NON-NEGOTIABLE)

All new functionality and bug-fix regressions MUST follow the red-green-refactor cycle: tests
are written and confirmed to fail *before* implementation begins. This applies to both backend
(xUnit) and frontend (Playwright E2E, Vitest).

- Backend contract tests and integration tests are required for all new API endpoints and for
  any endpoint whose contract changes.
- EF Core schema changes MUST be accompanied by verified migration scripts reviewed in PR.
- Tests MUST be committed alongside (or before) the feature code — not as a follow-up.
- No new feature is considered complete until all targeted tests pass in CI.
- PRs without tests MUST include explicit, documented justification reviewed by an approver.

**Rationale**: Late testing introduces regressions, slows review cycles, and reduces deployment
confidence. Test-first is the only discipline that guarantees specifications are executable.

### III. Vertical-Slice Architecture

The codebase MUST be organized by feature, not by layer.

- **Backend**: each feature lives in `Features/<Name>/` containing its endpoints, DTOs,
  handlers, and validators. Shared primitives go in `Domain/` or `Common/`; shared
  infrastructure in `Infrastructure/`. Horizontal layering of business logic is prohibited.
- **Frontend**: each feature is a route directory under `app/<feature>/`. Shared UI components
  live in `components/`; shared utilities in `lib/`. Cross-feature imports that bypass feature
  boundaries are prohibited.
- New projects, assemblies, or packages MUST be justified in `plan.md` Complexity Tracking
  and reference an approved spec. Speculative additions are prohibited (YAGNI).

**Rationale**: Vertical slices reduce merge conflicts, enable independent story delivery, and
make the codebase navigable without understanding the entire system.

### IV. Metrics Persisted on Write

All metric aggregations (registrations, attendees, unique domain counts, warm-account
influence) MUST be computed and stored at write time. Compute-on-read for aggregated metrics
is prohibited.

- Metric recomputes triggered by data changes MUST be atomic with the originating data write.
- No lazy or deferred metric computation that could produce stale reads under normal operation.
- The local data model is the authoritative ingestion-ready store. Normalized registration and
  attendance records MUST be persisted before any downstream or analytical processing.
- Schema changes that break normalization or remove the ability to replay/re-ingest records
  are prohibited without an explicit migration plan reviewed in an approved spec.

**Rationale**: Metrics are the product's primary analytical output. Persisting on write
guarantees consistent reads, avoids N+1 computation spikes, and keeps data ready for future
ingestion pipelines without schema rework.

### V. Security by Default

Authentication and authorization are non-negotiable on all user-facing and business endpoints.

- All API endpoints under `/api/v1` (except explicitly designated public health checks) MUST
  validate a Microsoft Entra ID JWT. `Microsoft.Identity.Web` is the sole approved
  authentication middleware for the backend.
- Microsoft Graph access MUST use delegated-only permissions via the OBO flow. Application
  permissions, webhooks, background Graph services, and Teams webinar integration are
  prohibited.
- Secrets MUST be stored in environment variables or user-secrets. No secrets, connection
  strings, or credentials may be committed to source control in any form.
- Logs MUST NEVER contain secrets, PII, or authentication tokens. Sensitive identifiers MUST
  be masked or omitted before writing to any log sink.
- The Entra app registration MUST expose only the `access_as_user` scope for
  frontend → backend token exchange and the minimum delegated Graph permissions required.

**Rationale**: Single-tenant Entra ID with delegated-only Graph access is the minimum security
posture for an organizational tool that handles identity and participation data.

### VI. Structured Observability

Production-safe structured logging is required across all critical backend flows and must be
non-intrusive on frontend flows.

- Log events MUST use stable, snake_case named properties (e.g., `session_id`,
  `registration_count`) — no free-form string interpolation with embedded runtime values.
- All request/response flows MUST carry a correlation identifier in structured log properties.
- Severity policy: `ERROR` for actionable failures requiring operator intervention; `WARNING`
  for degraded-but-recoverable states; `INFO` for lifecycle events; `DEBUG` permitted only
  in non-production contexts.
- Frontend: browser console errors MUST NOT be suppressed. Playwright E2E tests MUST capture
  network request logs via the test runner's network inspection hooks.
- No diagnostic data that could reveal PII, secrets, or auth tokens may appear in any log
  output regardless of severity level.

**Rationale**: Structured logs are the primary incident-triage tool. Without stable field names
and correlation context, diagnosing production failures is non-deterministic and unreliable.

## Tech Stack & Constraints

The following technology selections are approved and locked. Changes require a constitution
amendment.

| Layer | Approved Stack |
|---|---|
| Frontend | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS v4, Primer React v38, next-auth |
| Backend | ASP.NET Core Minimal API (.NET 10), EF Core, Azure SQL |
| Auth | Microsoft Entra ID single-tenant — `Microsoft.Identity.Web` |
| Graph | `Microsoft.Graph` SDK, delegated OBO only (`User.ReadBasic.All`, `User.Read`) |
| Hosting | Azure App Service |
| Testing — Backend | xUnit (`tests/backend/`) |
| Testing — Frontend | Playwright E2E (`src/frontend/e2e/`) |

**V1 Constraints** (in effect until an approved spec explicitly lifts them):

- No new external runtime dependencies without an approved spec and Complexity Tracking entry.
- No API pagination — V1 returns all records within reasonable operational limits.
- No optimistic UI updates — inline error banners with retry are the required error pattern.
- Desktop-first responsive layout; mobile is out of scope for V1.
- EF Core migrations MUST be explicit — no `AutoMigrate`. Migrations are committed and
  reviewed as part of feature PRs.
- No Teams webinar publish/sync API integration (removed; installs do not require Teams
  webinar app registration consent).

## Development Workflow

### Agent Routing

Agents are the authoritative implementation paths. Direct edits without the appropriate agent
SHOULD include a comment explaining the bypass.

| Task | Assigned Agent |
|---|---|
| Feature spec authoring (functional / technical) | `spec-driven-development` |
| General board CRUD, sprint planning, task/bug | `devops-workitem-manager` |
| TDD, test-first, regression coverage | `edgefront-tdd-engineer` |
| Backend API endpoints, DTOs, schema/migrations | `aspnet-api-expert` |
| Frontend UI/UX, accessibility, composition | `ui-ux-nextjs` |
| Structured logging, observability, SRE | `observability-sre` |
| Cross-stack integration, API contract alignment | `fullstack-integration` |
| CI/CD workflows, deployment safety, rollback | `cicd-devops` |
| Constitution amendments, speckit templates | `speckit.constitution` |

### Quality Gates

All pull requests MUST satisfy the following before merge:

1. **Build**: `dotnet build` (backend, `src/backend`) and `npm run build` (frontend,
   `src/frontend`) complete without errors or warnings.
2. **Lint**: `npm run lint` (frontend) produces zero errors.
3. **Tests**: All backend xUnit tests pass (`dotnet test`). New or changed functionality
   MUST have tests. PRs without tests MUST include documented justification.
4. **Constitution Check**: The PR author MUST confirm no principle in this constitution is
   violated. Violations require documented justification or a prior constitution amendment.
5. **Ecosystem Congruency**: After any dependency, structure, script, or domain-rule change,
   verify `.github/copilot-instructions.md`, `.github/instructions/`, `.github/agents/`, and
   `.github/skills/` still accurately reflect reality.

### Amendment Procedure

- **PATCH** (clarifications, wording, typo fixes): single-reviewer PR approval.
- **MINOR** (new principle, new section, materially expanded guidance): two-reviewer PR
  approval with written rationale.
- **MAJOR** (principle removal or redefinition, incompatible governance change): team
  discussion required; rationale and migration plan MUST be documented in the PR body;
  active in-flight feature work affected by the change MUST be assessed.
- All amendments MUST be executed via the `speckit.constitution` agent command to ensure
  template propagation and correct semantic version increment.

## Governance

This constitution supersedes all other project practices and ad hoc agreements. When conflicts
arise between this document and any other guidance file, the constitution takes precedence.

- All PRs and code reviews MUST verify compliance with the Core Principles.
- Complexity violations (e.g., adding a new framework, breaking slice isolation) MUST be
  justified in the Complexity Tracking table of the relevant feature `plan.md`.
- The `speckit.constitution` agent command is the canonical mechanism for proposing and
  ratifying amendments. Manual edits to this file bypass version tracking and MUST be
  accompanied by a manual version increment and Sync Impact Report update.
- Compliance is reviewed at each spec approval gate (before an Epic moves to `Active`) and
  at each PR merge gate.
- The Spec Kit runtime guidance lives in `.github/copilot-instructions.md`,
  `.github/instructions/`, and `.github/agents/`.

**Version**: 1.0.0 | **Ratified**: 2026-07-21 | **Last Amended**: 2026-07-21
