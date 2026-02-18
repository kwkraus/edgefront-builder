---
name: api-test-strategy
description: 'Define focused test strategy for ASP.NET Core minimal APIs. Use for contract tests, endpoint behavior coverage, and narrow verification command selection.'
argument-hint: 'Describe endpoint behavior changes, risk areas, and desired confidence level.'
---

# API Test Strategy

## When to Use
- Any backend API behavior or contract change
- Missing tests for status code and payload behavior
- Need fast confidence before broader test runs

## Quick Checklist
1. Identify highest-risk changed branches.
2. Add tests for happy path + main failure paths.
3. Assert status and payload shape.
4. Run narrow verification before broader runs.

## Deep Workflow
1. Scope risk by change type: contract-only, behavior-only, or both.
2. Create/adjust tests for the highest-risk branches first:
   - Happy path
   - Validation failure
   - Not found/conflict path
3. Assert both status code and response payload shape (including error details where applicable).
4. Keep tests feature-local and mirror endpoint route intent.
5. Run narrow verification first (targeted build/test command), then widen only if needed.
6. Report coverage gaps and next tests to add if time-constrained.

## Decision Points
- If behavior changed without contract change: prioritize branch logic tests.
- If contract changed: prioritize serialization and response shape tests.
- If bug fix: add regression test reproducing pre-fix failure.

## Completion Checks
- Tests fail before fix and pass after fix for changed behavior.
- Assertions cover status + payload, not status alone.
- Verification commands are minimal but sufficient for touched backend scope.
