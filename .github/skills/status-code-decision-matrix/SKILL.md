---
name: status-code-decision-matrix
description: 'Select correct HTTP status codes and payload behaviors for ASP.NET Core minimal APIs. Use for create/update/delete/read semantics, validation failures, conflicts, and not-found handling.'
argument-hint: 'Describe operation type, expected outcomes, and known failure modes.'
---

# Status Code Decision Matrix

## When to Use
- Endpoint behavior design or review in `src/backend`
- Inconsistent status code handling across endpoints
- PR review focused on HTTP semantics

## Quick Checklist
1. Classify operation type and expected outcomes.
2. Assign canonical success status.
3. Assign specific 4xx/5xx statuses per failure cause.
4. Document and justify compatibility exceptions.

## Deep Workflow
1. Classify operation type: read, create, replace, partial update, delete, command/action.
2. Enumerate all outcomes: success, validation failure, authz/authn failure, not found, conflict, transient dependency failure.
3. Select success status:
   - Read single/list: `200`
   - Create: `201` with location when resource URI is available
   - Delete with no body: `204`
   - Command accepted async: `202`
4. Select client-error status:
   - Validation/input errors: `400` with problem details
   - Authn/authz: `401`/`403`
   - Missing resource: `404`
   - State/version conflict: `409`
5. Select server/dependency status when applicable (`500`/`503`) and ensure no sensitive details leak.
6. Verify endpoint metadata and tests assert status + payload shape.

## Decision Rules
- Prefer specific 4xx statuses over generic `400` when cause is known.
- Use `404` for unknown identifiers, not for authorization failures.
- Use `409` when request is valid but current resource state prevents execution.
- When existing clients depend on non-canonical behavior, keep compatibility and annotate a migration plan.

## Completion Checks
- Every expected branch maps to one status code.
- Success paths and failure paths are both testable and documented.
- Status behavior is consistent with existing endpoint patterns.
