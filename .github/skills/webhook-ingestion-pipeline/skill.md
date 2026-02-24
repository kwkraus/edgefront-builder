---
name: webhook-ingestion-pipeline
description: 'Implement the webhook ingestion pipeline: validate, normalize, deduplicate, upsert, and trigger metrics recompute per SPEC-200/210/300.'
argument-hint: 'Describe the webhook event type, ingestion step, or reconciliation behavior to implement.'
---

# Webhook Ingestion Pipeline

## When to Use
- Implementing webhook endpoint validation and handshake
- Building the normalize → upsert → recompute flow
- Implementing reconciliation (attendance report ready)
- Ensuring idempotency for duplicate webhook delivery
- Writing integration tests for ingestion flows

## Quick Checklist
1. Validate webhook per SPEC-210 (handshake, clientState, single-tenant).
2. Identify session by subscription → teamsWebinarId mapping.
3. Normalize + upsert into NormalizedRegistration or NormalizedAttendance.
4. Trigger atomic metrics recompute per SPEC-300.
5. Verify idempotency — duplicates must not inflate metrics.

## Webhook Validation (SPEC-210)
1. If `validationToken` query param present: echo it back with 200 (handshake).
2. Validate `clientState` against stored hashed value.
3. Reject unknown subscriptionId.
4. Enforce single-tenant check.
5. Log security events with correlationId.

## Registration Webhook Flow
1. Extract notification payload.
2. If payload incomplete: fetch changed registration(s) from Graph (application token).
3. Normalize email (trim + lowercase) and domain (eTLD+1).
4. Upsert into NormalizedRegistration using unique constraint (ownerUserId, sessionId, email).
5. Within same transaction: recompute SessionMetrics + SeriesMetrics.
6. Commit atomically.

## Attendance Report Ready Flow (Reconciliation)
1. Fetch authoritative attendance report from Graph (application token).
2. Fetch authoritative registrations from Graph (application token).
3. Normalize all records.
4. Within a single transaction:
   a. Upsert all normalized attendance records.
   b. Upsert all normalized registration records.
   c. Delete local rows not present in authoritative sets.
   d. Recompute SessionMetrics(sessionId).
   e. Recompute SeriesMetrics(seriesId).
   f. Set reconcileStatus = Synced (if successful).
   g. Delete GraphSubscription records for this session.
5. Commit atomically.
6. If fetch fails: set reconcileStatus = Retrying, auto-retry with backoff.

## Idempotency Rules
- Unique constraints prevent duplicate rows: (ownerUserId, sessionId, email).
- Upsert semantics: INSERT or UPDATE on conflict.
- Metrics are always recomputed from current normalized data — never incremented.
- Full recompute guarantees correctness regardless of duplicate delivery.

## Error Handling
- Webhook processing failures must not crash the API.
- Failed ingestion: log with correlationId, return 200 to Graph (acknowledge receipt).
- Failed reconciliation fetch: set reconcileStatus = Retrying, schedule retry.
- After 24h of failed retries: set reconcileStatus = Disabled, stop retrying.

## Decision Points
- If notification type is unknown: log and ignore (do not fail).
- If session not found for subscription: log warning and acknowledge.
- If Graph data fetch fails during reconciliation: retry, do not partially commit.
- If concurrent webhooks arrive for same session: DB transaction isolation prevents race conditions.

## Mandatory Integration Tests (SPEC-200 §10, SPEC-300 §8)
- Registration webhook → normalized upsert → metrics updated
- Attendance report ready → full reconcile → metrics updated
- Duplicate webhook does not inflate metrics
- Concurrent events for same session → no inconsistent metrics
- Reconciliation deletes stale rows and updates metrics correctly

## Completion Checks
- Handshake echo implemented and tested.
- clientState validation enforced.
- Normalize → upsert → recompute pipeline is atomic.
- Idempotency proven by duplicate delivery tests.
- Reconciliation handles fetch failure with retry.
