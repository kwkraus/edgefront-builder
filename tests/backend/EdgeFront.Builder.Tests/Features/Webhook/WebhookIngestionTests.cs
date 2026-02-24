using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Features.Webhook;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EdgeFront.Builder.Tests.Features.Webhook;

/// <summary>
/// SPEC-200 §3 — Webhook ingestion tests.
/// </summary>
public class WebhookIngestionTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ITeamsGraphClient> _graphMock;
    private readonly MetricsRecomputeService _metricsRecompute;
    private readonly WebhookIngestionService _sut;
    private const string OwnerUserId = "ingestion-user";
    private const string TeamsWebinarId = "teams-webinar-ingestion-123";

    public WebhookIngestionTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);

        _graphMock = new Mock<ITeamsGraphClient>();

        var filter = new InternalDomainFilter(new[] { "internal.com" });
        var warmEval = new WarmRuleEvaluator(filter);
        _metricsRecompute = new MetricsRecomputeService(_db, filter, warmEval);

        _sut = new WebhookIngestionService(
            _db,
            _graphMock.Object,
            filter,
            _metricsRecompute,
            NullLogger<WebhookIngestionService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ─── Registration idempotency ────────────────────────────────────────────

    [Fact]
    public async Task HandleRegistration_UpsertsIdempotently_SecondCallProducesNoExtraRows()
    {
        var (series, session) = await SeedSeriesAndSessionAsync(TeamsWebinarId);

        var registrations = new[]
        {
            new RegistrationRecord("alice@external.com", DateTimeOffset.UtcNow),
            new RegistrationRecord("bob@external.com", DateTimeOffset.UtcNow)
        };

        _graphMock
            .Setup(g => g.GetRegistrationsAsync(TeamsWebinarId, default))
            .ReturnsAsync(registrations);

        // First call
        await _sut.HandleRegistrationAsync(TeamsWebinarId, "corr-1");

        var countAfterFirst = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId).CountAsync();
        countAfterFirst.Should().Be(2);

        // Second call (same data — idempotent)
        await _sut.HandleRegistrationAsync(TeamsWebinarId, "corr-2");

        var countAfterSecond = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId).CountAsync();
        countAfterSecond.Should().Be(2, "second call with same data must not add rows");
    }

    [Fact]
    public async Task HandleRegistration_RemovesStaleRegistrations()
    {
        var (_, session) = await SeedSeriesAndSessionAsync(TeamsWebinarId);

        // Seed a stale registration that won't be in the Graph response
        _db.NormalizedRegistrations.Add(new NormalizedRegistration
        {
            RegistrationId = Guid.NewGuid(),
            SessionId = session.SessionId,
            OwnerUserId = OwnerUserId,
            Email = "stale@ghost.com",
            EmailDomain = "ghost.com",
            RegisteredAt = DateTime.UtcNow.AddDays(-1)
        });
        await _db.SaveChangesAsync();

        // Graph now returns only the live registrant
        _graphMock
            .Setup(g => g.GetRegistrationsAsync(TeamsWebinarId, default))
            .ReturnsAsync(new[] { new RegistrationRecord("live@real.com", DateTimeOffset.UtcNow) });

        await _sut.HandleRegistrationAsync(TeamsWebinarId, "corr-del");

        var remaining = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId).ToListAsync();

        remaining.Should().ContainSingle(r => r.Email == "live@real.com");
        remaining.Should().NotContain(r => r.Email == "stale@ghost.com");
    }

    [Fact]
    public async Task HandleRegistration_NormalizesEmailToLower()
    {
        var (_, session) = await SeedSeriesAndSessionAsync(TeamsWebinarId);

        _graphMock
            .Setup(g => g.GetRegistrationsAsync(TeamsWebinarId, default))
            .ReturnsAsync(new[] { new RegistrationRecord("Alice@CONTOSO.COM", DateTimeOffset.UtcNow) });

        await _sut.HandleRegistrationAsync(TeamsWebinarId, "corr-norm");

        var reg = await _db.NormalizedRegistrations
            .SingleAsync(r => r.SessionId == session.SessionId);
        reg.Email.Should().Be("alice@contoso.com");
        reg.EmailDomain.Should().Be("contoso.com");
    }

    // ─── Attendance: subscriptions cleared on completion ────────────────────

    [Fact]
    public async Task HandleAttendance_ClearsSubscriptions_AfterReconciliation()
    {
        var (_, session) = await SeedSeriesAndSessionAsync(TeamsWebinarId);

        // Seed two subscriptions for this session
        _db.GraphSubscriptions.AddRange(
            BuildSubscription(session.SessionId, "sub-1"),
            BuildSubscription(session.SessionId, "sub-2"));
        await _db.SaveChangesAsync();

        _graphMock
            .Setup(g => g.GetAttendanceAsync(TeamsWebinarId, default))
            .ReturnsAsync(new[]
            {
                new AttendanceRecord("alice@external.com", true, 3600, null, null, null)
            });

        await _sut.HandleAttendanceReportAsync(TeamsWebinarId, "corr-att");

        var remainingSubs = await _db.GraphSubscriptions
            .Where(s => s.SessionId == session.SessionId).ToListAsync();
        remainingSubs.Should().BeEmpty("subscriptions must be cleared after attendance reconciliation");
    }

    [Fact]
    public async Task HandleAttendance_UpsertsIdempotently()
    {
        var (_, session) = await SeedSeriesAndSessionAsync(TeamsWebinarId);

        var attendances = new[]
        {
            new AttendanceRecord("alice@external.com", true, 3600, null, null, null)
        };

        _graphMock
            .Setup(g => g.GetAttendanceAsync(TeamsWebinarId, default))
            .ReturnsAsync(attendances);

        await _sut.HandleAttendanceReportAsync(TeamsWebinarId, "corr-att-1");
        await _sut.HandleAttendanceReportAsync(TeamsWebinarId, "corr-att-2");

        var count = await _db.NormalizedAttendances
            .Where(a => a.SessionId == session.SessionId).CountAsync();
        count.Should().Be(1, "idempotent upsert must not duplicate rows");
    }

    // ─── Unknown TeamsWebinarId handled gracefully ───────────────────────────

    [Fact]
    public async Task HandleRegistration_UnknownWebinarId_DoesNotThrow()
    {
        // Arrange — no session with this webinar ID
        await _sut.Invoking(s => s.HandleRegistrationAsync("unknown-webinar-id", "corr-x"))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAttendance_UnknownWebinarId_DoesNotThrow()
    {
        await _sut.Invoking(s => s.HandleAttendanceReportAsync("unknown-webinar-id", "corr-y"))
            .Should().NotThrowAsync();
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session)> SeedSeriesAndSessionAsync(string teamsWebinarId)
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Ingestion Test Series",
            Status = SeriesStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = series.SeriesId,
            OwnerUserId = OwnerUserId,
            Title = "Ingestion Session",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = SessionStatus.Published,
            TeamsWebinarId = teamsWebinarId
        };
        _db.Series.Add(series);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return (series, session);
    }

    private static GraphSubscription BuildSubscription(Guid sessionId, string subId) => new()
    {
        GraphSubscriptionId = Guid.NewGuid(),
        SessionId = sessionId,
        OwnerUserId = OwnerUserId,
        SubscriptionId = subId,
        ChangeType = ChangeType.Registration,
        ClientStateHash = "hash",
        ExpirationDateTime = DateTime.UtcNow.AddDays(1),
        CreatedAt = DateTime.UtcNow
    };
}
