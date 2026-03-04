---
name: data-schema-migration
description: 'Design and implement EF Core database schema, migrations, and constraint validation per SPEC-120.'
argument-hint: 'Describe the table, column, constraint, or migration change needed.'
---

# Data Schema and Migration

## When to Use
- Creating or modifying EF Core entity configurations
- Adding or updating database migrations
- Implementing unique constraints, indexes, or FK cascades
- Validating schema alignment with SPEC-120

## Quick Checklist
1. Verify all columns match SPEC-120 type definitions.
2. Verify all unique constraints and FK cascades per SPEC-120.
3. Verify all indexes per SPEC-120.
4. Generate migration via `dotnet ef migrations add`.
5. Validate migration applies cleanly.

## Schema Reference (SPEC-120)
### Series
- seriesId (UUID, PK), ownerUserId (string), title (string), status (string: Draft|Published), createdAt (datetime2 UTC), updatedAt (datetime2 UTC)
- Unique: (ownerUserId, title)

### Session
- sessionId (UUID, PK), seriesId (FK→Series), ownerUserId, title, startsAt, endsAt, status (Draft|Published), teamsWebinarId (nullable), driftStatus (None|DriftDetected), reconcileStatus (Synced|Reconciling|Retrying|Disabled), lastSyncAt (nullable), lastError (nullable)

### NormalizedRegistration
- registrationId (UUID, PK), sessionId (FK→Session), ownerUserId, email, emailDomain, registeredAt
- Unique: (ownerUserId, sessionId, email)

### NormalizedAttendance
- attendanceId (UUID, PK), sessionId (FK→Session), ownerUserId, email, emailDomain, attended (bool), durationSeconds (nullable), durationPercent (nullable), firstJoinAt (nullable), lastLeaveAt (nullable)
- Unique: (ownerUserId, sessionId, email)

### SessionMetrics
- sessionId (UUID, PK, FK→Session), totalRegistrations, totalAttendees, uniqueRegistrantAccountDomains, uniqueAttendeeAccountDomains, warmAccountsTriggered (nvarchar(max) JSON)

### SeriesMetrics
- seriesId (UUID, PK, FK→Series), totalRegistrations, totalAttendees, uniqueRegistrantAccountDomains, uniqueAccountsInfluenced, warmAccounts (nvarchar(max) JSON)

## EF Core Conventions
- UUID primary keys generated client-side.
- All datetime columns are datetime2 stored in UTC.
- JSON columns use nvarchar(max) with EF Core value converters.
- FK cascade deletes: Series → Session → all child tables.
- Migrations only — no auto-migration or ensure-created.

## Indexes
- Series(ownerUserId, createdAt desc)
- Session(seriesId, startsAt)
- NormalizedRegistration(sessionId, emailDomain)
- NormalizedAttendance(sessionId, emailDomain)

## Decision Points
- If a column type in code doesn't match SPEC-120: fix the code, not the spec.
- If a new column is needed that isn't in SPEC-120: add `TODO-SPEC` and request spec update.
- If migration conflicts arise: resolve by rebasing, not by editing existing migrations.

## Completion Checks
- All entity configurations match SPEC-120 column definitions exactly.
- All unique constraints are enforced and validated via integration tests.
- All indexes are created per SPEC-120.
- Migration applies cleanly to empty database.
- JSON value converters work correctly for warm account lists.
