# SPEC-300 — Metrics Engine & Aggregation (Build Ready)

## Scope
Defines deterministic recompute logic for SessionMetrics and SeriesMetrics.

## SessionMetrics
- totalRegistrations
- totalAttendees
- uniqueRegistrantAccountDomains
- uniqueAttendeeAccountDomains
- warmAccountsTriggered (W1)

## SeriesMetrics
- totalRegistrations
- totalAttendees
- uniqueRegistrantAccountDomains
- uniqueAccountsInfluenced (attendance-only)
- warmAccounts (one entry per domain, W2 > W1 precedence)

## Triggers
- Registration webhook
- Attendance webhook
- Reconciliation completion

## Transaction Rule
Normalized upserts + metrics recompute must be atomic.

## Testing
- Comprehensive unit tests (W1/W2, distinct counts)
- Integration tests for concurrency & idempotency

## Definition of Done
- No metric inflation under duplicate webhook delivery
- Metrics endpoints read persisted aggregates only
