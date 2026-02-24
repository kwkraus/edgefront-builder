# Goal
Create a **Master Specification** that orchestrates all sub-specs with explicit references, dependency order, and parallelization strategy so AI agents can execute with speed and accuracy.

This Master Spec acts as:
- The single source of truth
- The dependency map
- The execution sequencing guide
- The ambiguity detection layer

---

# MASTER SPEC STRUCTURE

## 1. System Vision (1–2 pages max)
- What EdgeFront Builder V1 does
- What it explicitly does NOT do
- Target users
- Success metrics

This keeps agents aligned on intent without letting them invent features.

---

## 2. Architecture & Technology Baseline (Reference: SPEC-000)
- Chosen stack
- Hosting model
- Repo structure
- CI/CD expectations
- Environment model (local/dev/prod)

This section must be stable before parallel work begins.

---

## 3. Domain & Contract Authority (Reference: SPEC-010, 011, 012)
Defines the shared language across all workstreams.

Includes:
- Entities and identity rules
- Tenant boundaries
- Account matching logic
- Event contracts
- API contracts

⚠ No downstream spec may redefine these.

---

## 4. Workstream Breakdown
Each workstream references its SPEC-xxx document.

| Workstream | Spec Ref | Depends On | Can Run In Parallel With |
|------------|----------|------------|--------------------------|
| Frontend Shell | SPEC-100 | API contracts | Ingestion, Metrics |
| Integrations/Ingestion | SPEC-200 | Domain Model | Frontend |
| Metrics Engine | SPEC-300 | Domain + Ingestion | Frontend |
| Admin & Config | SPEC-400 | Domain | Frontend |
| Reporting & Export | SPEC-500 | Metrics | Admin |

This table is what allows safe parallel execution.

---

## 5. Dependency Order for Maximum Speed

### Phase 0 – Lock the Spine
- SPEC-000 System Blueprint
- SPEC-010 Domain Model
- SPEC-011 API Contracts (even partial)

### Phase 1 – Parallel Foundations
- SPEC-100 Frontend Shell (mocked APIs allowed)
- SPEC-200 Ingestion (at least one connector)
- SPEC-300 Metrics Engine (with fixture data)

### Phase 2 – Convergence
- SPEC-500 Reporting
- SPEC-400 Admin

### Phase 3 – Hardening
- CI/CD
- Observability
- Security review

---

# Ideation Layer (Critical Before Build)

Yes — each spec should go through a focused ideation session before being considered build-ready.

Do NOT send first-draft specs directly to an implementation agent.

Each spec should go through this refinement loop:

1. Ambiguity Scan
   - Where could two engineers interpret this differently?
   - Are business rules example-backed?

2. Edge Case Enumeration
   - Missing data
   - Duplicate identities
   - Partial attendance
   - Time window boundary behavior

3. Data Integrity Rules
   - What happens if ingestion fails?
   - Can metrics compute with incomplete normalization?

4. Failure Behavior
   - Silent fallback?
   - Flag for manual correction?

5. Acceptance Test Drafting
   - Concrete examples with expected outputs

Only after this pass should a spec be marked:

BUILD READY

---

# Spec Readiness Levels

| Level | Meaning |
|-------|---------|
| Draft | Conceptual only |
| Refined | Business rules defined but edge cases incomplete |
| Build Ready | Contracts + examples + tests defined |
| Locked | Implemented and validated |

Agents should only execute against specs marked: BUILD READY.

---

# How This Prevents Agent Drift

• The Master Spec controls scope
• Sub-specs control behavior
• Contracts prevent silent mutation
• Ideation prevents ambiguity
• Phased dependency mapping enables safe parallelism

---

# Domain & Identity Rules — Build Ready (SPEC-010)

**Status:** BUILD READY ✅

This spec defines the canonical domain/identity model and the Teams webinar sync + ingestion contracts that drive all V1 metrics.

---

## 0) Locked decisions (V1)
### Identity & grouping
- Accounts are created by **domain uniqueness** (no external account database lookup).
  - `kpmg.com` and `kpmg.au` are **two different Accounts**.
