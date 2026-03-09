using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
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
/// Tests for individual session publish (Draft → Published within an already-Published series).
/// </summary>
public class SessionPublishTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SessionService _sut;
    private const string OwnerUserId = "pub-session-user";
    private const string OboToken = "obo-test-token";

    public SessionPublishTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        _sut = new SessionService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_HappyPath_CreatesWebinarAndTransitionsStatus()
    {
        var (_, session) = await SeedPublishedSeriesWithDraftSessionAsync();

        var graphMock = new Mock<ITeamsGraphClient>();
        graphMock.Setup(g => g.CreateWebinarAsync(
                session.Title, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), OboToken, default))
            .ReturnsAsync(new CreateWebinarResult("webinar-session-1", "https://teams.microsoft.com/l/meetup-join/session-1"));

        var (result, errorCode, _) = await _sut.PublishAsync(
            session.SessionId, OwnerUserId, OboToken, graphMock.Object, NullLogger.Instance);

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Published");
        result.TeamsWebinarId.Should().Be("webinar-session-1");
        result.JoinWebUrl.Should().Be("https://teams.microsoft.com/l/meetup-join/session-1");

        var dbSession = await _db.Sessions.FindAsync(session.SessionId);
        dbSession!.Status.Should().Be(SessionStatus.Published);
        dbSession.TeamsWebinarId.Should().Be("webinar-session-1");
        dbSession.JoinWebUrl.Should().Be("https://teams.microsoft.com/l/meetup-join/session-1");
        dbSession.ReconcileStatus.Should().Be(ReconcileStatus.Synced);
        dbSession.LastSyncAt.Should().NotBeNull();

        graphMock.Verify(g => g.PublishWebinarAsync("webinar-session-1", OboToken, default), Times.Once);
    }

    // ─── Already published ──────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_SessionAlreadyPublished_ReturnsError()
    {
        var (series, session) = await SeedPublishedSeriesWithDraftSessionAsync();
        session.Status = SessionStatus.Published;
        session.TeamsWebinarId = "existing-webinar";
        await _db.SaveChangesAsync();

        var (result, errorCode, _) = await _sut.PublishAsync(
            session.SessionId, OwnerUserId, OboToken, Mock.Of<ITeamsGraphClient>(), NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("SESSION_ALREADY_PUBLISHED");
    }

    // ─── Series not published ───────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_SeriesStillDraft_ReturnsError()
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Draft Series",
            Status = SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Series.Add(series);

        var session = BuildDraftSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        var (result, errorCode, _) = await _sut.PublishAsync(
            session.SessionId, OwnerUserId, OboToken, Mock.Of<ITeamsGraphClient>(), NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("SERIES_NOT_PUBLISHED");
    }

    // ─── Session not found ──────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_SessionNotFound_ReturnsError()
    {
        var (result, errorCode, _) = await _sut.PublishAsync(
            Guid.NewGuid(), OwnerUserId, OboToken, Mock.Of<ITeamsGraphClient>(), NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    // ─── License exception ──────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_LicenseException_ReturnsLicenseRequired_NoStatusChange()
    {
        var (_, session) = await SeedPublishedSeriesWithDraftSessionAsync();

        var graphMock = new Mock<ITeamsGraphClient>();
        graphMock.Setup(g => g.CreateWebinarAsync(
                It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), OboToken, default))
            .ThrowsAsync(new TeamsLicenseException());

        var (result, errorCode, _) = await _sut.PublishAsync(
            session.SessionId, OwnerUserId, OboToken, graphMock.Object, NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("TEAMS_LICENSE_REQUIRED");

        var dbSession = await _db.Sessions.FindAsync(session.SessionId);
        dbSession!.Status.Should().Be(SessionStatus.Draft);
    }

    // ─── Graph failure → rollback ───────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_GraphFailure_ReturnsPublishFailed_RollsBackWebinar()
    {
        var (_, session) = await SeedPublishedSeriesWithDraftSessionAsync();

        var graphMock = new Mock<ITeamsGraphClient>();
        graphMock.Setup(g => g.CreateWebinarAsync(
                It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), OboToken, default))
            .ReturnsAsync(new CreateWebinarResult("webinar-to-rollback", "https://teams.microsoft.com/l/meetup-join/rollback"));
        graphMock.Setup(g => g.PublishWebinarAsync("webinar-to-rollback", OboToken, default))
            .ThrowsAsync(new InvalidOperationException("Graph publish failed"));
        graphMock.Setup(g => g.DeleteWebinarAsync("webinar-to-rollback", OboToken, default))
            .Returns(Task.CompletedTask);

        var (result, errorCode, _) = await _sut.PublishAsync(
            session.SessionId, OwnerUserId, OboToken, graphMock.Object, NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("SESSION_PUBLISH_FAILED");

        var dbSession = await _db.Sessions.FindAsync(session.SessionId);
        dbSession!.Status.Should().Be(SessionStatus.Draft);
        dbSession.TeamsWebinarId.Should().BeNull();

        graphMock.Verify(g => g.DeleteWebinarAsync("webinar-to-rollback", OboToken, default), Times.Once);
    }

    // ─── Null graph client → stub path ──────────────────────────────────────

    [Fact]
    public async Task PublishAsync_NullGraphClient_SetsPublishedWithoutTeams()
    {
        var (_, session) = await SeedPublishedSeriesWithDraftSessionAsync();

        var (result, errorCode, _) = await _sut.PublishAsync(
            session.SessionId, OwnerUserId);

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Published");
        result.TeamsWebinarId.Should().BeNull();

        var dbSession = await _db.Sessions.FindAsync(session.SessionId);
        dbSession!.Status.Should().Be(SessionStatus.Published);
    }

    // ─── Past dates ───────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_SessionDatesInPast_ReturnsError()
    {
        var (series, session) = await SeedPublishedSeriesWithDraftSessionAsync();
        session.StartsAt = DateTime.UtcNow.AddHours(-2);
        session.EndsAt = DateTime.UtcNow.AddHours(-1);
        await _db.SaveChangesAsync();

        var (result, errorCode, errorMessage) = await _sut.PublishAsync(
            session.SessionId, OwnerUserId, OboToken, Mock.Of<ITeamsGraphClient>(), NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("SESSION_DATES_IN_PAST");
        errorMessage.Should().Contain("already started");
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session)> SeedPublishedSeriesWithDraftSessionAsync()
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Published Series",
            Status = SeriesStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Series.Add(series);

        var session = BuildDraftSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return (series, session);
    }

    private static Session BuildDraftSession(Guid seriesId) => new()
    {
        SessionId = Guid.NewGuid(),
        SeriesId = seriesId,
        OwnerUserId = OwnerUserId,
        Title = "New Draft Session",
        StartsAt = DateTime.UtcNow.AddDays(7),
        EndsAt = DateTime.UtcNow.AddDays(7).AddHours(1),
        Status = SessionStatus.Draft
    };
}
