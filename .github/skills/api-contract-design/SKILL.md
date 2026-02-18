---
name: api-contract-design
description: 'Design and refine ASP.NET Core minimal API request/response contracts. Use for DTO-first endpoint work, validation-ready shapes, OpenAPI metadata, and contract compatibility decisions.'
argument-hint: 'Describe endpoint purpose, route, request fields, response variants, and compatibility constraints.'
---

# API Contract Design

## When to Use
- New minimal API endpoints in `src/backend`
- Changes to request/response DTOs
- OpenAPI contract clarification before implementation
- Backward-compatibility decisions for existing clients

## Quick Checklist
1. Confirm route + verb match resource intent.
2. Define request/response DTOs (no domain entities).
3. Map each branch to status + payload shape.
4. Mark compatibility impact (additive/soft-breaking/hard-breaking).

## Inputs to Gather
1. Route and verb (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`)
2. Resource model and operation intent
3. Required vs optional fields
4. Success and failure response variants
5. Compatibility constraints (breaking vs additive)

## Deep Workflow
1. Define endpoint intent in one sentence and choose resource-oriented route shape.
2. Draft request and response DTOs as records/classes; never expose domain entities.
3. Add explicit field semantics (required, nullable, defaults, allowed ranges/enums).
4. Map each response variant to a concrete HTTP status and payload shape.
5. Verify contract coherence with minimal API metadata and OpenAPI output expectations.
6. Confirm whether the change is additive, soft-breaking, or hard-breaking.

## Decision Points
- If operation mutates state, include idempotency expectations.
- If partial update is needed, decide `PATCH` semantics and null-handling behavior.
- If conflicts are possible, include a deterministic conflict response contract.

## Completion Checks
- Request/response DTOs are explicit and serialization-safe.
- No domain entity types appear in public API signatures.
- Every documented status code has a defined payload contract.
- Route, verb, and DTO names align with resource intent.
