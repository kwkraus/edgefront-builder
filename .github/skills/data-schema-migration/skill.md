---
name: efcore-schema-and-migrations
description: 'Design EF Core schema changes, constraints, and migrations for the backend data model. Use for entity updates, indexes, uniqueness rules, cascade behavior, and migration planning.'
argument-hint: 'Describe the entity or table change, columns or constraints affected, and any migration or data-compatibility concerns.'
---

# Data Schema and Migration

Canonical schema reference for the backend data model.

## When to Use
- Create/modify EF Core entity configurations
- Add/update migrations
- Implement unique constraints, indexes, FK cascades
- Validate schema alignment

## Quick Checklist
1. Column types match schema below.
2. Unique constraints + FK cascades match.
3. Indexes match.
4. `dotnet ef migrations add`; verify applies cleanly.

## Schema

### Series
- `seriesId` (UUID, PK), `ownerUserId` (string), `title` (string), `status` (Draft|Published), `createdAt` (datetime2 UTC), `updatedAt` (datetime2 UTC)
- Unique: `(ownerUserId, title)`

### Session
- `sessionId` (UUID, PK), `seriesId` (FK→Series), `ownerUserId`, `title`, `startsAt`, `endsAt`, `status` (Draft|Published), `teamsWebinarId` (nullable), `driftStatus` (None|DriftDetected), `reconcileStatus` (Synced|Reconciling|Retrying|Disabled), `lastSyncAt` (nullable), `lastError` (nullable)

### NormalizedRegistration
- `registrationId` (UUID, PK), `sessionId` (FK→Session), `ownerUserId`, `email`, `emailDomain`, `registeredAt`
- Unique: `(ownerUserId, sessionId, email)`

### NormalizedAttendance
- `attendanceId` (UUID, PK), `sessionId` (FK→Session), `ownerUserId`, `email`, `emailDomain`, `attended` (bool), `durationSeconds` (nullable), `durationPercent` (nullable), `firstJoinAt` (nullable), `lastLeaveAt` (nullable)
- Unique: `(ownerUserId, sessionId, email)`

### SessionMetrics
- `sessionId` (UUID, PK, FK→Session), `totalRegistrations`, `totalAttendees`, `uniqueRegistrantAccountDomains`, `uniqueAttendeeAccountDomains`, `warmAccountsTriggered` (nvarchar(max) JSON)

### SeriesMetrics
- `seriesId` (UUID, PK, FK→Series), `totalRegistrations`, `totalAttendees`, `uniqueRegistrantAccountDomains`, `uniqueAccountsInfluenced`, `warmAccounts` (nvarchar(max) JSON)

## EF Core Conventions
- UUID PKs client-generated
- All datetimes are datetime2, stored UTC
- JSON columns: nvarchar(max) + value converters
- FK cascade: Series → Session → all children
- Explicit migrations only — no auto-migration, no `EnsureCreated`

## Indexes
- `Series(ownerUserId, createdAt desc)`
- `Session(seriesId, startsAt)`
- `NormalizedRegistration(sessionId, emailDomain)`
- `NormalizedAttendance(sessionId, emailDomain)`

## Decision Points
- Type mismatch between code and schema above → ask which is correct.
- New column not in schema → ask user for definition.
- Migration conflict → rebase; do not edit existing migrations.

## Completion Checks
- Entity configs match schema exactly
- Unique constraints enforced + integration-tested
- Indexes created per reference
- Migration applies cleanly to empty DB
- JSON converters work for warm-account lists
