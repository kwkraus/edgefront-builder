# SPEC-120 — Data Schema & Migrations (Build Ready)

## Tables
- Series
- Session
- NormalizedRegistration
- NormalizedAttendance
- GraphSubscription
- SessionMetrics
- SeriesMetrics

## Key Constraints
- Unique (ownerUserId, title) for Series
- Unique (ownerUserId, sessionId, email) for registrations & attendance
- FK cascade deletes

## Indexes
- Series(ownerUserId, createdAt desc)
- Session(seriesId, startsAt)
- Domain indexes on normalized tables

## Conventions
- UUID PKs
- UTC datetime2
- EF Core migrations only

## Definition of Done
- Initial migration created
- Unique constraints validated via integration tests

