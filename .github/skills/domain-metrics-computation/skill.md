---
name: domain-metrics-and-normalization
description: 'Apply email and domain normalization plus session and series metric rules, including W1/W2 warm-account logic and internal-domain exclusion. Use for domain logic changes, recompute behavior, and mandatory unit-test coverage.'
argument-hint: 'Describe the normalization rule, metric behavior, affected entities or sessions, and whether the task is implementation or test coverage.'
---

# Domain and Metrics Computation

Canonical owner of email/domain normalization and session/series metrics.

## When to Use
- Normalization logic
- SessionMetrics / SeriesMetrics computation
- W1/W2 warm-account rules
- Influence (uniqueAccountsInfluenced) counting
- Internal-domain exclusion
- Unit tests for any of the above

## Quick Checklist
1. Normalize: trim+lowercase email, eTLD+1 domain.
2. Filter internal domains before influence/warm.
3. Follow formulas below exactly.
4. Warm precedence W2 > W1; one entry per domain.

## Normalization
- **Email**: trim + lowercase. Identity key = normalized email.
- **Domain**: public-suffix-aware eTLD+1.
  - `kt.kpmg.com` → `kpmg.com`
  - `example.co.uk` → `example.co.uk` (not `co.uk`)
  - `kpmg.com` ≠ `kpmg.au` (different TLDs = different accounts)
- Internal domain list: env/config, validated at startup.
- Apply before persistence — store only normalized values.

## SessionMetrics
External-only = excludes internal domains.

| Metric | Formula |
|--------|---------|
| `totalRegistrations` | COUNT(NormalizedRegistration), all domains |
| `totalAttendees` | COUNT(NormalizedAttendance), all domains |
| `uniqueRegistrantAccountDomains` | COUNT(DISTINCT emailDomain) from registrations, external only |
| `uniqueAttendeeAccountDomains` | COUNT(DISTINCT emailDomain) from attendance, external only |
| `warmAccountsTriggered` (W1) | external emailDomains in attendance where COUNT(DISTINCT email) ≥ 2; lex-sorted |

## SeriesMetrics

| Metric | Formula |
|--------|---------|
| `totalRegistrations` | SUM(SessionMetrics.totalRegistrations) |
| `totalAttendees` | SUM(SessionMetrics.totalAttendees) |
| `uniqueRegistrantAccountDomains` | COUNT(DISTINCT emailDomain) across all sessions, external only |
| `uniqueAccountsInfluenced` | COUNT(DISTINCT emailDomain) across attendance in all sessions, external only |
| `warmAccounts` | W1: propagate from sessions. W2: any email in external domain attending ≥ 2 distinct sessions in series. **W2 > W1; one entry per domain; lex-sorted by accountDomain.** |

## Transaction Boundaries
- Normalized upserts + metrics recompute atomic (single DB transaction).
- Concurrency: DB-level strategy prevents same-session races.
- Any failure → rollback entire transaction; caller handles retry.

## Decision Points
- Registration-only records do NOT count toward influence or W2.
- Attendance with any duration (incl. 0) counts as attended.
- W1 requires distinct emails; duplicates of same email do NOT trigger.
- eTLD+1 library unavailable → use public suffix list; do not fall back to last-2-label.

## Mandatory Unit Tests
- Email case-insensitive → one Person
- Subdomain stripped via eTLD+1
- Different TLDs = different accounts
- Internal domains excluded from influence/warm
- W1: ≥ 2 distinct emails same domain triggers
- W1: duplicate records same email does NOT trigger
- W2: same email across ≥ 2 sessions triggers
- W2: registration-only does NOT trigger
- W2 > W1 precedence
- Stable lex-sort of warm lists
- Duplicate sync: metrics unchanged
- Repeated sync: metrics update correctly

## Completion Checks
- All normalization uses eTLD+1 (no last-2-label)
- Internal filter applied before influence/warm
- All mandatory tests pass
- Metrics recompute atomic with normalized writes
