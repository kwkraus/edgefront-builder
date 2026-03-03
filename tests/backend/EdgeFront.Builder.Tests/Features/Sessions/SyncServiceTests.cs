using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EdgeFront.Builder.Tests.Features.Sessions;

/// <summary>
/// Tests for SyncService — user-initiated sync of registrations and attendance
/// from Graph using delegated (OBO) tokens.
/// </summary>
public class SyncServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ITeamsGraphClient> _graphMock;
    private readonly SyncService _sut;
    private const string OwnerUserId = "sync-user-oid";
    private const string OboToken = "obo-sync-token";

    public SyncServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        _graphMock = new Mock<ITeamsGraphClient>();

        var filter = new InternalDomainFilter(Array.Empty<string>());
        var warmRuleEvaluator = new WarmRuleEvaluator(filter);
        var metricsRecompute = new MetricsRecomputeService(_db, filter, warmRuleEvaluator);

        _sut = new SyncService(
            _db, _graphMock.Object, metricsRecompute,
            NullLogger<SyncService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SyncSessionAsync_ReturnsNotFound_WhenSessionMissing()
    {
        var result = await _sut.SyncSessionAsync(Guid.NewGuid(), OwnerUserId, OboToken);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("session_not_found");
    }

    [Fact]
    public async Task SyncSessionAsync_ReturnsNotPublished_WhenDraft()
    {
        var (_, session) = await SeedPublishedSessionAsync(published: false);
        var result = await _sut.SyncSessionAsync(session.SessionId, OwnerUserId, OboToken);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("session_not_published");
    }

    [Fact]
    public async Task SyncSessionAsync_InsertsRegistrations_AndAttendance()
    {
        var (_, session) = await SeedPublishedSessionAsync();

        _graphMock.Setup(g => g.GetRegistrationsAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(new[]
            {
                new RegistrationRecord("alice@example.com", DateTimeOffset.UtcNow),
                new RegistrationRecord("bob@example.com", DateTimeOffset.UtcNow),
            });

        _graphMock.Setup(g => g.GetAttendanceAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(new[]
            {
                new AttendanceRecord("alice@example.com", true, 3600, null, null, null),
            });

        var result = await _sut.SyncSessionAsync(session.SessionId, OwnerUserId, OboToken);

        result.Success.Should().BeTrue();

        var regs = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId).ToListAsync();
        regs.Should().HaveCount(2);

        var atts = await _db.NormalizedAttendances
            .Where(a => a.SessionId == session.SessionId).ToListAsync();
        atts.Should().HaveCount(1);

        // Session status updated
        var dbSession = await _db.Sessions.FindAsync(session.SessionId);
        dbSession!.ReconcileStatus.Should().Be(ReconcileStatus.Synced);
        dbSession.LastSyncAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncSessionAsync_RemovesStaleRegistrations()
    {
        var (_, session) = await SeedPublishedSessionAsync();

        // Existing registration that is NOT in the Graph response
        _db.NormalizedRegistrations.Add(new NormalizedRegistration
        {
            RegistrationId = Guid.NewGuid(),
            SessionId = session.SessionId,
            OwnerUserId = OwnerUserId,
            Email = "stale@old.com",
            EmailDomain = "old.com",
            RegisteredAt = DateTime.UtcNow.AddDays(-5)
        });
        await _db.SaveChangesAsync();

        _graphMock.Setup(g => g.GetRegistrationsAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(new[]
            {
                new RegistrationRecord("fresh@new.com", DateTimeOffset.UtcNow),
            });
        _graphMock.Setup(g => g.GetAttendanceAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(Array.Empty<AttendanceRecord>());

        await _sut.SyncSessionAsync(session.SessionId, OwnerUserId, OboToken);

        var regs = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId).ToListAsync();
        regs.Should().HaveCount(1);
        regs[0].Email.Should().Be("fresh@new.com");
    }

    [Fact]
    public async Task SyncSessionAsync_IsIdempotent_UpsertsDuplicateEmails()
    {
        var (_, session) = await SeedPublishedSessionAsync();

        var registrations = new[]
        {
            new RegistrationRecord("alice@example.com", DateTimeOffset.UtcNow),
        };
        _graphMock.Setup(g => g.GetRegistrationsAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(registrations);
        _graphMock.Setup(g => g.GetAttendanceAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(Array.Empty<AttendanceRecord>());

        // Sync twice
        await _sut.SyncSessionAsync(session.SessionId, OwnerUserId, OboToken);
        await _sut.SyncSessionAsync(session.SessionId, OwnerUserId, OboToken);

        var regs = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId).ToListAsync();
        regs.Should().HaveCount(1);
    }

    [Fact]
    public async Task SyncSessionAsync_PassesOboToken_ToGraphClient()
    {
        var (_, session) = await SeedPublishedSessionAsync();

        _graphMock.Setup(g => g.GetRegistrationsAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(Array.Empty<RegistrationRecord>());
        _graphMock.Setup(g => g.GetAttendanceAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(Array.Empty<AttendanceRecord>());

        await _sut.SyncSessionAsync(session.SessionId, OwnerUserId, OboToken);

        _graphMock.Verify(g => g.GetRegistrationsAsync(session.TeamsWebinarId!, OboToken, default), Times.Once);
        _graphMock.Verify(g => g.GetAttendanceAsync(session.TeamsWebinarId!, OboToken, default), Times.Once);
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session)> SeedPublishedSessionAsync(bool published = true)
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Sync Test Series",
            Status = published ? SeriesStatus.Published : SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Series.Add(series);

        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = series.SeriesId,
            OwnerUserId = OwnerUserId,
            Title = "Sync Session",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = published ? SessionStatus.Published : SessionStatus.Draft,
            TeamsWebinarId = published ? "webinar-sync-test" : null,
            ReconcileStatus = ReconcileStatus.Synced
        };
        _db.Sessions.Add(session);

        await _db.SaveChangesAsync();
        return (series, session);
    }
}
