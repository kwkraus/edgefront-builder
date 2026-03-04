---
name: domain-metrics-computation
description: 'Implement domain normalization (eTLD+1), identity rules, W1/W2 warm logic, influence counting, and internal domain exclusion per SPEC-010 and SPEC-300.'
argument-hint: 'Describe the domain rule, metric computation, or normalization behavior to implement or test.'
---

# Domain and Metrics Computation

## When to Use
- Implementing email/domain normalization logic
- Implementing SessionMetrics or SeriesMetrics computation
- Implementing W1/W2 warm account rules
- Implementing influence (uniqueAccountsInfluenced) counting
- Applying internal domain exclusion filter
- Writing unit tests for any of the above

## Quick Checklist
1. Apply normalization: trim + lowercase email, eTLD+1 for domain.
2. Filter internal domains before computing influence and warm.
3. Follow SPEC-300 computation pseudocode exactly.
4. Verify warm precedence: W2 > W1, one entry per domain.

## Domain Normalization (SPEC-010)
1. Email: trim whitespace + lowercase. Identity key = normalized email.
2. Domain: extract registrable domain using public-suffix-aware eTLD+1 parsing.
   - `kt.kpmg.com` → `kpmg.com`
   - `example.co.uk` → `example.co.uk` (not `co.uk`)
   - `kpmg.com` ≠ `kpmg.au` (different TLDs = different accounts)
3. Internal domain list: loaded from environment/config, validated at startup.
4. Normalization applied before persistence — store only normalized values.

## SessionMetrics Computation (SPEC-300 §3.1)
All counts exclude internal domains for influence/warm, include for totals:
1. `totalRegistrations` = COUNT(NormalizedRegistration) — all domains
2. `totalAttendees` = COUNT(NormalizedAttendance) — all domains
3. `uniqueRegistrantAccountDomains` = COUNT(DISTINCT emailDomain) from registrations — external only
4. `uniqueAttendeeAccountDomains` = COUNT(DISTINCT emailDomain) from attendance — external only
5. `warmAccountsTriggered` (W1): for each external emailDomain in attendance, if COUNT(DISTINCT email) >= 2 → include. Lexicographic sort.

## SeriesMetrics Computation (SPEC-300 §3.2)
1. `totalRegistrations` = SUM(SessionMetrics.totalRegistrations)
2. `totalAttendees` = SUM(SessionMetrics.totalAttendees)
3. `uniqueRegistrantAccountDomains` = COUNT(DISTINCT emailDomain) across all sessions — external only
4. `uniqueAccountsInfluenced` = COUNT(DISTINCT emailDomain) across attendance in all sessions — external only
5. `warmAccounts`:
   - W1: propagate from SessionMetrics.warmAccountsTriggered
   - W2: any email in an external domain attending >= 2 distinct sessions in series
   - Precedence: W2 > W1 — store one entry per domain
   - Lexicographic sort by accountDomain

## Transaction Boundaries (SPEC-300 §5)
- Normalized upserts + metrics recompute must be atomic (single DB transaction).
- Concurrency: use database-level strategy to prevent race conditions on same session.
- If any step fails: rollback entire transaction; caller handles retry.

## Decision Points
- Registration-only records do NOT count toward influence or W2.
- Attendance with any duration (even zero) counts as attended.
- W1 does not trigger on duplicate records of same email (must be distinct emails).
- If eTLD+1 library is unavailable, use a well-known public suffix list; do not fall back to last-2-label.

## Mandatory Unit Tests (SPEC-300 §7)
- Same email differing by case → one Person
- Subdomain stripped correctly via eTLD+1
- Different TLDs = different accounts
- Internal domains excluded from influence and warm
- W1: >= 2 distinct emails same domain triggers warm
- W1: duplicate records of same email do not trigger
- W2: same email across >= 2 sessions triggers warm
- W2: registration-only does not trigger
- W2 > W1 precedence
- Stable lexicographic sort of warm lists
- Duplicate sync does not change metrics
- Repeated sync upsert updates metrics correctly

## Completion Checks
- All normalization uses eTLD+1 (no last-2-label heuristic).
- Internal domain filter applied before influence/warm computation.
- All SPEC-300 §7 unit tests pass.
- Metrics recompute is atomic with normalized data writes.