- Subdomains are ignored in V1 (simple heuristic): `kt.kpmg.com` → `kpmg.com`.
- Person identity is keyed by **normalized email** (trim + lowercase).
- Internal domains are excluded from influence + warm metrics.

### Influence
- **Unique Accounts Influenced = attendance-only**.
  - Registration-only does **not** count toward influence.
  - Registrations are still stored as an "Interest Signal".

### Warm account (per-series only)
Warm is evaluated **per series** and is attendance-driven.
- **W1 (Session-scoped):** ≥ 2 **distinct emails** from same Account in a single session.
- **W2 (Series-scoped):** same Person attends ≥ 2 sessions in the same series.
  - Registrations do not count toward W2.

### Series/session lifecycle (Builder ↔ Teams)
- Builder is the source of truth for Series and Sessions.
- Sessions map 1:1 to Teams webinars via stable Teams ids (no title matching).
- Draft → Published lifecycle:
  - Teams webinars are created only on **Series Publish**.
- Post-publish edits:
  - Single atomic action: **Save & Publish to Teams**.
  - If publish fails: show "Publish failed" + retry.
  - If user navigates away while failing: unsynced edits are discarded.
- Drift detection (Teams edited directly):
  - Detect drift for title + time slot only.
  - Builder values remain primary; show Teams values for comparison.
- Delete:
  - Deleting a session deletes the mapped Teams webinar (with user confirmation).

### Webhook ingestion
- Webhook-driven updates for:
  - Registrations (created/updated)
  - Attendance report ready (created)
- Store **normalized records only** (no raw payload storage).
- On each webhook event: normalize + persist + recompute immediately.
- Webhook processing must be **idempotent**.
- Attendance qualification: if Teams attendance record exists → `attended=true` (no duration threshold).

### Consistency check (reconciliation)
- Trigger: Attendance report ready webhook.
- Action: reconcile **both** registrations and attendance for the session:
  - re-fetch authoritative sets
  - upsert normalized
  - delete missing
  - recompute SessionMetrics + SeriesMetrics
- If reconciliation fetch fails: auto-retry with backoff until success.
- Reconciliation status visible in both Series list and Session detail.

### Subscriptions (Graph change notifications)
- Subscriptions created automatically per session on Series Publish.
- Subscriptions auto-renew via in-app background worker.
- After attendance reconciliation succeeds: **delete subscriptions** for that session.
- Renewal failure policy: retry for a fixed window (e.g., 24h), then disable and require manual intervention.

---

## 1) Data model authority (V1)
### Series
- seriesId (UUID)
- ownerUserId (Entra OID)
- title (unique per user)
- status (Draft | Published)
- createdAt, updatedAt

### Session
- sessionId (UUID)
- seriesId
- ownerUserId
- title (not unique)
- startsAt, endsAt
- status (Draft | Published | Reconciled)
- teamsWebinarId (string, nullable until published)
- driftStatus (None | DriftDetected)
- reconcileStatus (Synced | Reconciling | Retrying | Disabled)
- lastSyncAt (nullable)
- lastError (nullable)

### NormalizedRegistration
- registrationId (UUID)
- sessionId
- ownerUserId
- email (normalized)
- emailDomain (normalized account domain)
- registeredAt

**Uniqueness:** (ownerUserId, sessionId, email)

### NormalizedAttendance
- attendanceId (UUID)
- sessionId
- ownerUserId
- email (normalized)
- emailDomain (normalized account domain)
- attended = true
- durationSeconds (optional)
- durationPercent (optional)
- firstJoinAt (optional)
- lastLeaveAt (optional)

**Uniqueness:** (ownerUserId, sessionId, email)

### SessionMetrics
- sessionId
- totalRegistrations
- totalAttendees
- uniqueRegistrantAccountDomains
- uniqueAttendeeAccountDomains
- warmAccountsTriggered (list of accountDomain)

### SeriesMetrics
- seriesId
- totalRegistrations
- totalAttendees
- uniqueRegistrantAccountDomains (distinct across series)
- uniqueAccountsInfluenced (attendance-only integer)
- warmAccounts (list of {accountDomain, warmRule})

---

