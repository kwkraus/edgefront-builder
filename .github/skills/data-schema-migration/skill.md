---
name: efcore-schema-and-migrations
description: 'Design EF Core schema changes, constraints, and migrations for the backend data model. Use for entity updates, indexes, uniqueness rules, cascade behavior, and migration planning.'
argument-hint: 'Describe the entity or table change, columns or constraints affected, and any migration or data-compatibility concerns.'
---

# Data Schema and Migration

## When to Use
- Creating or modifying EF Core entity configurations
- Adding or updating database migrations
- Implementing unique constraints, indexes, or FK cascades
- Validating schema alignment with project domain model

## Quick Checklist
1. Verify all columns match schema reference type definitions.
2. Verify all unique constraints and FK cascades per schema reference below.
3. Verify all indexes per schema reference below.
4. Generate migration via `dotnet ef migrations add`.
5. Validate migration applies cleanly.

## Schema Reference
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
- If a column type in code doesn't match the schema reference below, ask the user which is correct.
- If a new column is needed that isn't in the schema reference, ask the user for the column definition.
- If migration conflicts arise: resolve by rebasing, not by editing existing migrations.

## Completion Checks
- All entity configurations match schema reference column definitions exactly.
- All unique constraints are enforced and validated via integration tests.
- All indexes are created per schema reference.
- Migration applies cleanly to empty database.
- JSON value converters work correctly for warm account lists.
