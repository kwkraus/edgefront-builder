# SPEC-210 — Webhook Security & Validation (Build Ready)

## Endpoint
POST /api/v1/webhooks/graph

## Handshake
- Echo validationToken

## Security
- Validate clientState (stored hashed)
- Reject unknown subscription
- Single-tenant enforcement

## Replay Handling
- Optional dedupe store (24h TTL)
- Idempotent ingestion required

## Logging
- CorrelationId per request
- Required security event logs

## Definition of Done
- Handshake implemented
- clientState validation enforced
- Duplicate handling validated via tests