## 2) Computation rules (authoritative)
### Account domain normalization (V1)
- lowercase + trim
- drop subdomain labels to keep last 2 labels (simple heuristic)

### SessionMetrics calculations
- totalRegistrations = count(NormalizedRegistration where sessionId)
- totalAttendees = count(NormalizedAttendance where sessionId)
- uniqueRegistrantAccountDomains = count(distinct emailDomain) from registrations
- uniqueAttendeeAccountDomains = count(distinct emailDomain) from attendance
- warmAccountsTriggered (W1 only) = domains where distinct attendee emails in this session ≥ 2

### SeriesMetrics calculations
- totalRegistrations = sum(session.totalRegistrations)
- totalAttendees = sum(session.totalAttendees)
- uniqueRegistrantAccountDomains = count(distinct emailDomain) across registrations in all sessions
- uniqueAccountsInfluenced = count(distinct emailDomain) across **attendance** in all sessions
- warmAccounts list:
  - include (domain, "W1") if any session triggers W1 for that domain
  - include (domain, "W2") if any person in that domain attends ≥ 2 sessions in the series

---

## 3) Acceptance tests (must pass)
### Identity & normalization
- Same email differing by case → one Person.
- Subdomain stripped: kt.kpmg.com → kpmg.com.
- Domains with different TLDs are separate accounts: kpmg.com ≠ kpmg.au.

### Influence
- Registration-only does not contribute to uniqueAccountsInfluenced.
- Attendance contributes regardless of duration.
- Internal domains excluded.

### Warm
- W1 requires ≥ 2 distinct emails in the same session.
- W2 requires same email attending ≥ 2 sessions in same series.
- Registrations do not count toward W2.
- Warm evaluated per series only.

### Sync & lifecycle
- Draft edits do not create Teams webinars.
- Publish creates webinars + subscriptions.
- Published session uses Save & Publish atomic flow.
- Publish failure: retry; navigating away discards unsynced edits.
- Drift detection shows warning and comparison values.
- Delete requires confirmation and deletes Teams webinar.

### Ingestion & reconciliation
- Webhook events are idempotent (duplicates do not inflate metrics).
- On every webhook: normalize + persist + recompute session + series.
- Attendance report ready triggers full reconcile (regs + attendance).
- Reconcile retries automatically; status visible in series + session views.

### Subscriptions
- Auto-renew before expiration for non-reconciled sessions.
- Delete subscriptions on successful reconciliation.
- Renewal failure window disables webhooks and flags requires attention.

---

## 4) Definition of Done for implementation agents (SPEC-010)
- All acceptance tests above implemented as **unit + integration tests**.
- Metrics unit tests cover W1/W2, influence logic, and distinct domain counting.
- Integration tests cover publish, webhook ingestion, reconciliation, and subscription renewal lifecycle.
- OpenAPI updated for any endpoints introduced while implementing this spec.


# Teams Integration & Ingestion — Build Ready (SPEC-200)

**Status:** BUILD READY ✅

This spec defines how EdgeFront Builder integrates with Microsoft Teams (Webinars) via Microsoft Graph, manages lifecycle, and ingests registration + attendance data.

---

## 0) Architectural Boundary
- Single Entra tenant application.
- REST API (monolith) hosts:
  - TeamsGraphClient
  - WebhookController
  - SubscriptionRenewalHostedService
- All integration logic isolated under `/Integrations` and `/Ingestion` modules.

---

## 1) Authentication & Authorization
### 1.1 App Registration
- Single-tenant Entra ID app.
- Application permissions (not delegated) for webinar management and reporting.
- Use client credential flow.

### 1.2 Token Handling
- Centralized Graph token acquisition service.
- Tokens cached in memory.
- Automatic refresh before expiry.

---

## 2) Webinar Lifecycle Management

### 2.1 On Series Publish
For each Session:
1. Create Teams webinar.
2. Store `teamsWebinarId`.
3. Create Graph webhook subscriptions for:
   - Registrations (created, updated)
   - Attendance report ready (created)
4. Store:
   - subscriptionId
   - expirationDateTime

If any step fails → entire publish fails (atomic behavior).

---

