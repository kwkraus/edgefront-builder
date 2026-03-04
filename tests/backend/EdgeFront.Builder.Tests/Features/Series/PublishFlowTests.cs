using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Series;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EdgeFront.Builder.Tests.Features.Series;

/// <summary>
/// Publish-flow tests. All Graph calls are delegated (OBO) — no subscriptions.
/// </summary>
public class PublishFlowTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SeriesService _sut;
    private const string OwnerUserId = "pub-user-oid";
    private const string OboToken = "obo-test-token";

    public PublishFlowTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        _sut = new SeriesService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── HappyPath ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_HappyPath_WebinarsCreated_SeriesAndSessionsPublished()
    {
        // Arrange
        var (series, session1, session2) = await SeedSeriesWithTwoSessionsAsync();

        var graphMock = new Mock<ITeamsGraphClient>();
        graphMock.Setup(g => g.CreateWebinarAsync(
                session1.Title, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), OboToken, default))
            .ReturnsAsync(new CreateWebinarResult("webinar-id-1", "https://teams.microsoft.com/l/meetup-join/1"));
        graphMock.Setup(g => g.CreateWebinarAsync(
                session2.Title, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), OboToken, default))
            .ReturnsAsync(new CreateWebinarResult("webinar-id-2", "https://teams.microsoft.com/l/meetup-join/2"));

        // Act
        var (result, errorCode) = await _sut.PublishAsync(
            series.SeriesId, OwnerUserId, OboToken, graphMock.Object, NullLogger.Instance);

        // Assert
        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Published");

        var dbSeries = await _db.Series.FindAsync(series.SeriesId);
        dbSeries!.Status.Should().Be(SeriesStatus.Published);

        var dbSessions = await _db.Sessions
            .Where(s => s.SeriesId == series.SeriesId).ToListAsync();
        dbSessions.Should().AllSatisfy(s => s.Status.Should().Be(SessionStatus.Published));

        // Webinar IDs should be stored on sessions
        var teamsIds = dbSessions.Select(s => s.TeamsWebinarId).ToList();
        teamsIds.Should().BeEquivalentTo(new[] { "webinar-id-1", "webinar-id-2" });

        // Verify PublishWebinarAsync was called for each webinar
        graphMock.Verify(g => g.PublishWebinarAsync("webinar-id-1", OboToken, default), Times.Once);
        graphMock.Verify(g => g.PublishWebinarAsync("webinar-id-2", OboToken, default), Times.Once);
    }

    // ─── License error → rollback ────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_LicenseException_ReturnsLicenseRequired_NothingPersisted()
    {
        // Arrange
        var (series, _, _) = await SeedSeriesWithTwoSessionsAsync();

        var graphMock = new Mock<ITeamsGraphClient>();
        graphMock.Setup(g => g.CreateWebinarAsync(
                It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), OboToken, default))
            .ThrowsAsync(new TeamsLicenseException());

        // Act
        var (result, errorCode) = await _sut.PublishAsync(
            series.SeriesId, OwnerUserId, OboToken, graphMock.Object, NullLogger.Instance);

        // Assert
        result.Should().BeNull();
        errorCode.Should().Be("TEAMS_LICENSE_REQUIRED");

        // Series must still be Draft
        var dbSeries = await _db.Series.FindAsync(series.SeriesId);
        dbSeries!.Status.Should().Be(SeriesStatus.Draft);
    }

    // ─── Partial failure (second webinar fails) → rollback ──────────────────

    [Fact]
    public async Task PublishAsync_SecondWebinarFails_ReturnsPublishFailed_FirstWebinarDeleted()
    {
        // Arrange
        var (series, session1, session2) = await SeedSeriesWithTwoSessionsAsync();

        var graphMock = new Mock<ITeamsGraphClient>();

        // First webinar succeeds
        graphMock.Setup(g => g.CreateWebinarAsync(
                session1.Title, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), OboToken, default))
            .ReturnsAsync(new CreateWebinarResult("webinar-id-partial", "https://teams.microsoft.com/l/meetup-join/partial"));

        // Second webinar fails with a generic error
        graphMock.Setup(g => g.CreateWebinarAsync(
                session2.Title, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), OboToken, default))
            .ThrowsAsync(new InvalidOperationException("Graph failure"));

        graphMock.Setup(g => g.DeleteWebinarAsync(
                It.IsAny<string>(), OboToken, default))
            .Returns(Task.CompletedTask);

        // Act
        var (result, errorCode) = await _sut.PublishAsync(
            series.SeriesId, OwnerUserId, OboToken, graphMock.Object, NullLogger.Instance);

        // Assert
        result.Should().BeNull();
        errorCode.Should().BeOneOf("PUBLISH_FAILED", "PUBLISH_PARTIAL_FAILURE");

        // Series must still be Draft
        var dbSeries = await _db.Series.FindAsync(series.SeriesId);
        dbSeries!.Status.Should().Be(SeriesStatus.Draft);

        // Rollback delete was called for the first webinar
        graphMock.Verify(g => g.DeleteWebinarAsync("webinar-id-partial", OboToken, default), Times.Once);
    }

    // ─── Null graph client → stub behaviour ─────────────────────────────────

    [Fact]
    public async Task PublishAsync_NullGraphClient_SetsPublishedStatusWithoutTeams()
    {
        // Arrange
        var (series, _, _) = await SeedSeriesWithTwoSessionsAsync();

        // Act — no graphClient, no oboToken
        var (result, errorCode) = await _sut.PublishAsync(series.SeriesId, OwnerUserId);

        // Assert
        errorCode.Should().BeNull();
        result!.Status.Should().Be("Published");

        var dbSeries = await _db.Series.FindAsync(series.SeriesId);
        dbSeries!.Status.Should().Be(SeriesStatus.Published);

        // No Teams webinar IDs should be set
        var sessions = await _db.Sessions.Where(s => s.SeriesId == series.SeriesId).ToListAsync();
        sessions.Should().AllSatisfy(s => s.TeamsWebinarId.Should().BeNull());
    }

    // ─── Non-existent series ─────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_ReturnsSeriesNotFound_WhenSeriesDoesNotExist()
    {
        var (result, errorCode) = await _sut.PublishAsync(Guid.NewGuid(), OwnerUserId);

        result.Should().BeNull();
        errorCode.Should().Be("series_not_found");
    }

    [Fact]
    public async Task PublishAsync_ReturnsSeriesNotFound_ForWrongOwner()
    {
        var (series, _, _) = await SeedSeriesWithTwoSessionsAsync();

        var (result, errorCode) = await _sut.PublishAsync(series.SeriesId, "other-user");

        result.Should().BeNull();
        errorCode.Should().Be("series_not_found");
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session, Session)> SeedSeriesWithTwoSessionsAsync()
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Test Series",
            Status = SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Series.Add(series);

        var s1 = BuildSession(series.SeriesId, "Session Alpha");
        var s2 = BuildSession(series.SeriesId, "Session Beta");
        _db.Sessions.AddRange(s1, s2);
        await _db.SaveChangesAsync();
        return (series, s1, s2);
    }

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session)> SeedSeriesWithOneSessionAsync()
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Single Session Series",
            Status = SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Series.Add(series);

        var s1 = BuildSession(series.SeriesId, "Solo Session");
        _db.Sessions.Add(s1);
        await _db.SaveChangesAsync();
        return (series, s1);
    }

    private static Session BuildSession(Guid seriesId, string title) => new()
    {
        SessionId = Guid.NewGuid(),
        SeriesId = seriesId,
        OwnerUserId = OwnerUserId,
        Title = title,
        StartsAt = DateTime.UtcNow.AddDays(1),
        EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1),
        Status = SessionStatus.Draft
    };
}
