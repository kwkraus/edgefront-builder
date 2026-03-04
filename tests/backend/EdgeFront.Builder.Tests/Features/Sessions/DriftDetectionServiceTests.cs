using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace EdgeFront.Builder.Tests.Features.Sessions;

/// <summary>
/// Tests for DriftDetectionService — SPEC-200 §5 drift detection.
/// </summary>
public class DriftDetectionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ITeamsGraphClient> _graphMock;
    private readonly IMemoryCache _cache;
    private readonly DriftDetectionService _sut;
    private const string OwnerUserId = "drift-user-oid";
    private const string OboToken = "obo-drift-token";

    public DriftDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .Options;
        _db = new AppDbContext(options);
        _graphMock = new Mock<ITeamsGraphClient>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new DriftDetectionService(_db, _graphMock.Object, _cache);
    }

    public void Dispose()
    {
        _db.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public async Task CheckDrift_ReturnsNone_WhenSessionNotFound()
    {
        var result = await _sut.CheckDriftAsync(Guid.NewGuid(), OwnerUserId, OboToken);
        result.Should().Be(DriftStatus.None);
    }

    [Fact]
    public async Task CheckDrift_ReturnsNone_WhenSessionIsDraft()
    {
        var session = await SeedSessionAsync(published: false);

        var result = await _sut.CheckDriftAsync(session.SessionId, OwnerUserId, OboToken);

        result.Should().Be(DriftStatus.None);
    }

    [Fact]
    public async Task CheckDrift_ReturnsCurrentStatus_WhenNoOboToken()
    {
        var session = await SeedSessionAsync(driftStatus: DriftStatus.DriftDetected);

        var result = await _sut.CheckDriftAsync(session.SessionId, OwnerUserId, oboToken: null);

        result.Should().Be(DriftStatus.DriftDetected);
        _graphMock.Verify(
            g => g.GetWebinarMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckDrift_ReturnsNone_WhenTeamsDataMatches()
    {
        var session = await SeedSessionAsync();

        _graphMock.Setup(g => g.GetWebinarMetadataAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(new TeamsWebinarInfo(
                session.TeamsWebinarId!,
                session.Title,
                new DateTimeOffset(session.StartsAt, TimeSpan.Zero),
                new DateTimeOffset(session.EndsAt, TimeSpan.Zero),
                "https://teams.microsoft.com/l/meetup-join/test"));

        var result = await _sut.CheckDriftAsync(session.SessionId, OwnerUserId, OboToken);

        result.Should().Be(DriftStatus.None);
    }

    [Fact]
    public async Task CheckDrift_ReturnsDriftDetected_WhenTitleDiffers()
    {
        var session = await SeedSessionAsync();

        _graphMock.Setup(g => g.GetWebinarMetadataAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(new TeamsWebinarInfo(
                session.TeamsWebinarId!,
                "Different Title",
                new DateTimeOffset(session.StartsAt, TimeSpan.Zero),
                new DateTimeOffset(session.EndsAt, TimeSpan.Zero),
                "https://teams.microsoft.com/l/meetup-join/test"));

        var result = await _sut.CheckDriftAsync(session.SessionId, OwnerUserId, OboToken);

        result.Should().Be(DriftStatus.DriftDetected);

        // Verify drift was persisted to DB
        var dbSession = await _db.Sessions.FindAsync(session.SessionId);
        dbSession!.DriftStatus.Should().Be(DriftStatus.DriftDetected);
    }

    [Fact]
    public async Task CheckDrift_ReturnsDriftDetected_WhenStartsAtDiffers()
    {
        var session = await SeedSessionAsync();

        _graphMock.Setup(g => g.GetWebinarMetadataAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(new TeamsWebinarInfo(
                session.TeamsWebinarId!,
                session.Title,
                new DateTimeOffset(session.StartsAt.AddHours(1), TimeSpan.Zero),
                new DateTimeOffset(session.EndsAt, TimeSpan.Zero),
                "https://teams.microsoft.com/l/meetup-join/test"));

        var result = await _sut.CheckDriftAsync(session.SessionId, OwnerUserId, OboToken);

        result.Should().Be(DriftStatus.DriftDetected);
    }

    [Fact]
    public async Task CheckDrift_ReturnsDriftDetected_WhenWebinarNotFoundInGraph()
    {
        var session = await SeedSessionAsync();

        _graphMock.Setup(g => g.GetWebinarMetadataAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync((TeamsWebinarInfo?)null);

        var result = await _sut.CheckDriftAsync(session.SessionId, OwnerUserId, OboToken);

        result.Should().Be(DriftStatus.DriftDetected);
    }

    [Fact]
    public async Task CheckDrift_ReturnsCurrentStatus_WhenGraphCallFails()
    {
        var session = await SeedSessionAsync(driftStatus: DriftStatus.None);

        _graphMock.Setup(g => g.GetWebinarMetadataAsync(session.TeamsWebinarId!, OboToken, default))
            .ThrowsAsync(new InvalidOperationException("Graph unavailable"));

        var result = await _sut.CheckDriftAsync(session.SessionId, OwnerUserId, OboToken);

        result.Should().Be(DriftStatus.None);
    }

    [Fact]
    public async Task CheckDrift_UsesCachedResult_OnSecondCall()
    {
        var session = await SeedSessionAsync();

        _graphMock.Setup(g => g.GetWebinarMetadataAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(new TeamsWebinarInfo(
                session.TeamsWebinarId!,
                session.Title,
                new DateTimeOffset(session.StartsAt, TimeSpan.Zero),
                new DateTimeOffset(session.EndsAt, TimeSpan.Zero),
                "https://teams.microsoft.com/l/meetup-join/test"));

        // First call — hits Graph
        await _sut.CheckDriftAsync(session.SessionId, OwnerUserId, OboToken);
        // Second call — should use cache
        await _sut.CheckDriftAsync(session.SessionId, OwnerUserId, OboToken);

        _graphMock.Verify(
            g => g.GetWebinarMetadataAsync(session.TeamsWebinarId!, OboToken, default),
            Times.Once,
            "Graph should only be called once; second call should use cache");
    }

    [Fact]
    public async Task CheckDrift_ReturnsNone_WhenTimesWithinTolerance()
    {
        var session = await SeedSessionAsync();

        // Graph times differ by 0.5 seconds — within the 1-second tolerance
        _graphMock.Setup(g => g.GetWebinarMetadataAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(new TeamsWebinarInfo(
                session.TeamsWebinarId!,
                session.Title,
                new DateTimeOffset(session.StartsAt.AddMilliseconds(500), TimeSpan.Zero),
                new DateTimeOffset(session.EndsAt.AddMilliseconds(-500), TimeSpan.Zero),
                "https://teams.microsoft.com/l/meetup-join/test"));

        var result = await _sut.CheckDriftAsync(session.SessionId, OwnerUserId, OboToken);

        result.Should().Be(DriftStatus.None);
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<Session> SeedSessionAsync(
        bool published = true, DriftStatus driftStatus = DriftStatus.None)
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Drift Test Series",
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
            Title = "Drift Test Session",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = published ? SessionStatus.Published : SessionStatus.Draft,
            TeamsWebinarId = published ? "webinar-drift-test" : null,
            DriftStatus = driftStatus,
            ReconcileStatus = ReconcileStatus.Synced
        };
        _db.Sessions.Add(session);

        await _db.SaveChangesAsync();
        return session;
    }
}
