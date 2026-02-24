# SPEC-200 — Teams Integration & Ingestion (Build Ready)

## Graph Permission Model (Hybrid)
- **Delegated (OBO flow):** Webinar create, update, delete — requires `VirtualEvent.ReadWrite`
  - User must have Teams account with webinar-capable license
  - Backend exchanges user's access token via OBO for Graph delegated token
  - If Graph rejects due to missing license/capability: show clear error explaining Teams webinar licensing is required
- **Application (client credentials):** Subscriptions, registration reads, attendance reads, background renewal — requires `VirtualEvent.Read.All` + `VirtualEvent.Read.Chat`
  - App registration requires application access policy assigned to authorized users
- Token acquisition centralized in a TeamsGraphClient service

## Publish Flow
On Series publish (delegated — user present):
- Create Teams webinar per session via OBO token
- Store teamsWebinarId
- Create subscriptions via application token (Registration, AttendanceReport)
- If publish fails after partial remote creation, run compensating rollback (best-effort delete created webinars/subscriptions) before returning failure
- If compensating rollback itself fails: log failures, surface partial-failure state to user
- If Graph rejects webinar creation due to licensing: surface "Teams webinar license required" error; do not retry

## Webhook Handling
- Webhook endpoint is machine-authenticated (no user JWT)
- Validate clientState
- Map subscription → session
- Normalize + upsert
- Trigger metrics recompute

## Drift Detection
- On Published session load: fetch metadata from Graph (application token) and compare title, start, end
- Cache drift check results for 5 minutes per session to avoid excessive Graph calls
- If mismatch → set driftStatus = DriftDetected; do not auto-overwrite

## Reconciliation
Attendance report ready (application token — no user present):
- Fetch authoritative registrations + attendance
- Upsert
- Delete missing
- Recompute
- Delete subscriptions

## Delete
- Deleting a Session (Published): delete Teams webinar via OBO token + subscriptions via application token
- Deleting a Series: cascade through all sessions, best-effort delete their Teams webinars + subscriptions

## Renewal
- Background hosted service (application token)
- Renew before expiration
- 24h retry window then disable

## Definition of Done
- Integration tests for publish, webhook, reconcile, renewal, delete
- OBO token exchange tested
- Licensing error handling verified
- Drift detection caching verified
