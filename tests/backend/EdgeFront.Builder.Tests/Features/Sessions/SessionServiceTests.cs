using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Tests.Features.Sessions;

public class SessionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SessionService _sut;
    private const string OwnerUserId = "user-oid-123";
    private const string OtherUserId = "other-user-oid-456";

    public SessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .Options;
        _db = new AppDbContext(options);
        _sut = new SessionService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenEndsAtNotAfterStartsAt()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        var req = new CreateSessionRequest(
            "Bad Times",
            StartsAt: DateTime.UtcNow.AddHours(2),
            EndsAt: DateTime.UtcNow.AddHours(1));  // EndsAt before StartsAt

        // Act
        var (session, errorCode) = await _sut.CreateAsync(series.SeriesId, req, OwnerUserId);

        // Assert
        session.Should().BeNull();
        errorCode.Should().Be("invalid_time_range");
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenEndsAtEqualsStartsAt()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        var when = DateTime.UtcNow.AddHours(1);
        var req = new CreateSessionRequest("Zero Duration", when, when);

        // Act
        var (session, errorCode) = await _sut.CreateAsync(series.SeriesId, req, OwnerUserId);

        // Assert
        session.Should().BeNull();
        errorCode.Should().Be("invalid_time_range");
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenSeriesDoesNotExist()
    {
        // Act — use a random series ID that has no matching row
        var req = new CreateSessionRequest(
            "Orphan Session",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(2));

        var (session, errorCode) = await _sut.CreateAsync(Guid.NewGuid(), req, OwnerUserId);

        // Assert
        session.Should().BeNull();
        errorCode.Should().Be("series_not_found");
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenSeriesBelongsToDifferentOwner()
    {
        // Arrange — series owned by another user
        var series = BuildSeries(ownerOverride: OtherUserId);
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        var req = new CreateSessionRequest(
            "Cross-owner",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(2));

        // Act
        var (session, errorCode) = await _sut.CreateAsync(series.SeriesId, req, OwnerUserId);

        // Assert
        session.Should().BeNull();
        errorCode.Should().Be("series_not_found");
    }

    [Fact]
    public async Task CreateAsync_CreatesSession()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        var startsAt = DateTime.UtcNow.AddHours(1);
        var endsAt = startsAt.AddHours(1);
        var req = new CreateSessionRequest("Valid Session", startsAt, endsAt);

        // Act
        var (session, errorCode) = await _sut.CreateAsync(series.SeriesId, req, OwnerUserId);

        // Assert
        errorCode.Should().BeNull();
        session.Should().NotBeNull();
        session!.SeriesId.Should().Be(series.SeriesId);
        session.Title.Should().Be("Valid Session");
    }

    // ---------- GetBySeriesAsync ----------

    [Fact]
    public async Task GetBySeriesAsync_ReturnsEmptyList_ForUnknownSeries()
    {
        // Act
        var result = (await _sut.GetBySeriesAsync(Guid.NewGuid(), OwnerUserId)).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySeriesAsync_ReturnsOnlySessionsForSeries()
    {
        // Arrange
        var series1 = BuildSeries();
        var series2 = BuildSeries();
        _db.Series.AddRange(series1, series2);
        _db.Sessions.AddRange(
            BuildSession(series1.SeriesId),
            BuildSession(series1.SeriesId),
            BuildSession(series2.SeriesId));
        await _db.SaveChangesAsync();

        // Act
        var result = (await _sut.GetBySeriesAsync(series1.SeriesId, OwnerUserId)).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBySeriesAsync_ReturnsZeroPresenterAndCoordinatorCounts_WhenNoneAdded()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        _db.Sessions.Add(BuildSession(series.SeriesId));
        await _db.SaveChangesAsync();

        // Act
        var result = (await _sut.GetBySeriesAsync(series.SeriesId, OwnerUserId)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].PresenterCount.Should().Be(0);
        result[0].CoordinatorCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBySeriesAsync_ReturnsCorrectPresenterCount()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);

        _db.Set<SessionPresenter>().AddRange(
            new SessionPresenter { SessionPresenterId = Guid.NewGuid(), SessionId = session.SessionId, EntraUserId = "p1", DisplayName = "Presenter 1", Email = "p1@test.com", CreatedAt = DateTime.UtcNow },
            new SessionPresenter { SessionPresenterId = Guid.NewGuid(), SessionId = session.SessionId, EntraUserId = "p2", DisplayName = "Presenter 2", Email = "p2@test.com", CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        // Act
        var result = (await _sut.GetBySeriesAsync(series.SeriesId, OwnerUserId)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].PresenterCount.Should().Be(2);
        result[0].CoordinatorCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBySeriesAsync_ReturnsCorrectCoordinatorCount()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);

        _db.Set<SessionCoordinator>().Add(
            new SessionCoordinator { SessionCoordinatorId = Guid.NewGuid(), SessionId = session.SessionId, EntraUserId = "c1", DisplayName = "Coordinator 1", Email = "c1@test.com", CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        // Act
        var result = (await _sut.GetBySeriesAsync(series.SeriesId, OwnerUserId)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].PresenterCount.Should().Be(0);
        result[0].CoordinatorCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBySeriesAsync_PresenterAndCoordinatorCounts_AreIsolatedPerSession()
    {
        // Arrange: two sessions in the same series; each has different role counts
        var series = BuildSeries();
        _db.Series.Add(series);
        var session1 = BuildSession(series.SeriesId);
        var session2 = BuildSession(series.SeriesId);
        _db.Sessions.AddRange(session1, session2);

        _db.Set<SessionPresenter>().Add(
            new SessionPresenter { SessionPresenterId = Guid.NewGuid(), SessionId = session1.SessionId, EntraUserId = "p1", DisplayName = "Presenter", Email = "p@test.com", CreatedAt = DateTime.UtcNow });
        _db.Set<SessionCoordinator>().AddRange(
            new SessionCoordinator { SessionCoordinatorId = Guid.NewGuid(), SessionId = session2.SessionId, EntraUserId = "c1", DisplayName = "Coord 1", Email = "c1@test.com", CreatedAt = DateTime.UtcNow },
            new SessionCoordinator { SessionCoordinatorId = Guid.NewGuid(), SessionId = session2.SessionId, EntraUserId = "c2", DisplayName = "Coord 2", Email = "c2@test.com", CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        // Act
        var result = (await _sut.GetBySeriesAsync(series.SeriesId, OwnerUserId))
            .ToDictionary(s => s.SessionId);

        // Assert
        result[session1.SessionId].PresenterCount.Should().Be(1);
        result[session1.SessionId].CoordinatorCount.Should().Be(0);
        result[session2.SessionId].PresenterCount.Should().Be(0);
        result[session2.SessionId].CoordinatorCount.Should().Be(2);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForWrongOwner()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(session.SessionId, OtherUserId);

        // Assert
        result.Should().BeNull();
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_RemovesSession_ReturnsTrue()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(session.SessionId, OwnerUserId);

        // Assert
        result.Should().BeTrue();
        var saved = await _db.Sessions.FindAsync(session.SessionId);
        saved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForNonExistentSession()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), OwnerUserId);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForWrongOwner()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(session.SessionId, OtherUserId);

        // Assert
        result.Should().BeFalse();
        (await _db.Sessions.FindAsync(session.SessionId)).Should().NotBeNull("session should not be deleted by wrong owner");
    }

    [Fact]
    public async Task DeleteAsync_DeletesSession_WhenLastInSeries()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);

        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(session.SessionId, OwnerUserId);

        // Assert
        result.Should().BeTrue();
        (await _db.Sessions.FindAsync(session.SessionId)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DeletesSession_WhenOtherSessionsRemain()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);

        var session1 = BuildSession(series.SeriesId);
        var session2 = BuildSession(series.SeriesId);
        _db.Sessions.AddRange(session1, session2);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(session1.SessionId, OwnerUserId);

        // Assert
        result.Should().BeTrue();
        (await _db.Sessions.FindAsync(session1.SessionId)).Should().BeNull();
        (await _db.Sessions.FindAsync(session2.SessionId)).Should().NotBeNull("remaining session should not be deleted");
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_UpdatesAllFields()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        var newStart = DateTime.UtcNow.AddDays(5);
        var newEnd = newStart.AddHours(2);
        var req = new UpdateSessionRequest("New Title", newStart, newEnd);

        // Act
        var (result, errorCode) = await _sut.UpdateAsync(session.SessionId, req, OwnerUserId);

        // Assert
        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Title");
        result.StartsAt.Should().BeCloseTo(newStart, TimeSpan.FromSeconds(1));
        result.EndsAt.Should().BeCloseTo(newEnd, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateAsync_ReturnsError_WhenEndsAtNotAfterStartsAt()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        var when = DateTime.UtcNow.AddDays(1);
        var req = new UpdateSessionRequest("Title", when, when.AddHours(-1));

        // Act
        var (result, errorCode) = await _sut.UpdateAsync(session.SessionId, req, OwnerUserId);

        // Assert
        result.Should().BeNull();
        errorCode.Should().Be("invalid_time_range");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsError_WhenSessionNotFound()
    {
        var req = new UpdateSessionRequest("Title", DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        var (result, errorCode) = await _sut.UpdateAsync(Guid.NewGuid(), req, OwnerUserId);

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsError_ForWrongOwner()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        var req = new UpdateSessionRequest("Hack", DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        // Act
        var (result, errorCode) = await _sut.UpdateAsync(session.SessionId, req, OtherUserId);

        // Assert
        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    [Fact]
    public async Task UpdateTitleAsync_UpdatesOnlyTitle()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        var originalStartsAt = session.StartsAt;
        var originalEndsAt = session.EndsAt;
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        var req = new UpdateSessionTitleRequest("Renamed Session");

        // Act
        var (result, errorCode) = await _sut.UpdateTitleAsync(session.SessionId, req, OwnerUserId);

        // Assert
        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Renamed Session");
        result.StartsAt.Should().BeCloseTo(originalStartsAt, TimeSpan.FromSeconds(1));
        result.EndsAt.Should().BeCloseTo(originalEndsAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateTitleAsync_UpdatesTitle()
    {
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        var req = new UpdateSessionTitleRequest("Renamed");

        var (result, errorCode) = await _sut.UpdateTitleAsync(
            session.SessionId,
            req,
            OwnerUserId);

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Renamed");
    }

    [Fact]
    public async Task UpdateTitleAsync_ReturnsError_WhenSessionNotFound()
    {
        var req = new UpdateSessionTitleRequest("Title");

        var (result, errorCode) = await _sut.UpdateTitleAsync(Guid.NewGuid(), req, OwnerUserId);

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    [Fact]
    public async Task UpdateTitleAsync_ReturnsError_ForWrongOwner()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        var req = new UpdateSessionTitleRequest("Hack");

        // Act
        var (result, errorCode) = await _sut.UpdateTitleAsync(session.SessionId, req, OtherUserId);

        // Assert
        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    // ---------- GetByIdAsync (success) ----------

    [Fact]
    public async Task GetByIdAsync_ReturnsSession_WithCorrectFields()
    {
        // Arrange
        var series = BuildSeries();
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(session.SessionId, OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be(session.SessionId);
        result.SeriesId.Should().Be(series.SeriesId);
        result.Title.Should().Be("Test Session");
    }

    // ---------- Helpers ----------

    private EdgeFront.Builder.Domain.Entities.Series BuildSeries(string? ownerOverride = null) =>
        new()
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = ownerOverride ?? OwnerUserId,
            Title = "Test Series " + Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private Session BuildSession(Guid seriesId) =>
        new()
        {
            SessionId = Guid.NewGuid(),
            SeriesId = seriesId,
            OwnerUserId = OwnerUserId,
            Title = "Test Session",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1)
        };
}
