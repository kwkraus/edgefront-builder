# SPEC-010 — Domain & Identity (Build Ready)

## Authority
Defines identity model, normalization rules, influence logic, warm rules.

## Identity
- Person = normalized email
- Account = normalized registrable domain (public-suffix-aware eTLD+1 parsing)
- Subdomains ignored after registrable-domain normalization
- Internal domains excluded using validated environment/config list

## Influence
- Attendance-only
- Registration stored but not counted toward influence

## Warm Rules (Per Series)
- W1: ≥2 distinct emails from same domain in one session
- W2: Same email attends ≥2 sessions in same series
- Precedence: W2 > W1
- Store one warm entry per domain (do not store both W1 and W2 for same domain)

## Session Lifecycle
`status`: Draft → Published
`reconcileStatus` tracks reconciliation state separately

## Delete Behavior
- Deleting a Session (Published): deletes mapped Teams webinar + subscriptions (with user confirmation)
- Deleting a Series: cascade-deletes all Sessions and best-effort deletes their Teams webinars + subscriptions (with user confirmation)

## Reconciliation
Attendance report triggers authoritative re-fetch and recompute.

## Acceptance Tests
- Domain normalization
- Attendance-only influence
- W1/W2 correctness
- Internal exclusion

## Definition of Done
- Comprehensive unit + integration tests
