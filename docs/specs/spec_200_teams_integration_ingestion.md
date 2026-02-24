# SPEC-200 — Teams Integration & Ingestion (Build Ready)

## Publish Flow
On Series publish:
- Create Teams webinar per session
- Store teamsWebinarId
- Create subscriptions (Registration, AttendanceReport)
- If publish fails after partial remote creation, run compensating rollback (best-effort delete created webinars/subscriptions) before returning failure

## Webhook Handling
- Webhook endpoint is machine-authenticated (no user JWT)
- Validate clientState
- Map subscription → session
- Normalize + upsert
- Trigger metrics recompute

## Reconciliation
Attendance report ready:
- Fetch authoritative registrations + attendance
- Upsert
- Delete missing
- Recompute
- Delete subscriptions

## Renewal
- Background hosted service
- Renew before expiration
- 24h retry window then disable

## Definition of Done
- Integration tests for publish, webhook, reconcile, renewal