## 3) Post-Publish Update Flow
- "Save & Publish to Teams" performs:
  1. Local validation
  2. Persist changes
  3. PATCH webinar via Graph
  4. Update local `updatedAt`
- If PATCH fails:
  - Show "Publish failed"
  - Allow retry
  - If user leaves → discard unsynced edits

---

## 4) Drift Detection
On session load (Published sessions only):
1. Fetch webinar metadata from Graph.
2. Compare:
   - Title
   - Start time
   - End time
3. If mismatch → set `driftStatus = DriftDetected`.
4. Do not auto-overwrite.

---

## 5) Webhook Handling

### 5.1 Endpoint Requirements
- HTTPS public endpoint
- Validation token handshake supported
- Signature validation enforced

### 5.2 Event Processing Flow
For each notification:
1. Validate signature.
2. Identify session by `teamsWebinarId`.
3. Dispatch to IngestionService.

### 5.3 Registration Webhook
- Fetch changed registration(s) if payload incomplete.
- Normalize.
- Upsert into `NormalizedRegistration`.
- Recompute SessionMetrics + SeriesMetrics.

### 5.4 Attendance Report Ready Webhook
- Fetch authoritative attendance report.
- Fetch authoritative registrations.
- Upsert normalized data.
- Delete local rows not present in Teams.
- Recompute SessionMetrics + SeriesMetrics.
- Mark session `Reconciled = true` if successful.
- Delete subscriptions for that session.

---

## 6) Idempotency Rules
- Unique constraints:
  - (ownerUserId, sessionId, email) for registrations
  - (ownerUserId, sessionId, email) for attendance
- Webhook duplicates must not inflate counts.
- Reconcile overwrites stale local data.

---

## 7) Subscription Renewal

### 7.1 Renewal Hosted Service
Runs on interval (e.g., every 15 minutes).

Renew subscriptions where:
- Session status = Published
- Session not reconciled
- expirationDateTime within renewal window

### 7.2 Renewal Failure Policy
- Exponential backoff retries.
- Retry window: up to 24 hours.
- After window:
  - Mark `reconcileStatus = Disabled`
  - Stop renewal attempts
  - Surface visible alert in UI.

### 7.3 Cleanup
- On successful reconciliation → delete subscriptions.

---

## 8) Reconciliation Status States
Session.reconcileStatus values:
- Synced
- Reconciling
- Retrying
- Disabled

Status visible in:
- Series list (badge)
- Session detail view (full status + last error)

---

## 9) Error Handling Contracts
- Graph API failures logged with correlation id.
- Webhook failures must not crash API.
- Publish failures never leave partial Teams state.
- Retry logic must be non-blocking for API threads.

---

## 10) Integration Test Requirements

Mandatory integration tests:
- Publish creates webinar + subscriptions.
- Registration webhook updates metrics.
- Attendance report ready triggers full reconciliation.
- Duplicate webhook does not inflate metrics.
- Subscription renewal before expiry.
- Renewal failure window disables webhooks.
- Deleting session deletes webinar + subscriptions.

---

## 11) Definition of Done (SPEC-200)
- All flows covered by integration tests.
- Webhook validation implemented.
- Idempotency guaranteed.
- Subscription lifecycle automated.
- OpenAPI updated with webhook endpoint.


# Metrics Engine & Aggregation — Build Ready (SPEC-300)

**Status:** BUILD READY ✅

This spec defines the canonical metrics computation rules, recomputation triggers, transactional boundaries, and unit/integration testing requirements.

---

## 0) Scope & Authority
### In scope
- Compute and persist **SessionMetrics** and **SeriesMetrics** as defined in SPEC-010.
- Recompute on:
  - Registration webhook events
  - Attendance webhook events
  - Reconciliation completion
- Provide deterministic, idempotent outcomes.

### Out of scope (V1)
- AccountInSeriesMetrics tables
- Global cross-series dashboards
- Any “duration threshold” gating
- Any edits to attendance/registration by users

**Authority:** If SPEC-300 conflicts with SPEC-010, SPEC-010 wins.

---

