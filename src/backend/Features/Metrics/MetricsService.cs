using EdgeFront.Builder.Features.Metrics.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Features.Metrics;

public class MetricsService
{
    private readonly AppDbContext _db;

    public MetricsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SeriesMetricsResponseDto?> GetSeriesMetricsAsync(Guid seriesId, string ownerUserId)
    {
        // Verify ownership via the Series table
        var seriesExists = await _db.Series
            .AnyAsync(s => s.SeriesId == seriesId && s.OwnerUserId == ownerUserId);
        if (!seriesExists) return null;

        var m = await _db.SeriesMetrics
            .FirstOrDefaultAsync(sm => sm.SeriesId == seriesId);
        if (m is null) return null;

        var warmAccounts = m.WarmAccounts
            .Select(w => new WarmAccountEntryDto(w.AccountDomain, w.WarmRule.ToString()))
            .ToList();

        return new SeriesMetricsResponseDto(
            m.SeriesId,
            m.TotalRegistrations,
            m.TotalAttendees,
            m.TotalQaQuestions,
            m.AnsweredQaQuestions,
            m.UniqueRegistrantAccountDomains,
            m.UniqueAccountsInfluenced,
            warmAccounts);
    }

    public async Task<SessionMetricsResponseDto?> GetSessionMetricsAsync(Guid sessionId, string ownerUserId)
    {
        // Verify ownership via the Session table
        var sessionExists = await _db.Sessions
            .AnyAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (!sessionExists) return null;

        var m = await _db.SessionMetrics
            .FirstOrDefaultAsync(sm => sm.SessionId == sessionId);
        if (m is null) return null;

        return new SessionMetricsResponseDto(
            m.SessionId,
            m.TotalRegistrations,
            m.TotalAttendees,
            m.TotalQaQuestions,
            m.AnsweredQaQuestions,
            m.UniqueRegistrantAccountDomains,
            m.UniqueAttendeeAccountDomains,
            m.WarmAccountsTriggered);
    }
}
