using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Series;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EdgeFront.Builder.Tests.Features.Series;

public class PublishFlowTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SeriesService _sut;
    private const string OwnerUserId = "pub-user-oid";

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

    [Fact]
    public async Task PublishAsync_PublishesSeriesAndDraftSessions()
    {
        var (series, _, _) = await SeedSeriesWithTwoSessionsAsync();

        var (result, errorCode) = await _sut.PublishAsync(series.SeriesId, OwnerUserId);

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Published");

        var dbSeries = await _db.Series.FindAsync(series.SeriesId);
        dbSeries!.Status.Should().Be(SeriesStatus.Published);

        var dbSessions = await _db.Sessions
            .Where(s => s.SeriesId == series.SeriesId).ToListAsync();
        dbSessions.Should().AllSatisfy(s =>
        {
            s.Status.Should().Be(SessionStatus.Published);
            s.TeamsWebinarId.Should().BeNull();
        });
    }

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
