---
name: graph-delegated-sync-pipeline
description: 'Design and implement the delegated Graph sync pipeline from fetch through normalization, upsert, and metrics recompute. Use for user-initiated registrations or attendance sync, idempotency, and atomic recomputation.'
argument-hint: 'Describe the sync stage, Graph data being processed, normalization or deduplication rules, and any idempotency or transaction concern.'
---

# Delegated Data Sync Pipeline

Fetch → normalize → upsert → recompute flow, user-initiated via OBO.

## When to Use
- Implement fetch/normalize/upsert/recompute stages
- User-initiated sync on page load
- Ensure idempotency
- Integration tests for sync

## Pipeline

1. User opens session/series page → sync triggered with OBO token.
2. Fetch from Graph (see `graph-teams-integration`): registrations + attendance.
3. Normalize (see `domain-metrics-computation`): trim+lowercase email, eTLD+1 domain.
4. Upsert NormalizedRegistration / NormalizedAttendance via unique `(ownerUserId, sessionId, email)`.
5. **Within same transaction**: recompute SessionMetrics + SeriesMetrics.
6. Update `LastSyncAt` on session.
7. Commit atomically.

## Series Sync
Iterate all published sessions; sync each individually. Per-session failures logged but do not block other sessions.

## Idempotency
- Unique constraints `(ownerUserId, sessionId, email)` prevent duplicate rows.
- Upsert = INSERT or UPDATE on conflict.
- Metrics always fully recomputed from current normalized data — never incremented.
- Full recompute guarantees correctness across repeated calls.

## Error Handling
- Sync failure → surface to user with retry (inline error banner).
- Graph fetch failure → log with correlationId + surface to frontend.
- Licensing error → surface "Teams webinar license required".

## Decision Points
- Session not found → 404.
- Graph fetch fails → surface error; no partial commit.
- Concurrent syncs same session → DB transaction isolation prevents race.

## Mandatory Integration Tests
- Sync → normalized upsert → metrics updated
- Repeated sync does not inflate metrics
- Concurrent syncs same session → no inconsistency
- Partial Graph failure → no partial commit

## Completion Checks
- Normalize → upsert → recompute atomic
- Idempotency proven by tests
- `LastSyncAt` updated
- Error states surface clearly to frontend
