using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EdgeFront.Builder.Tests.Features.Sessions;

public class SessionPublishTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SessionService _sut;
    private const string OwnerUserId = "pub-session-user";

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

    [Fact]
    public async Task PublishAsync_HappyPath_TransitionsStatusWithoutTeamsFields()
    {
        var (_, session) = await SeedPublishedSeriesWithDraftSessionAsync();

        var (result, errorCode, _) = await _sut.PublishAsync(session.SessionId, OwnerUserId);

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Published");
        result.TeamsWebinarId.Should().BeNull();
        result.JoinWebUrl.Should().BeNull();

        var dbSession = await _db.Sessions.FindAsync(session.SessionId);
        dbSession!.Status.Should().Be(SessionStatus.Published);
        dbSession.TeamsWebinarId.Should().BeNull();
        dbSession.JoinWebUrl.Should().BeNull();
        dbSession.ReconcileStatus.Should().Be(ReconcileStatus.Synced);
        dbSession.LastSyncAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_SessionAlreadyPublished_ReturnsError()
    {
        var (_, session) = await SeedPublishedSeriesWithDraftSessionAsync();
        session.Status = SessionStatus.Published;
        await _db.SaveChangesAsync();

        var (result, errorCode, _) = await _sut.PublishAsync(session.SessionId, OwnerUserId);

        result.Should().BeNull();
        errorCode.Should().Be("SESSION_ALREADY_PUBLISHED");
    }

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

        var (result, errorCode, _) = await _sut.PublishAsync(session.SessionId, OwnerUserId);

        result.Should().BeNull();
        errorCode.Should().Be("SERIES_NOT_PUBLISHED");
    }

    [Fact]
    public async Task PublishAsync_SessionNotFound_ReturnsError()
    {
        var (result, errorCode, _) = await _sut.PublishAsync(Guid.NewGuid(), OwnerUserId);

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

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
