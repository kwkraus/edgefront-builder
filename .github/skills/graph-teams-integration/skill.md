---
name: graph-teams-webinar-integration
description: 'Design delegated Microsoft Graph and Teams webinar integration for the backend. Use for OBO token exchange, webinar CRUD, registrations and attendance reads, drift handling, and Graph error semantics.'
argument-hint: 'Describe the Graph operation, Teams webinar context, delegated scopes or token flow, and any failure or retry concern.'
---

# Graph Teams Integration

Canonical owner of Graph endpoints, OBO flow, and webinar lifecycle.

## When to Use
- Webinar create/update/delete via Graph
- OBO token exchange
- User-initiated data sync (registrations, attendance)
- Drift detection vs Graph metadata
- Any Microsoft.Identity.Web / Microsoft.Graph SDK work

## Quick Checklist
1. All Graph ops use OBO — user must be authenticated.
2. Verify endpoint + delegated permissions.
3. Centralize token acquisition via `TeamsGraphClient` + `OboTokenService`.
4. Error handling with correlation IDs + Graph-specific classification.

## Operation → Scope

| Operation | Flow | Delegated scope |
|-----------|------|-----------------|
| Webinar CRUD | OBO | `VirtualEvent.ReadWrite` |
| Registration reads | OBO | `VirtualEvent.ReadWrite` |
| Attendance reads | OBO | `OnlineMeetingArtifact.Read.All` |
| Drift detection | OBO | (delegated) |

**No client credentials** — all operations require an authenticated user.

## Token Flow (OBO)
Extract user JWT from request → `ITokenAcquisition.GetAccessTokenForUserAsync` with Graph scopes.

## Error Classification

| Code | Class | Action |
|------|-------|--------|
| 401/403 | Token/permission | Log + surface clearly |
| 404 | Not found | Handle gracefully (drift/delete) |
| 429 | Throttled | Respect `Retry-After` |
| 5xx | Transient | Exponential backoff retry |

Map Graph response → domain model (e.g. webinar id → `teamsWebinarId`). Log all ops with correlation id, operation name, result.

## Publish Flow
For each session in series:
1. Create webinar (OBO) → store `teamsWebinarId`
2. Publish webinar (OBO) → `POST .../publish`

On any failure: best-effort rollback (delete created webinars); if rollback fails log + surface partial-failure state; return failure.

## Data Sync
1. User opens session/series page → sync triggered.
2. Registrations (OBO): `GET /solutions/virtualEvents/webinars/{id}/registrations`
3. Attendance (OBO): sessions → attendanceReports → attendanceRecords
4. Hand off to normalize → upsert → recompute pipeline (see `delegated-data-sync-pipeline`).
5. Update `LastSyncAt` on session.

Drift cache: 5-minute window.

## Decision Points
- Licensing error (e.g. no Teams Premium) → non-retryable; clear user message.
- Drift fetch fails → keep previous `driftStatus`.
- Webinar delete fails during series/session delete → log best-effort, continue local delete.

## Completion Checks
- All Graph calls OBO (no client credentials)
- All calls have correlation-id error handling
- Publish atomic with compensating rollback
- Integration tests mock Graph for all flows
