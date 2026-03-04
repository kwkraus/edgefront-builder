# SPEC-200 — Teams Integration & Data Sync (Build Ready)

## Graph Permission Model (Delegated Only)
- **Delegated (OBO flow):** All Graph operations — requires `VirtualEvent.ReadWrite`
  - User must have Teams account with webinar-capable license
  - Backend exchanges user's access token via OBO for Graph delegated token
  - If Graph rejects due to missing license/capability: show clear error explaining Teams webinar licensing is required
- **No application permissions** — all data operations require an authenticated user
- Token acquisition centralized in a TeamsGraphClient service

## App Registration — Required Delegated Permissions
| Permission | Type | Purpose |
|---|---|---|
| `openid` | Delegated | OIDC sign-in |
| `profile` | Delegated | User profile claims |
| `email` | Delegated | Email claim |
| `offline_access` | Delegated | Refresh token for silent renewal |
| `VirtualEvent.ReadWrite` | Delegated | Create, read, update, delete Teams webinars, registrations, and attendance |
| `OnlineMeetingArtifact.Read.All` | Delegated | Read attendance reports for webinar sessions (required by Graph for `/attendanceReports` endpoint) |

**Permissions to NOT configure (removed):**
- ~~`VirtualEvent.Read.All` (Application)~~ — replaced by delegated VirtualEvent.ReadWrite
- ~~`VirtualEvent.Read.Chat` (Application)~~ — replaced by delegated VirtualEvent.ReadWrite

**Exposed API scope:** `api://{ClientId}/access_as_user` — frontend requests this scope; backend validates it and uses OBO to exchange for Graph token.

## Publish Flow
On Series publish (delegated — user present):
- Create Teams webinar per session via OBO token
- Store teamsWebinarId
- If publish fails after partial remote creation, run compensating rollback (best-effort delete created webinars) before returning failure
- If compensating rollback itself fails: log failures, surface partial-failure state to user
- If Graph rejects webinar creation due to licensing: surface "Teams webinar license required" error; do not retry

## Data Sync (Replaces Webhooks)
- No webhooks or background services — all data sync is user-initiated
- **Session sync:** Triggered automatically when user opens a session detail page
  - Fetches registrations and attendance from Graph via OBO token
  - Normalizes, deduplicates, and upserts into local database
  - Triggers metrics recompute
  - Updates `LastSyncAt` timestamp on session
- **Series sync:** Triggered automatically when user opens a series detail page
  - Iterates all published sessions and syncs each
  - Individual session failures are logged but do not block other sessions
- Sync is idempotent — safe to call multiple times
- Frontend shows "Syncing from Teams…" indicator during sync
- Frontend shows "Last Synced" timestamp (relative format) per session

## Drift Detection
- On Published session load: fetch metadata from Graph (OBO token) and compare title, start, end
- Cache drift check results for 5 minutes per session to avoid excessive Graph calls
- If mismatch → set driftStatus = DriftDetected; do not auto-overwrite

## Delete
- Deleting a Session (Published): delete Teams webinar via OBO token
- Deleting a Series: cascade through all sessions, best-effort delete their Teams webinars

## Definition of Done
- Integration tests for publish, sync, delete
- OBO token exchange tested
- Licensing error handling verified
- Drift detection caching verified
- Sync idempotency verified
