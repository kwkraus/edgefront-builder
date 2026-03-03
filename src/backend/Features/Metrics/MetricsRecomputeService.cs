using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Features.Metrics;

/// <summary>
/// Atomically recomputes and upserts <see cref="SessionMetrics"/> and <see cref="SeriesMetrics"/>
/// from the normalised registration / attendance rows stored in the database.
///
/// SPEC-300 §7 (Session metrics) and §8 (Series metrics).
/// </summary>
public class MetricsRecomputeService
{
    private readonly AppDbContext _db;
    private readonly InternalDomainFilter _internalDomainFilter;
    private readonly WarmRuleEvaluator _warmRuleEvaluator;

    public MetricsRecomputeService(
        AppDbContext db,
        InternalDomainFilter internalDomainFilter,
        WarmRuleEvaluator warmRuleEvaluator)
    {
        _db = db;
        _internalDomainFilter = internalDomainFilter;
        _warmRuleEvaluator = warmRuleEvaluator;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Session metrics (SPEC-300 §7)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Atomically computes and upserts <see cref="SessionMetrics"/> for the given session.
    /// All reads and the upsert happen inside a single EF transaction.
    /// </summary>
    public async Task RecomputeSessionMetricsAsync(Guid sessionId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var registrations = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == sessionId)
            .ToListAsync(ct);

        var attendances = await _db.NormalizedAttendances
            .Where(a => a.SessionId == sessionId)
            .ToListAsync(ct);

        // totalRegistrations / totalAttendees include internal domains
        var totalRegistrations = registrations.Count;
        var totalAttendees = attendances.Count;

        // unique domains exclude internal
        var uniqueRegistrantDomains = registrations
            .Where(r => !_internalDomainFilter.IsInternal(r.EmailDomain))
            .Select(r => r.EmailDomain.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var uniqueAttendeeDomains = attendances
            .Where(a => !_internalDomainFilter.IsInternal(a.EmailDomain))
            .Select(a => a.EmailDomain.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        // W1: ≥2 distinct emails from the same domain in this session (external only)
        var warmDomainsW1 = _warmRuleEvaluator.EvaluateW1(attendances).ToList();

        // Upsert
        var existing = await _db.SessionMetrics.FindAsync([sessionId], ct);
        if (existing is null)
        {
            _db.SessionMetrics.Add(new SessionMetrics
            {
                SessionId = sessionId,
                TotalRegistrations = totalRegistrations,
                TotalAttendees = totalAttendees,
                UniqueRegistrantAccountDomains = uniqueRegistrantDomains,
                UniqueAttendeeAccountDomains = uniqueAttendeeDomains,
                WarmAccountsTriggered = warmDomainsW1
            });
        }
        else
        {
            existing.TotalRegistrations = totalRegistrations;
            existing.TotalAttendees = totalAttendees;
            existing.UniqueRegistrantAccountDomains = uniqueRegistrantDomains;
            existing.UniqueAttendeeAccountDomains = uniqueAttendeeDomains;
            existing.WarmAccountsTriggered = warmDomainsW1;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Series metrics (SPEC-300 §8)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Atomically computes and upserts <see cref="SeriesMetrics"/> for the given series.
    /// </summary>
    public async Task RecomputeSeriesMetricsAsync(Guid seriesId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Collect all sessions for this series
        var sessionIds = await _db.Sessions
            .Where(s => s.SeriesId == seriesId)
            .Select(s => s.SessionId)
            .ToListAsync(ct);

        var registrations = await _db.NormalizedRegistrations
            .Where(r => sessionIds.Contains(r.SessionId))
            .ToListAsync(ct);

        var attendances = await _db.NormalizedAttendances
            .Where(a => sessionIds.Contains(a.SessionId))
            .ToListAsync(ct);

        // Include internal for raw counts
        var totalRegistrations = registrations.Count;
        var totalAttendees = attendances.Count;

        // Unique registrant domains (external only)
        var uniqueRegistrantDomains = registrations
            .Where(r => !_internalDomainFilter.IsInternal(r.EmailDomain))
            .Select(r => r.EmailDomain.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        // uniqueAccountsInfluenced = distinct external domains that attended (not registration-only)
        var uniqueAccountsInfluenced = attendances
            .Where(a => !_internalDomainFilter.IsInternal(a.EmailDomain))
            .Select(a => a.EmailDomain.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        // Warm accounts: W2 > W1 precedence per domain, external only
        // W1 requires ≥2 distinct emails from the same domain within a *single* session.
        // Evaluate per-session, then union triggered domains across all sessions.
        var w1Domains = attendances
            .GroupBy(a => a.SessionId)
            .SelectMany(g => _warmRuleEvaluator.EvaluateW1(g))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // W2 at series level
        var w2Domains = _warmRuleEvaluator.EvaluateW2(attendances)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // All warm domains (union)
        var allWarmDomains = w1Domains.Union(w2Domains, StringComparer.OrdinalIgnoreCase);

        var warmAccounts = allWarmDomains
            .Select(domain => new WarmAccountEntry
            {
                AccountDomain = domain,
                // W2 takes precedence when a domain qualifies for both
                WarmRule = w2Domains.Contains(domain) ? WarmRule.W2 : WarmRule.W1
            })
            .ToList();

        // Upsert
        var existing = await _db.SeriesMetrics.FindAsync([seriesId], ct);
        if (existing is null)
        {
            _db.SeriesMetrics.Add(new SeriesMetrics
            {
                SeriesId = seriesId,
                TotalRegistrations = totalRegistrations,
                TotalAttendees = totalAttendees,
                UniqueRegistrantAccountDomains = uniqueRegistrantDomains,
                UniqueAccountsInfluenced = uniqueAccountsInfluenced,
                WarmAccounts = warmAccounts
            });
        }
        else
        {
            existing.TotalRegistrations = totalRegistrations;
            existing.TotalAttendees = totalAttendees;
            existing.UniqueRegistrantAccountDomains = uniqueRegistrantDomains;
            existing.UniqueAccountsInfluenced = uniqueAccountsInfluenced;
            existing.WarmAccounts = warmAccounts;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
