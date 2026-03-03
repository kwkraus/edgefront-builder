using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Series;
using EdgeFront.Builder.Features.Series.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Tests.Features.Series;

public class SeriesServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SeriesService _sut;
    private const string OwnerUserId = "user-oid-123";
    private const string OtherUserId = "other-user-oid-456";

    public SeriesServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .Options;
        _db = new AppDbContext(options);
        _sut = new SeriesService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ReturnsOnlySeriesOwnedByUser()
    {
        // Arrange
        _db.Series.AddRange(
            BuildSeries("Alpha", OwnerUserId),
            BuildSeries("Beta", OwnerUserId),
            BuildSeries("Gamma", OtherUserId));
        await _db.SaveChangesAsync();

        // Act
        var result = (await _sut.GetAllAsync(OwnerUserId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.Status.Should().Be("Draft"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCorrectMetricsAndSessionCount()
    {
        // Arrange
        var series = BuildSeries("Alpha", OwnerUserId);
        _db.Series.Add(series);
        _db.Sessions.Add(BuildSession(series.SeriesId, OwnerUserId));
        _db.Sessions.Add(BuildSession(series.SeriesId, OwnerUserId));
        _db.SeriesMetrics.Add(new SeriesMetrics
        {
            SeriesId = series.SeriesId,
            TotalRegistrations = 10,
            TotalAttendees = 5,
            UniqueAccountsInfluenced = 3
        });
        await _db.SaveChangesAsync();

        // Act
        var result = (await _sut.GetAllAsync(OwnerUserId)).Single();

        // Assert
        result.SessionCount.Should().Be(2);
        result.TotalRegistrations.Should().Be(10);
        result.TotalAttendees.Should().Be(5);
        result.UniqueAccountsInfluenced.Should().Be(3);
    }

    [Fact]
    public async Task GetAllAsync_HasReconcileIssues_IsFalse_WithDelegatedOnlyModel()
    {
        // Arrange — with delegated-only model, hasReconcileIssues is always false
        var series = BuildSeries("Alpha", OwnerUserId);
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId, OwnerUserId);
        session.ReconcileStatus = ReconcileStatus.Reconciling;
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // Act
        var result = (await _sut.GetAllAsync(OwnerUserId)).Single();

        // Assert
        result.HasReconcileIssues.Should().BeFalse();
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_CreatesSeries_WithDraftStatus()
    {
        // Act
        var result = await _sut.CreateAsync(new CreateSeriesRequest("My Series"), OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("My Series");
        result.Status.Should().Be("Draft");
        result.SeriesId.Should().NotBeEmpty();

        var saved = await _db.Series.FindAsync(result.SeriesId);
        saved.Should().NotBeNull();
        saved!.OwnerUserId.Should().Be(OwnerUserId);
        saved.Status.Should().Be(SeriesStatus.Draft);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtAndUpdatedAt_ToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = await _sut.CreateAsync(new CreateSeriesRequest("Timed Series"), OwnerUserId);

        // Assert
        result.CreatedAt.Should().BeAfter(before);
        result.UpdatedAt.Should().BeAfter(before);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_UpdatesTitle()
    {
        // Arrange
        var series = BuildSeries("Old Title", OwnerUserId);
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.UpdateAsync(series.SeriesId, new UpdateSeriesRequest("New Title"), OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Title");

        var saved = await _db.Series.FindAsync(series.SeriesId);
        saved!.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForWrongOwner()
    {
        // Arrange
        var series = BuildSeries("Alpha", OwnerUserId);
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.UpdateAsync(series.SeriesId, new UpdateSeriesRequest("Hack"), OtherUserId);

        // Assert
        result.Should().BeNull();
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_RemovesSeries_ReturnsTrue()
    {
        // Arrange
        var series = BuildSeries("ToDelete", OwnerUserId);
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(series.SeriesId, OwnerUserId);

        // Assert
        result.Should().BeTrue();
        var saved = await _db.Series.FindAsync(series.SeriesId);
        saved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var result = await _sut.DeleteAsync(Guid.NewGuid(), OwnerUserId);

        // Assert
        result.Should().BeFalse();
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForWrongOwner()
    {
        // Arrange
        var series = BuildSeries("Secret", OwnerUserId);
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(series.SeriesId, OtherUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSeries_ForCorrectOwner()
    {
        // Arrange
        var series = BuildSeries("Visible", OwnerUserId);
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(series.SeriesId, OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Visible");
    }

    // ---------- Helpers ----------

    private static EdgeFront.Builder.Domain.Entities.Series BuildSeries(string title, string owner) =>
        new()
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = owner,
            Title = title,
            Status = SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static Session BuildSession(Guid seriesId, string owner) =>
        new()
        {
            SessionId = Guid.NewGuid(),
            SeriesId = seriesId,
            OwnerUserId = owner,
            Title = "Session",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = SessionStatus.Draft,
            DriftStatus = DriftStatus.None,
            ReconcileStatus = ReconcileStatus.Synced
        };
}