## 1) Inputs (authoritative)
### Normalized tables
- NormalizedRegistration(sessionId, ownerUserId, email, emailDomain, registeredAt)
- NormalizedAttendance(sessionId, ownerUserId, email, emailDomain, attended=true, durationSeconds?, durationPercent?)

### Domain rules
- Domain normalization is defined in SPEC-010 and must be applied before persistence.

---

## 2) Outputs (authoritative)
### SessionMetrics
Stored per sessionId:
- totalRegistrations (int)
- totalAttendees (int)
- uniqueRegistrantAccountDomains (int)
- uniqueAttendeeAccountDomains (int)
- warmAccountsTriggered (string[] of accountDomain; W1 only)

### SeriesMetrics
Stored per seriesId:
- totalRegistrations (int)
- totalAttendees (int)
- uniqueRegistrantAccountDomains (int; distinct across series)
- uniqueAccountsInfluenced (int; distinct domains across attendance in series)
- warmAccounts ({accountDomain, warmRule}[])

---

## 3) Computation Rules (authoritative)

### 3.1 SessionMetrics(sessionId)
Compute from normalized rows where (ownerUserId, sessionId):

1) totalRegistrations
- COUNT(registrations)

2) totalAttendees
- COUNT(attendance)

3) uniqueRegistrantAccountDomains
- COUNT(DISTINCT registrations.emailDomain)

4) uniqueAttendeeAccountDomains
- COUNT(DISTINCT attendance.emailDomain)

5) warmAccountsTriggered (W1 only)
- For each emailDomain in attendance:
  - if COUNT(DISTINCT attendance.email) for that domain >= 2 → include domain
- Output list must be stable-sorted lexicographically.

### 3.2 SeriesMetrics(seriesId)
Let sessions = all sessions in series.

1) totalRegistrations
- SUM(SessionMetrics.totalRegistrations)

2) totalAttendees
- SUM(SessionMetrics.totalAttendees)

3) uniqueRegistrantAccountDomains (distinct across series)
- COUNT(DISTINCT registrations.emailDomain) across all sessions in series

4) uniqueAccountsInfluenced
- COUNT(DISTINCT attendance.emailDomain) across all sessions in series

5) warmAccounts
- Start empty map warm[domain] = set of rules.

W1 contribution:
- For each session in series:
  - For each domain in SessionMetrics.warmAccountsTriggered:
    - add rule "W1" for that domain

W2 contribution:
- For each domain in attendance across series:
  - Determine if any email in that domain appears in attendance for >=2 distinct sessions.
  - If yes → add rule "W2" for that domain

Final warmAccounts list rules:
- If a domain qualifies for both W1 and W2, prefer storing **W2 only** OR store both?
  - V1 decision: store **one entry per domain** with rule precedence: W2 > W1.
  - If W2 true → warmRule="W2" else if W1 true → warmRule="W1".
- Output list must be stable-sorted lexicographically by accountDomain.

---

## 4) Recompute Triggers & Scope

### 4.1 On registration webhook affecting sessionId
- Upsert normalized registration
- Recompute:
  - SessionMetrics(sessionId)
  - SeriesMetrics(seriesId)

### 4.2 On attendance webhook affecting sessionId
- Upsert normalized attendance
- Recompute:
  - SessionMetrics(sessionId)
  - SeriesMetrics(seriesId)

### 4.3 On reconciliation completion for sessionId
- After authoritative re-fetch + upsert + delete-missing:
  - Recompute SessionMetrics(sessionId)
  - Recompute SeriesMetrics(seriesId)

---

## 5) Transaction & Consistency Boundaries

### 5.1 Ingestion + recompute atomicity
For each webhook event, the system must ensure:
- Normalized data writes and metrics updates occur in a single transaction.

Transaction steps:
1) Upsert normalized rows
2) Recompute SessionMetrics
3) Recompute SeriesMetrics
4) Commit

If any step fails:
- Roll back.
- Retry policy is handled by caller (IngestionService).

### 5.2 Concurrency & locking
- Multiple webhook events may arrive concurrently for the same session.
- Must prevent race conditions that produce incorrect metrics.

