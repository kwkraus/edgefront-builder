# SPEC-110 — API Surface & OpenAPI Contract (Build Ready)

## Principles
- All endpoints authenticated
- Contract-first (OpenAPI source of truth)
- Metrics read-only

## Base Path
/api/v1

## Core Endpoints
Series:
- GET /series
- POST /series
- GET /series/{id}
- PUT /series/{id}
- DELETE /series/{id}
- POST /series/{id}:publish

Sessions:
- GET /series/{id}/sessions
- POST /series/{id}/sessions
- GET /sessions/{id}
- PUT /sessions/{id}
- DELETE /sessions/{id}

Metrics:
- GET /series/{id}/metrics
- GET /sessions/{id}/metrics

Webhook:
- POST /webhooks/graph

## Error Envelope
{ errorCode, message, correlationId, details? }

## Definition of Done
- OpenAPI YAML present
- CI validation enabled

