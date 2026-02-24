# SPEC-300 — Metrics Engine & Aggregation (Build Ready)

## Scope
Defines deterministic recompute logic for SessionMetrics and SeriesMetrics.

## Internal Domain Exclusion
Before computing influence and warm metrics, filter out all attendance and registration rows whose `emailDomain` appears in the configured internal-domain exclusion list (see SPEC-010). Internal domains are still stored in normalized tables but excluded from all metric aggregations below.

## SessionMetrics
- totalRegistrations (all domains, including internal)
- totalAttendees (all domains, including internal)
- uniqueRegistrantAccountDomains (external only)
- uniqueAttendeeAccountDomains (external only)
- warmAccountsTriggered (W1, external only)

## SeriesMetrics
- totalRegistrations (all domains, including internal)
- totalAttendees (all domains, including internal)
- uniqueRegistrantAccountDomains (external only)
- uniqueAccountsInfluenced (attendance-only, external only)
- warmAccounts (one entry per domain, W2 > W1 precedence, external only)

## Triggers
- Registration webhook
- Attendance webhook
- Reconciliation completion

## Transaction Rule
Normalized upserts + metrics recompute must be atomic.

## Storage
- `SessionMetrics.warmAccountsTriggered`: JSON column (string array)
- `SeriesMetrics.warmAccounts`: JSON column (array of {accountDomain, warmRule})

## Testing
- Comprehensive unit tests (W1/W2, distinct counts, internal domain exclusion)
- Integration tests for concurrency & idempotency
- Verify internal domains do not appear in influence or warm results

## Definition of Done
- No metric inflation under duplicate webhook delivery
- Metrics endpoints read persisted aggregates only
- Internal domain exclusion covered by unit tests
