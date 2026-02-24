# EdgeFront Builder — Implementation Playbook

This document defines how AI agents are used to implement the system safely, deterministically, and in parallel.

It is separate from the Master Spec.
The Master Spec defines WHAT to build.
This Playbook defines HOW to build it.

---

# 1. Core Principles

1. Spec authority is absolute.
   - No behavior may be invented outside a SPEC-xxx document.
   - If ambiguity exists, stop and raise `TODO-SPEC`.

2. One spec per implementation branch.
   - Never mix scopes.

3. All work must be test-backed.
   - Unit + integration tests required per spec Definition of Done.

4. OpenAPI is contract-first.
   - No endpoint exists without being defined in SPEC-110.

---

# 2. Branching Strategy

Main branch: always green.

Branch naming:
- spec-000-scaffold
- spec-120-schema
- spec-110-openapi
- spec-010-domain
- spec-200-teams
- spec-210-webhook-security
- spec-300-metrics
- spec-100-frontend

Each branch implements exactly one SPEC.

Merge only when:
- Tests pass
- CI pipeline green
- No scope creep detected

---

# 3. Execution Phases

## Phase 1 — Foundation
1. SPEC-000 (Scaffold)
2. SPEC-120 (Schema + migrations)
3. SPEC-110 (OpenAPI contract)

## Phase 2 — Domain & CRUD
4. SPEC-010 (Domain rules)
5. Implement CRUD endpoints from SPEC-110

## Phase 3 — Frontend Shell (parallel after Phase 1)
6. SPEC-100

## Phase 4 — Teams Integration
7. SPEC-200
8. SPEC-210

## Phase 5 — Metrics Engine
9. SPEC-300

---

# 4. AI Implementation Chat Template

Every implementation chat must start with:

## Scope
Implement SPEC-XXX only.
Do not modify other specs.

## Dependencies
List already merged specs.

## Output Requirements
- Files changed
- Tests added
- Migrations added (if applicable)
- OpenAPI updates (if applicable)

## Hard Gates
- All unit tests pass
- All integration tests pass
- No TODO left in critical path
- No schema changes without migration

## Stop Rule
If a required rule is missing in the spec:
- Add comment `TODO-SPEC`
- Stop implementation
- Do not invent behavior

---

# 5. Parallelization Model

Allowed Parallel Builds:
- SPEC-100 (Frontend) after Phase 1
- SPEC-200 & SPEC-210 together

Not Parallelizable:
- SPEC-300 depends on SPEC-200
- SPEC-120 must precede domain + metrics

---

# 6. Quality Enforcement

Mandatory for each PR:
- Unit coverage >= 80%
- 100% coverage for metrics engine logic
- Integration tests cover:
  - Publish flow
  - Webhook ingestion
  - Reconciliation
  - Renewal behavior

---

# 7. Drift Prevention Rules

- Schema changes require SPEC-120 update.
- Endpoint changes require SPEC-110 update.
- Metric rule changes require SPEC-010 or SPEC-300 update.
- UI behavior changes require SPEC-100 update.

No silent changes allowed.

---

# 8. When to Open New Chat

Open a new chat for each spec implementation.
Reference only:
- The SPEC document
- This Playbook
- Relevant dependency specs

Do not rely on prior conversation memory.

---

# 9. Completion Criteria for V1

V1 is complete when:
- All specs implemented
- CI green
- Dev deployment validated
- Publish → Webinar → Webhook → Metrics flow verified end-to-end
- No metric inflation under duplicate webhook delivery

---

This Playbook governs all AI-assisted development for EdgeFront Builder.

