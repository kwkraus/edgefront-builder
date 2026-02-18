---
name: integration-contract-alignment
description: 'Coordinate frontend consumption with backend API contracts. Use for cross-stack route/DTO alignment, compatibility checks, and integration handoff decisions.'
argument-hint: 'Describe endpoint changes, frontend data usage, and compatibility constraints.'
---

# Integration Contract Alignment

## When to Use
- Backend route/DTO changes that impact frontend consumption
- Frontend integration work against evolving backend responses
- PR review focused on cross-stack API compatibility

## Quick Checklist
1. Identify changed routes and response fields.
2. Map frontend consumers to impacted contract fields.
3. Define compatibility impact and migration strategy.
4. Align payload expectations before implementation merge.

## Deep Workflow
1. Inventory affected backend endpoints and frontend call sites.
2. Confirm route, verb, request shape, and response shape alignment.
3. Classify compatibility impact (additive, soft-breaking, hard-breaking).
4. Define transition plan for non-additive changes (versioning, fallback, phased rollout).
5. Ensure frontend type/model updates are consistent with backend contract.
6. Produce explicit integration acceptance checks for key user flows.

## Decision Points
- If change is hard-breaking, block merge until migration path is explicit.
- If frontend and backend cannot ship simultaneously, require backward-compatible contract window.
- If response semantics changed, route to `status-code-decision-matrix` for status consistency.

## Completion Checks
- Contract changes and frontend updates are mutually consistent.
- Compatibility impact is documented with rollout assumptions.
- Key integration flow checks are defined and testable.
