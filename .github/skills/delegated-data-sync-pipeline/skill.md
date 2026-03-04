---
name: delegated-data-sync-pipeline
description: 'Implement the delegated data sync pipeline: fetch from Graph, normalize, deduplicate, upsert, and trigger metrics recompute per SPEC-200/300.'
argument-hint: 'Describe the sync step, normalization behavior, or idempotency concern to implement.'
---

# Delegated Data Sync Pipeline

## When to Use
- Building the fetch → normalize → upsert → recompute flow for registrations/attendance
- Implementing user-initiated sync triggered on page load
- Ensuring idempotency for repeated sync operations
- Writing integration tests for sync flows

## Quick Checklist
1. User opens session/series detail page → triggers sync via OBO token.
2. Fetch registrations and attendance from Graph API (delegated).
3. Normalize + upsert into NormalizedRegistration or NormalizedAttendance.
4. Trigger atomic metrics recompute per SPEC-300.
5. Update `LastSyncAt` timestamp on session.
6. Verify idempotency — repeated syncs must not inflate metrics.

## Session Sync Flow (SPEC-200)
1. Receive sync request with user's OBO token.
2. Fetch registrations from Graph: `GET /solutions/virtualEvents/webinars/{id}/registrations`.
3. Fetch attendance from Graph via sessions → attendanceReports → attendanceRecords.
4. Normalize email (trim + lowercase) and domain (eTLD+1).
5. Upsert into NormalizedRegistration using unique constraint (ownerUserId, sessionId, email).
6. Upsert into NormalizedAttendance using unique constraint.
7. Within same transaction: recompute SessionMetrics + SeriesMetrics.
8. Update `LastSyncAt` timestamp on session.
9. Commit atomically.

## Series Sync Flow (SPEC-200)
1. Iterate all published sessions in the series.
2. Sync each session individually.
3. Individual session failures are logged but do not block other sessions.

## Idempotency Rules
- Unique constraints prevent duplicate rows: (ownerUserId, sessionId, email).
- Upsert semantics: INSERT or UPDATE on conflict.
- Metrics are always recomputed from current normalized data — never incremented.
- Full recompute guarantees correctness regardless of repeated sync calls.

## Error Handling
- Sync failures surface to user with retry option (inline error banner).
- Failed Graph fetch: log with correlationId, surface error to frontend.
- If Graph returns licensing error: surface "Teams webinar license required" message.

## Decision Points
- If session not found: return 404.
- If Graph data fetch fails: surface error, do not partially commit.
- If concurrent syncs arrive for same session: DB transaction isolation prevents race conditions.

## Mandatory Integration Tests (SPEC-200, SPEC-300 §8)
- Sync → normalized upsert → metrics updated
- Repeated sync does not inflate metrics
- Concurrent syncs for same session → no inconsistent metrics
- Partial Graph failure → no partial commit

## Completion Checks
- Normalize → upsert → recompute pipeline is atomic.
- Idempotency proven by repeated sync tests.
- LastSyncAt updated correctly.
- Error states surface clearly to frontend.