V1 requirement:
- Use a database-level transactional strategy that guarantees correctness.
  - Acceptable approaches:
    - Serializable transaction for recompute scope
    - Explicit row lock on Session + Series aggregate rows before recompute
    - Advisory lock keyed by (ownerUserId, sessionId)

Implementation must document which approach is used.

---

## 6) Performance Constraints (V1)
- Optimize for correctness over performance.
- Recompute may be full-scan of normalized rows for the session/series.
- Expected volume is low in V1; correctness is priority.

---

## 7) Unit Tests (mandatory, comprehensive)

### 7.1 SessionMetrics tests
- totalRegistrations count
- totalAttendees count
- distinct registrant domains
- distinct attendee domains
- W1 warm trigger (>=2 distinct emails same domain)
- W1 does not trigger on duplicate records of same email
- stable sort of warmAccountsTriggered

### 7.2 SeriesMetrics tests
- sum registrations/attendees across sessions
- distinct registrant domains across series
- distinct attendee domains across series (uniqueAccountsInfluenced)
- W1 propagation from sessions
- W2 detection across sessions
- W2 does not trigger with registrations only
- W2 triggers with same email across 2 sessions
- precedence rule W2 > W1
- stable sort of warmAccounts

### 7.3 Idempotency tests
- Duplicate webhook upsert does not change metrics
- Reconciliation delete-missing removes stale rows and updates metrics correctly

---

## 8) Integration Tests (mandatory)
- Webhook registration event → normalized upsert → Session+Series metrics updated
- Webhook attendance event → normalized upsert → Session+Series metrics updated
- Attendance report ready → reconcile (regs+attendance) → metrics updated
- Concurrent event simulation for same session (no inconsistent metrics)

---

## 9) OpenAPI / API surface
Metrics endpoints must only read from persisted aggregates:
- GET /series/{seriesId}/metrics
- GET /sessions/{sessionId}/metrics

No compute-on-read.

---

## 10) Definition of Done (SPEC-300)
- All unit tests and integration tests pass.
- Concurrency strategy documented and covered by at least one integration test.
- Metrics endpoints query only aggregates.
- No metric inflation under duplicate webhook delivery.


# EdgeFront Builder — Master Specification Index

**Project Status:** Architecture Locked for V1 Implementation

This index defines authoritative specs, dependency order, and build sequencing for AI-assisted execution.

---

## Spec Inventory & Status

| Spec ID | Name | Status | Depends On | Parallelizable After |
|----------|------|--------|------------|----------------------|
| SPEC-000 | System Blueprint | BUILD READY ✅ | — | — |
| SPEC-010 | Domain & Identity | BUILD READY ✅ | SPEC-000 | — |
| SPEC-200 | Teams Integration & Ingestion | BUILD READY ✅ | SPEC-000, SPEC-010 | — |
| SPEC-300 | Metrics Engine & Aggregation | BUILD READY ✅ | SPEC-010, SPEC-200 | — |

---

## Dependency Order (Implementation Phases)

### Phase 1 — Foundation
1. SPEC-000 (System Blueprint scaffolding)
2. SPEC-010 (Domain model + migrations)

### Phase 2 — Integration Layer
3. SPEC-200 (Teams + Webhook + Subscription lifecycle)

### Phase 3 — Metrics & Aggregation
4. SPEC-300 (Metrics engine + recompute logic)

---

## Execution Model for AI Agents

Each implementation chat should:
- Reference the SPEC-xxx explicitly.
- Implement only that spec’s scope.
- Produce unit + integration tests per Definition of Done.
- Update OpenAPI where applicable.

No spec may redefine authority defined in an earlier spec.

---



**Status:** BUILD READY ✅

This spec defines the V1 system architecture baseline, repo structure, stack choices, environments, CI/CD, deployment, and operational guardrails.

---

## 0) Guiding principles
- Correctness over performance (V1).
- Spec authority lives in this canvas; code must conform.
- Avoid user-facing hard errors where possible; surface “data unavailable” states with clear next actions.
- Keep V1 simple; design seams for future extraction.

---

## 1) Tech stack (V1 locked)
### Backend
- .NET (latest LTS at implementation time) Web API (REST)
- OpenAPI/Swagger enabled
- EF Core
- Background hosted services inside the same app for:
  - Graph subscription renewal
  - Reconciliation retry loop (if implemented as background worker)

