using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EdgeFront.Builder.Tests.Features.Metrics;

/// <summary>
/// SPEC-300 §7 and §8 — Metrics recompute service tests.
/// </summary>
public class MetricsRecomputeTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly MetricsRecomputeService _sut;
    private readonly InternalDomainFilter _filter;
    private const string OwnerUserId = "metrics-user";

    // "acme.com" is the internal domain for all tests
    private static readonly string[] InternalDomains = ["acme.com"];

    public MetricsRecomputeTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        _filter = new InternalDomainFilter(InternalDomains);
        var warmEval = new WarmRuleEvaluator(_filter);
        _sut = new MetricsRecomputeService(_db, _filter, warmEval);
    }

    public void Dispose() => _db.Dispose();

    // ─────────────────────────────────────────────────────────────────────────
    // Session metrics (SPEC-300 §7)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SessionMetrics_TotalCountsIncludeInternalDomains()
    {
        var (series, session) = await SeedSeriesAndSessionAsync();

        // 2 external + 1 internal registration
        await SeedRegistrationsAsync(session.SessionId,
            "ext1@external.com",
            "ext2@other.com",
            "int@acme.com");

        // 2 external + 1 internal attendance
        await SeedAttendancesAsync(session.SessionId,
            "ext1@external.com",
            "int@acme.com",
            "ext3@third.com");

        await _sut.RecomputeSessionMetricsAsync(session.SessionId);

        var m = await _db.SessionMetrics.FindAsync(session.SessionId);
        m!.TotalRegistrations.Should().Be(3);   // includes internal
        m.TotalAttendees.Should().Be(3);          // includes internal
    }

    [Fact]
    public async Task SessionMetrics_UniqueDomainsExcludeInternalDomains()
    {
        var (_, session) = await SeedSeriesAndSessionAsync();

        await SeedRegistrationsAsync(session.SessionId,
            "a@external.com",
            "b@other.com",
            "c@acme.com");   // internal — excluded

        await SeedAttendancesAsync(session.SessionId,
            "a@external.com",
            "d@acme.com",   // internal — excluded
            "e@third.com");

        await _sut.RecomputeSessionMetricsAsync(session.SessionId);

        var m = await _db.SessionMetrics.FindAsync(session.SessionId);
        m!.UniqueRegistrantAccountDomains.Should().Be(2); // external.com, other.com
        m.UniqueAttendeeAccountDomains.Should().Be(2);     // external.com, third.com
    }

    [Fact]
    public async Task SessionMetrics_W1_TriggeredWhenTwoDistinctEmailsSameDomain()
    {
        var (_, session) = await SeedSeriesAndSessionAsync();

        // Two distinct emails at contoso.com → W1 triggers
        await SeedAttendancesAsync(session.SessionId,
            "alice@contoso.com",
            "bob@contoso.com",
            "solo@other.com");    // only 1 email from other.com → no W1

        await _sut.RecomputeSessionMetricsAsync(session.SessionId);

        var m = await _db.SessionMetrics.FindAsync(session.SessionId);
        m!.WarmAccountsTriggered.Should().ContainSingle()
            .Which.Should().Be("contoso.com");
    }

    [Fact]
    public async Task SessionMetrics_W1_NotTriggeredForSingleEmail()
    {
        var (_, session) = await SeedSeriesAndSessionAsync();

        // Only 1 email per domain — W1 should not trigger
        await SeedAttendancesAsync(session.SessionId,
            "alice@contoso.com",
            "bob@fabrikam.com");

        await _sut.RecomputeSessionMetricsAsync(session.SessionId);

        var m = await _db.SessionMetrics.FindAsync(session.SessionId);
        m!.WarmAccountsTriggered.Should().BeEmpty();
    }

    [Fact]
    public async Task SessionMetrics_W1_ExcludesInternalDomains()
    {
        var (_, session) = await SeedSeriesAndSessionAsync();

        // Two emails at internal domain — W1 should NOT trigger
        await SeedAttendancesAsync(session.SessionId,
            "alice@acme.com",
            "bob@acme.com");

        await _sut.RecomputeSessionMetricsAsync(session.SessionId);

        var m = await _db.SessionMetrics.FindAsync(session.SessionId);
        m!.WarmAccountsTriggered.Should().BeEmpty();
    }

    [Fact]
    public async Task SessionMetrics_EmptySession_ReturnsAllZeros()
    {
        var (_, session) = await SeedSeriesAndSessionAsync();

        await _sut.RecomputeSessionMetricsAsync(session.SessionId);

        var m = await _db.SessionMetrics.FindAsync(session.SessionId);
        m!.TotalRegistrations.Should().Be(0);
        m.TotalAttendees.Should().Be(0);
        m.TotalQaQuestions.Should().Be(0);
        m.AnsweredQaQuestions.Should().Be(0);
        m.UniqueRegistrantAccountDomains.Should().Be(0);
        m.UniqueAttendeeAccountDomains.Should().Be(0);
        m.WarmAccountsTriggered.Should().BeEmpty();
    }

    [Fact]
    public async Task SessionMetrics_QaCounts_AreComputedFromImportedQaRows()
    {
        var (_, session) = await SeedSeriesAndSessionAsync();

        _db.NormalizedQaEntries.AddRange(
            new NormalizedQaEntry
            {
                QaEntryId = Guid.NewGuid(),
                SessionId = session.SessionId,
                OwnerUserId = OwnerUserId,
                QuestionText = "What is next?",
                IsAnswered = true,
                AskedAt = DateTime.UtcNow
            },
            new NormalizedQaEntry
            {
                QaEntryId = Guid.NewGuid(),
                SessionId = session.SessionId,
                OwnerUserId = OwnerUserId,
                QuestionText = "Will slides be shared?",
                IsAnswered = false,
                AskedAt = DateTime.UtcNow
            });
        await _db.SaveChangesAsync();

        await _sut.RecomputeSessionMetricsAsync(session.SessionId);

        var m = await _db.SessionMetrics.FindAsync(session.SessionId);
        m!.TotalQaQuestions.Should().Be(2);
        m.AnsweredQaQuestions.Should().Be(1);
    }

    [Fact]
    public async Task SessionMetrics_Idempotent_SameResultOnDoubleRecompute()
    {
        var (_, session) = await SeedSeriesAndSessionAsync();

        await SeedAttendancesAsync(session.SessionId,
            "alice@contoso.com",
            "bob@contoso.com");

        // First compute
        await _sut.RecomputeSessionMetricsAsync(session.SessionId);
        var m1 = await _db.SessionMetrics.FindAsync(session.SessionId);
        var warm1 = m1!.WarmAccountsTriggered.ToList();

        // Second compute
        await _sut.RecomputeSessionMetricsAsync(session.SessionId);
        var m2 = await _db.SessionMetrics.FindAsync(session.SessionId);

        m2!.TotalAttendees.Should().Be(m1.TotalAttendees);
        m2.WarmAccountsTriggered.Should().BeEquivalentTo(warm1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Series metrics (SPEC-300 §8)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SeriesMetrics_UniqueAccountsInfluenced_CountsAttendanceOnlyDomains()
    {
        var (series, session1) = await SeedSeriesAndSessionAsync();
        var session2 = await SeedExtraSessionAsync(series.SeriesId);

        // Registered but never attended — should NOT count toward influenced
        await SeedRegistrationsAsync(session1.SessionId, "reg-only@never.com");

        // Attended — should count
        await SeedAttendancesAsync(session1.SessionId, "alice@attender.com");
        await SeedAttendancesAsync(session2.SessionId, "bob@attender2.com");

        await _sut.RecomputeSeriesMetricsAsync(series.SeriesId);

        var m = await _db.SeriesMetrics.FindAsync(series.SeriesId);
        // attender.com + attender2.com = 2 unique influenced domains
        m!.UniqueAccountsInfluenced.Should().Be(2);
    }

    [Fact]
    public async Task SeriesMetrics_W2_TriggersForSameEmailAcrossTwoSessions()
    {
        var (series, session1) = await SeedSeriesAndSessionAsync();
        var session2 = await SeedExtraSessionAsync(series.SeriesId);

        // alice attends both sessions → W2 for contoso.com
        await SeedAttendancesAsync(session1.SessionId, "alice@contoso.com");
        await SeedAttendancesAsync(session2.SessionId, "alice@contoso.com");

        await _sut.RecomputeSeriesMetricsAsync(series.SeriesId);

        var m = await _db.SeriesMetrics.FindAsync(series.SeriesId);
        var w2 = m!.WarmAccounts.Where(w => w.WarmRule == WarmRule.W2).ToList();
        w2.Should().ContainSingle(w => w.AccountDomain == "contoso.com");
    }

    [Fact]
    public async Task SeriesMetrics_W2_TakesPrecedenceOverW1_SameDomain()
    {
        // Domain qualifies for both W1 (2 emails, 1 session) and W2 (same email, 2 sessions)
        var (series, session1) = await SeedSeriesAndSessionAsync();
        var session2 = await SeedExtraSessionAsync(series.SeriesId);

        // Two distinct emails in session1 → W1 for contoso.com
        await SeedAttendancesAsync(session1.SessionId,
            "alice@contoso.com",
            "bob@contoso.com");

        // alice also attends session2 → W2 for contoso.com
        await SeedAttendancesAsync(session2.SessionId, "alice@contoso.com");

        await _sut.RecomputeSeriesMetricsAsync(series.SeriesId);

        var m = await _db.SeriesMetrics.FindAsync(series.SeriesId);
        var entry = m!.WarmAccounts.Single(w => w.AccountDomain == "contoso.com");

        // W2 must take precedence over W1
        entry.WarmRule.Should().Be(WarmRule.W2);
    }

    [Fact]
    public async Task SeriesMetrics_InternalDomainsExcluded_FromInfluenceAndWarm()
    {
        var (series, session1) = await SeedSeriesAndSessionAsync();

        // Internal attendees — should not count toward influenced or warm
        await SeedAttendancesAsync(session1.SessionId,
            "alice@acme.com",
            "bob@acme.com");

        await _sut.RecomputeSeriesMetricsAsync(series.SeriesId);

        var m = await _db.SeriesMetrics.FindAsync(series.SeriesId);
        m!.UniqueAccountsInfluenced.Should().Be(0);
        m.WarmAccounts.Should().BeEmpty();
    }

    [Fact]
    public async Task SeriesMetrics_W1_EvaluatedPerSession_OneEmailPerSessionDoesNotTrigger()
    {
        // Regression: two distinct emails from the same domain, but each in a DIFFERENT
        // session, must NOT trigger W1 (W1 requires ≥2 distinct emails within ONE session).
        var (series, session1) = await SeedSeriesAndSessionAsync();
        var session2 = await SeedExtraSessionAsync(series.SeriesId);

        // alice in session1, bob in session2 — different sessions, same domain
        await SeedAttendancesAsync(session1.SessionId, "alice@contoso.com");
        await SeedAttendancesAsync(session2.SessionId, "bob@contoso.com");

        await _sut.RecomputeSeriesMetricsAsync(series.SeriesId);

        var m = await _db.SeriesMetrics.FindAsync(series.SeriesId);
        // contoso.com should NOT appear as W1 (only one email per session)
        m!.WarmAccounts.Should().NotContain(w => w.AccountDomain == "contoso.com" && w.WarmRule == WarmRule.W1);
    }

    [Fact]
    public async Task SeriesMetrics_W1_TriggersOnlyWhenTwoEmailsSameSession()
    {
        // Confirm W1 IS triggered when two distinct emails share a domain within one session,
        // even across a multi-session series.
        var (series, session1) = await SeedSeriesAndSessionAsync();
        var session2 = await SeedExtraSessionAsync(series.SeriesId);

        // Two emails in session1 → W1 for contoso.com
        await SeedAttendancesAsync(session1.SessionId,
            "alice@contoso.com",
            "bob@contoso.com");

        // session2 has a single email from a different domain → no W1 for fabrikam.com
        await SeedAttendancesAsync(session2.SessionId, "carol@fabrikam.com");

        await _sut.RecomputeSeriesMetricsAsync(series.SeriesId);

        var m = await _db.SeriesMetrics.FindAsync(series.SeriesId);
        m!.WarmAccounts.Should().ContainSingle(w => w.AccountDomain == "contoso.com" && w.WarmRule == WarmRule.W1);
        m.WarmAccounts.Should().NotContain(w => w.AccountDomain == "fabrikam.com");
    }

    [Fact]
    public async Task SeriesMetrics_TotalCountsIncludeInternal()
    {
        var (series, session1) = await SeedSeriesAndSessionAsync();

        await SeedRegistrationsAsync(session1.SessionId,
            "ext@external.com",
            "int@acme.com");

        await SeedAttendancesAsync(session1.SessionId,
            "ext@external.com",
            "int@acme.com");

        await _sut.RecomputeSeriesMetricsAsync(series.SeriesId);

        var m = await _db.SeriesMetrics.FindAsync(series.SeriesId);
        m!.TotalRegistrations.Should().Be(2); // includes internal
        m.TotalAttendees.Should().Be(2);       // includes internal
    }

    [Fact]
    public async Task SeriesMetrics_QaCounts_AggregateAcrossSessions()
    {
        var (series, session1) = await SeedSeriesAndSessionAsync();
        var session2 = await SeedExtraSessionAsync(series.SeriesId);

        _db.NormalizedQaEntries.AddRange(
            new NormalizedQaEntry
            {
                QaEntryId = Guid.NewGuid(),
                SessionId = session1.SessionId,
                OwnerUserId = OwnerUserId,
                QuestionText = "Q1",
                IsAnswered = true,
                AskedAt = DateTime.UtcNow
            },
            new NormalizedQaEntry
            {
                QaEntryId = Guid.NewGuid(),
                SessionId = session2.SessionId,
                OwnerUserId = OwnerUserId,
                QuestionText = "Q2",
                IsAnswered = false,
                AskedAt = DateTime.UtcNow
            },
            new NormalizedQaEntry
            {
                QaEntryId = Guid.NewGuid(),
                SessionId = session2.SessionId,
                OwnerUserId = OwnerUserId,
                QuestionText = "Q3",
                IsAnswered = true,
                AskedAt = DateTime.UtcNow
            });
        await _db.SaveChangesAsync();

        await _sut.RecomputeSeriesMetricsAsync(series.SeriesId);

        var m = await _db.SeriesMetrics.FindAsync(series.SeriesId);
        m!.TotalQaQuestions.Should().Be(3);
        m.AnsweredQaQuestions.Should().Be(2);
    }

    [Fact]
    public async Task SeriesMetrics_Idempotent_SameResultOnDoubleRecompute()
    {
        var (series, session1) = await SeedSeriesAndSessionAsync();
        var session2 = await SeedExtraSessionAsync(series.SeriesId);

        await SeedAttendancesAsync(session1.SessionId, "alice@contoso.com");
        await SeedAttendancesAsync(session2.SessionId, "alice@contoso.com");

        await _sut.RecomputeSeriesMetricsAsync(series.SeriesId);
        var m1 = await _db.SeriesMetrics.FindAsync(series.SeriesId);
        var warm1 = m1!.WarmAccounts.ToList();

        await _sut.RecomputeSeriesMetricsAsync(series.SeriesId);
        var m2 = await _db.SeriesMetrics.FindAsync(series.SeriesId);

        m2!.UniqueAccountsInfluenced.Should().Be(m1.UniqueAccountsInfluenced);
        m2.WarmAccounts.Should().BeEquivalentTo(warm1,
            o => o.ComparingByMembers<WarmAccountEntry>());
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session)> SeedSeriesAndSessionAsync()
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Metrics Test Series",
            Status = SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = series.SeriesId,
            OwnerUserId = OwnerUserId,
            Title = "Session 1",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1)
        };
        _db.Series.Add(series);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return (series, session);
    }

    private async Task<Session> SeedExtraSessionAsync(Guid seriesId)
    {
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = seriesId,
            OwnerUserId = OwnerUserId,
            Title = "Session 2",
            StartsAt = DateTime.UtcNow.AddDays(2),
            EndsAt = DateTime.UtcNow.AddDays(2).AddHours(1)
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    private async Task SeedRegistrationsAsync(Guid sessionId, params string[] emails)
    {
        foreach (var email in emails)
        {
            _db.NormalizedRegistrations.Add(new NormalizedRegistration
            {
                RegistrationId = Guid.NewGuid(),
                SessionId = sessionId,
                OwnerUserId = OwnerUserId,
                Email = email.ToLowerInvariant(),
                EmailDomain = email.Split('@')[1].ToLowerInvariant(),
                RegisteredAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedAttendancesAsync(Guid sessionId, params string[] emails)
    {
        foreach (var email in emails)
        {
            _db.NormalizedAttendances.Add(new NormalizedAttendance
            {
                AttendanceId = Guid.NewGuid(),
                SessionId = sessionId,
                OwnerUserId = OwnerUserId,
                Email = email.ToLowerInvariant(),
                EmailDomain = email.Split('@')[1].ToLowerInvariant(),
                Attended = true
            });
        }
        await _db.SaveChangesAsync();
    }
}
