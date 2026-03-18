---
name: graph-teams-webinar-integration
description: 'Design delegated Microsoft Graph and Teams webinar integration for the backend. Use for OBO token exchange, webinar CRUD, registrations and attendance reads, drift handling, and Graph error semantics.'
argument-hint: 'Describe the Graph operation, Teams webinar context, delegated scopes or token flow, and any failure or retry concern.'
---

# Graph Teams Integration

## When to Use
- Implementing webinar create/update/delete via Graph API
- Setting up OBO token exchange for delegated Graph calls
- Implementing user-initiated data sync (registrations, attendance)
- Implementing drift detection against Graph metadata
- Any work involving Microsoft.Identity.Web or Microsoft.Graph SDK

## Quick Checklist
1. All Graph operations use OBO flow — user must be authenticated.
2. Verify Graph API endpoint and delegated permission requirements.
3. Implement centralized token acquisition via TeamsGraphClient + OboTokenService.
4. Add error handling with correlation IDs and Graph-specific failure modes.

## Deep Workflow
1. Classify the operation:
   - Webinar CRUD → OBO flow (delegated `VirtualEvent.ReadWrite`)
   - Registration reads → OBO flow (delegated `VirtualEvent.ReadWrite`)
   - Attendance reads → OBO flow (delegated `OnlineMeetingArtifact.Read.All`)
   - Drift detection → OBO flow (delegated)
2. Implement token acquisition:
   - OBO: Extract user JWT from request → call `ITokenAcquisition.GetAccessTokenForUserAsync` with Graph scopes
   - **No client credentials** — all operations require an authenticated user
3. Implement the Graph API call with retry and error classification:
   - 401/403: Token or permission issue — log and surface clearly
   - 404: Resource not found — handle gracefully for drift/delete
   - 429: Throttled — respect Retry-After header
   - 5xx: Transient — retry with exponential backoff
4. Map Graph response to domain model (e.g., webinar ID → teamsWebinarId).
5. Log all Graph operations with correlation ID, operation name, and result.

## Publish Flow
1. For each session in series:
   a. Create webinar via OBO → store teamsWebinarId
   b. Publish webinar via OBO → POST .../publish
2. If any step fails:
   a. Run compensating rollback: best-effort delete created webinars
   b. If rollback fails: log failures, surface partial-failure state
   c. Return failure to caller

## Data Sync Flow
1. User opens session/series page → triggers sync.
2. Fetch registrations via OBO: `GET /solutions/virtualEvents/webinars/{id}/registrations`.
3. Fetch attendance via OBO: sessions → attendanceReports → attendanceRecords.
4. Hand off to normalize → upsert → recompute pipeline.
5. Update `LastSyncAt` timestamp on session.

## Decision Points
- If Graph API returns licensing error (e.g., user lacks Teams Premium): classify as non-retryable, surface clear message.
- If drift detection fetch fails: keep previous driftStatus, do not clear.
- If webinar delete fails during series/session delete: log as best-effort failure, continue with local delete.

## Completion Checks
- All Graph calls use OBO tokens (no client credentials).
- All Graph calls have error handling with correlation IDs.
- Publish is atomic with compensating rollback.
- Integration tests mock Graph API responses for all flows.
