# SPEC-010 — Domain & Identity (Build Ready)

## Authority
Defines identity model, normalization rules, influence logic, warm rules.

## Identity
- Person = normalized email
- Account = normalized domain
- Subdomains ignored
- Internal domains excluded

## Influence
- Attendance-only
- Registration stored but not counted toward influence

## Warm Rules (Per Series)
- W1: ≥2 distinct emails from same domain in one session
- W2: Same email attends ≥2 sessions in same series
- Precedence: W2 > W1

## Session Lifecycle
Draft → Published → Reconciled

## Reconciliation
Attendance report triggers authoritative re-fetch and recompute.

## Acceptance Tests
- Domain normalization
- Attendance-only influence
- W1/W2 correctness
- Internal exclusion

## Definition of Done
- Comprehensive unit + integration tests

