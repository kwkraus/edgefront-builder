# SPEC-110 — API Surface & OpenAPI Contract (Build Ready)

## Principles
- All user/business endpoints authenticated via JWT (Entra ID)
- Webhook endpoint is machine-authenticated via Graph notification validation controls
- Contract-first (OpenAPI source of truth)
- Metrics read-only
- No pagination in V1 — list endpoints return all results for the authenticated user

## Base Path
/api/v1

## Core Endpoints

### Series
- `GET /series` → SeriesListItem[]
- `POST /series` ← CreateSeriesRequest → SeriesResponse (201)
- `GET /series/{id}` → SeriesResponse
- `PUT /series/{id}` ← UpdateSeriesRequest → SeriesResponse
- `DELETE /series/{id}` → 204 (cascade-deletes sessions + best-effort Teams cleanup)
- `POST /series/{id}:publish` → SeriesResponse (triggers Teams webinar creation)

### Sessions
- `GET /series/{id}/sessions` → SessionListItem[]
- `POST /series/{id}/sessions` ← CreateSessionRequest → SessionResponse (201)
- `GET /sessions/{id}` → SessionResponse
- `PUT /sessions/{id}` ← UpdateSessionRequest → SessionResponse (Save & Publish if Published)
- `DELETE /sessions/{id}` → 204 (deletes Teams webinar if Published, requires confirmation from UI)

### Metrics
- `GET /series/{id}/metrics` → SeriesMetricsResponse
- `GET /sessions/{id}/metrics` → SessionMetricsResponse

### Webhook
- `POST /webhooks/graph` (machine-authenticated, not JWT)

## Request DTOs

### CreateSeriesRequest
```json
{ "title": "string" }
```

### UpdateSeriesRequest
```json
{ "title": "string" }
```

### CreateSessionRequest
```json
{ "title": "string", "startsAt": "datetime", "endsAt": "datetime" }
```

### UpdateSessionRequest
```json
{ "title": "string", "startsAt": "datetime", "endsAt": "datetime" }
```

## Response DTOs

### SeriesListItem
```json
{
  "seriesId": "uuid",
  "title": "string",
  "status": "Draft | Published",
  "sessionCount": "int",
  "totalRegistrations": "int",
  "totalAttendees": "int",
  "uniqueAccountsInfluenced": "int",
  "hasReconcileIssues": "bool",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### SeriesResponse
```json
{
  "seriesId": "uuid",
  "title": "string",
  "status": "Draft | Published",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### SessionListItem
```json
{
  "sessionId": "uuid",
  "title": "string",
  "startsAt": "datetime",
  "endsAt": "datetime",
  "status": "Draft | Published",
  "reconcileStatus": "Synced | Reconciling | Retrying | Disabled",
  "driftStatus": "None | DriftDetected",
  "totalRegistrations": "int",
  "totalAttendees": "int"
}
```

### SessionResponse
```json
{
  "sessionId": "uuid",
  "seriesId": "uuid",
  "title": "string",
  "startsAt": "datetime",
  "endsAt": "datetime",
  "status": "Draft | Published",
  "teamsWebinarId": "string | null",
  "reconcileStatus": "Synced | Reconciling | Retrying | Disabled",
  "driftStatus": "None | DriftDetected",
  "lastSyncAt": "datetime | null",
  "lastError": "string | null"
}
```

### SeriesMetricsResponse
```json
{
  "seriesId": "uuid",
  "totalRegistrations": "int",
  "totalAttendees": "int",
  "uniqueRegistrantAccountDomains": "int",
  "uniqueAccountsInfluenced": "int",
  "warmAccounts": [{ "accountDomain": "string", "warmRule": "W1 | W2" }]
}
```

### SessionMetricsResponse
```json
{
  "sessionId": "uuid",
  "totalRegistrations": "int",
  "totalAttendees": "int",
  "uniqueRegistrantAccountDomains": "int",
  "uniqueAttendeeAccountDomains": "int",
  "warmAccountsTriggered": ["string"]
}
```

## Error Envelope
```json
{ "errorCode": "string", "message": "string", "correlationId": "string", "details": "object | null" }
```

## Definition of Done
- OpenAPI YAML present and matches DTOs above
- CI validation enabled
