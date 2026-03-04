# EdgeFront Builder — Master Specification Index

**Project Status:** Architecture Locked for V1 Implementation

This document is the authoritative index of all specifications.
Each spec exists as an independent canvas document.
Non-authoritative planning/playbook docs may guide execution process, but they cannot override SPEC behavior.

---

## Spec Inventory

| Spec ID | Name | Status | Depends On |
|----------|------|--------|------------|
| SPEC-000 | System Blueprint | BUILD READY ✅ | — |
| SPEC-010 | Domain & Identity | BUILD READY ✅ | SPEC-000 |
| SPEC-100 | Frontend Shell & UX Contract | BUILD READY ✅ | SPEC-000, SPEC-010 |
| SPEC-110 | API Surface & OpenAPI Contract | BUILD READY ✅ | SPEC-000, SPEC-010, SPEC-100 |
| SPEC-120 | Data Schema & Migrations | BUILD READY ✅ | SPEC-000, SPEC-010 |
| SPEC-200 | Teams Integration & Data Sync | BUILD READY ✅ | SPEC-000, SPEC-010 |
| SPEC-210 | Webhook Security & Validation | DEPRECATED ❌ | — |
| SPEC-300 | Metrics Engine & Aggregation | BUILD READY ✅ | SPEC-010, SPEC-200 |

---

## Implementation Phases

### Phase 1 — Foundation
1. SPEC-000
2. SPEC-010
3. SPEC-120

### Phase 2 — Frontend (Parallel After Phase 1)
4. SPEC-100
5. SPEC-110

### Phase 3 — Teams Integration
6. SPEC-200
7. ~~SPEC-210~~ (Deprecated — webhooks removed)

### Phase 4 — Metrics Engine
8. SPEC-300

---

## Execution Rules for AI Implementation

- Each implementation chat must explicitly reference the SPEC ID.
- Implementation scope must not exceed the spec’s defined scope.
- If conflict exists between specs, precedence follows dependency order.
- All specs require comprehensive unit and integration testing per their Definition of Done.

---

**Note:** This index contains no behavioral logic. All authority lives in the individual spec documents.
