# SPEC-120 — Data Schema & Migrations (Build Ready)

## Tables

### Series
- seriesId (UUID, PK)
- ownerUserId (string, Entra OID)
- title (string)
- status (string: Draft | Published)
- createdAt (datetime2, UTC)
- updatedAt (datetime2, UTC)

### Session
- sessionId (UUID, PK)
- seriesId (UUID, FK → Series)
- ownerUserId (string, Entra OID)
- title (string)
- startsAt (datetime2, UTC)
- endsAt (datetime2, UTC)
- status (string: Draft | Published)
- teamsWebinarId (string, nullable)
- driftStatus (string: None | DriftDetected, default None)
- reconcileStatus (string: Synced | Reconciling | Retrying | Disabled, default Synced)
- lastSyncAt (datetime2, nullable)
- lastError (string, nullable)

### NormalizedRegistration
- registrationId (UUID, PK)
- sessionId (UUID, FK → Session)
- ownerUserId (string)
- email (string, normalized)
- emailDomain (string, normalized registrable domain)
- registeredAt (datetime2, UTC)

### NormalizedAttendance
- attendanceId (UUID, PK)
- sessionId (UUID, FK → Session)
- ownerUserId (string)
- email (string, normalized)
- emailDomain (string, normalized registrable domain)
- attended (bool, always true)
- durationSeconds (int, nullable)
- durationPercent (decimal, nullable)
- firstJoinAt (datetime2, nullable)
- lastLeaveAt (datetime2, nullable)

### GraphSubscription
- graphSubscriptionId (UUID, PK)
- sessionId (UUID, FK → Session)
- ownerUserId (string)
- subscriptionId (string, Graph subscription id)
- changeType (string: registration | attendanceReport)
- clientStateHash (string, hashed clientState)
- expirationDateTime (datetime2, UTC)
- createdAt (datetime2, UTC)

### SessionMetrics
- sessionId (UUID, PK, FK → Session)
- totalRegistrations (int)
- totalAttendees (int)
- uniqueRegistrantAccountDomains (int)
- uniqueAttendeeAccountDomains (int)
- warmAccountsTriggered (nvarchar(max), JSON string array)

### SeriesMetrics
- seriesId (UUID, PK, FK → Series)
- totalRegistrations (int)
- totalAttendees (int)
- uniqueRegistrantAccountDomains (int)
- uniqueAccountsInfluenced (int)
- warmAccounts (nvarchar(max), JSON array of {accountDomain, warmRule})

## Key Constraints
- Unique (ownerUserId, title) for Series
- Unique (ownerUserId, sessionId, email) for NormalizedRegistration
- Unique (ownerUserId, sessionId, email) for NormalizedAttendance
- Unique (sessionId, subscriptionId) for GraphSubscription
- FK cascade deletes: Series → Session → NormalizedRegistration, NormalizedAttendance, GraphSubscription, SessionMetrics

## Indexes
- Series(ownerUserId, createdAt desc)
- Session(seriesId, startsAt)
- NormalizedRegistration(sessionId, emailDomain)
- NormalizedAttendance(sessionId, emailDomain)
- GraphSubscription(sessionId)
- GraphSubscription(expirationDateTime) — for renewal queries

## Conventions
- UUID PKs
- UTC datetime2
- EF Core migrations only
- JSON columns use nvarchar(max)

## Definition of Done
- Initial migration created
- All unique constraints validated via integration tests
- GraphSubscription lifecycle validated via integration tests

