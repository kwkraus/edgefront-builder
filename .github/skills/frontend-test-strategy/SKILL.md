---
name: frontend-test-strategy
description: 'Define focused frontend test strategy for Next.js features. Use for user-visible behavior coverage, risk-based test selection, and narrow verification command choice.'
argument-hint: 'Describe UI behavior changes, risk areas, and desired confidence level.'
---

# Frontend Test Strategy

## When to Use
- Any user-visible frontend behavior change in `src/frontend`
- Missing tests for route/component behavior
- Need fast confidence before broader test/build runs

## Quick Checklist
1. Identify highest-risk user-visible behavior changes.
2. Add tests for happy path and main failure/degraded paths.
3. Assert rendered output and interaction outcomes.
4. Run narrow validation before widening scope.

## Deep Workflow
1. Classify change risk: rendering-only, interaction logic, data-fetching behavior, or mixed.
2. Add/adjust tests for highest-risk paths first:
   - Primary success behavior
   - Loading/error/empty states where applicable
   - Regression path for prior bug behavior
3. Assert user-observable outcomes (content, controls, navigation), not implementation internals.
4. Keep tests feature-local and aligned with route/component responsibilities.
5. Run minimal verification commands for touched frontend scope, then expand only if needed.
6. Report coverage gaps and follow-up tests when time-constrained.

## Decision Points
- If behavior change affects accessibility or semantics, include explicit a11y-relevant assertions.
- If API contract assumptions changed, coordinate with `integration-contract-alignment`.
- If tests are brittle due to structure coupling, refocus assertions on user-visible behavior.

## Completion Checks
- New behavior is covered by focused tests.
- Assertions validate outcomes users can observe.
- Verification scope is minimal but sufficient for touched frontend paths.