### Frontend
- Web app (React-based) with authenticated UI via Entra ID
- Minimal dashboard scope (per-series and per-session only)

### Database
- Relational DB (one database)
- Ownership boundary: per-user (Entra OID)

### Identity/Auth
- Entra ID single-tenant app
- Backend validates JWT access tokens
- Authorization: user can only access rows where ownerUserId == current user OID

### Hosting (Azure)
- Single-region (V1)
- App hosting: Azure App Service (Linux) OR Azure Container Apps (single container)
  - V1 preference: App Service for simplicity
- Database: Azure SQL

---

## 2) High-level architecture

### 2.1 Components
- Web UI
- Web API (monolith)
  - Series/Sessions CRUD
  - Teams/Graph integration
  - Webhook endpoint
  - Ingestion + normalization
  - Metrics recompute + persistence
  - Subscription renewal background worker
- Azure SQL

### 2.2 Key invariants
- Builder is the source of truth for Series/Sessions.
- Published sessions: Save == Save & Publish to Teams (atomic).
- Metrics endpoints query only persisted aggregates.
- Webhook ingestion is idempotent.

---

## 3) Repo structure (recommended)
Single repo with clear module boundaries:

- `/apps/web` (frontend)
- `/apps/api` (backend)
- `/libs/domain` (entities + rules; optional folder split inside api if single-language)
- `/libs/contracts` (OpenAPI + event contract docs)
- `/infra` (Bicep/Terraform; V1 minimal)
- `/tests` (integration harness, fixtures)

Backend internal structure (apps/api):
- `/Controllers`
- `/Application` (services/use-cases)
- `/Domain` (entities + rules + metrics logic)
- `/Infrastructure` (EF Core, Graph client, hosted services)
- `/Persistence` (DbContext, migrations)
- `/Integrations/Graph` (TeamsGraphClient)
- `/Ingestion` (normalization, dedupe, reconciliation)
- `/Metrics` (recompute orchestrator)

---

## 4) Environments
- Local dev
- Dev (shared)
- Prod

Environment variables / secrets:
- Entra app client id
- Entra tenant id
- Entra app client secret (or Managed Identity in later hardening)
- Graph webhook secret/validation configuration
- DB connection string

---

## 5) CI/CD (V1)
Pipeline stages:
1) Build
2) Unit tests (must run fast)
3) Integration tests (can run slower)
4) Lint/format checks
5) Package
6) Deploy to Dev (on main)
7) Deploy to Prod (manual approval)

Required gates:
- All tests green
- OpenAPI generation/validation step
- Database migration validation (generate migration scripts)

---

## 6) Observability & diagnostics (V1)
- Structured logging with correlation IDs
  - Correlation propagated from webhook handling through ingestion and metrics recompute
- Key log events:
  - Publish series started/completed
  - Webinar create/update/delete results
  - Webhook received/validated
  - Ingestion upsert counts
  - Metrics recompute started/completed
  - Reconciliation started/completed/retry
  - Subscription renewal attempted/succeeded/failed

---

## 7) Error handling UX contract
- Avoid raw stack traces surfaced to user.
- Prefer:
  - “Publish failed” with retry
  - “Webhook degraded / Disabled” status indicators
  - Clear guidance on what to do next

---

## 8) Security baseline
- Validate JWT tokens on every API request.
- Webhook endpoint:
  - Supports validation handshake
  - Validates notification authenticity
  - Rejects unauthenticated/unsigned payloads
- Principle of least privilege for Graph permissions.

---

## 9) Seed data & fixtures
- Provide fixture datasets for metrics/integration tests:
  - series with multiple sessions
  - registrations-only scenario
  - attendance-only scenario
  - mixed internal/external domains
  - warm W1 and W2 scenarios

---

## 10) Definition of Done (SPEC-000)
- Repo scaffolding in place as per structure.
- Local dev instructions documented.
- CI pipeline runs build + unit + integration tests.
- Deploy path to Dev environment validated.
- OpenAPI published and kept in sync.

