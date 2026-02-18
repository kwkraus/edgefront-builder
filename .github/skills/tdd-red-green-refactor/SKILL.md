---
name: tdd-red-green-refactor
description: 'Apply red-green-refactor with minimal scope and explicit decision points. Use for test-first implementation flow across frontend and backend changes.'
argument-hint: 'Describe requested behavior, target component, and constraints on scope/refactoring.'
---

# TDD Red Green Refactor

## When to Use
- New behavior implementation requiring test-first workflow
- Bug fixes that need regression-first coverage
- Refactors that must preserve externally observable behavior

## Quick Checklist
1. Write a failing test for requested behavior.
2. Implement the smallest change to make it pass.
3. Refactor only where needed for clarity/maintainability.
4. Re-run focused verification and report outcomes.

## Deep Workflow
1. Define acceptance behavior in one sentence and convert it to a focused failing test.
2. Confirm the test fails for the expected reason before changing production code.
3. Implement minimal production changes to satisfy the failing test.
4. Re-run focused tests; if passing, perform constrained refactor that preserves behavior.
5. Re-run tests after refactor and broaden verification only when risk indicates.
6. Report red/green evidence, refactor scope, and residual risks.

## Decision Points
- If multiple failures appear, isolate the smallest deterministic failure first.
- If passing requires broad code movement, split work and keep the first change minimal.
- If bug fix is requested, require a regression test that fails before the fix.

## Completion Checks
- A failing test existed before implementation.
- Production change is minimal and traceable to test intent.
- Refactor does not alter externally observable behavior.
- Focused verification passes for touched scope.
