using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Features.Metrics.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Tests.Features.Metrics;

/// <summary>
/// Tests for MetricsService — ownership-checked metrics query layer.
/// </summary>
public class MetricsServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly MetricsService _sut;
    private const string OwnerUserId = "metrics-query-user";
    private const string OtherUserId = "other-query-user";

    public MetricsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .Options;
        _db = new AppDbContext(options);
        _sut = new MetricsService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── GetSeriesMetricsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetSeriesMetrics_ReturnsNull_WhenSeriesNotFound()
    {
        var result = await _sut.GetSeriesMetricsAsync(Guid.NewGuid(), OwnerUserId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSeriesMetrics_ReturnsNull_ForWrongOwner()
    {
        var series = await SeedSeriesAsync();
        await SeedSeriesMetricsAsync(series.SeriesId);

        var result = await _sut.GetSeriesMetricsAsync(series.SeriesId, OtherUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSeriesMetrics_ReturnsNull_WhenNoMetricsExist()
    {
        var series = await SeedSeriesAsync();

        var result = await _sut.GetSeriesMetricsAsync(series.SeriesId, OwnerUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSeriesMetrics_ReturnsDto_WithCorrectValues()
    {
        var series = await SeedSeriesAsync();
        await SeedSeriesMetricsAsync(series.SeriesId, totalReg: 15, totalAtt: 8, influenced: 5);

        var result = await _sut.GetSeriesMetricsAsync(series.SeriesId, OwnerUserId);

        result.Should().NotBeNull();
        result!.SeriesId.Should().Be(series.SeriesId);
        result.TotalRegistrations.Should().Be(15);
        result.TotalAttendees.Should().Be(8);
        result.UniqueAccountsInfluenced.Should().Be(5);
    }

    [Fact]
    public async Task GetSeriesMetrics_MapsWarmAccounts_ToDtos()
    {
        var series = await SeedSeriesAsync();
        var warmAccounts = new List<WarmAccountEntry>
        {
            new() { AccountDomain = "acme.com", WarmRule = WarmRule.W1 },
            new() { AccountDomain = "contoso.com", WarmRule = WarmRule.W2 },
        };
        await SeedSeriesMetricsAsync(series.SeriesId, warmAccounts: warmAccounts);

        var result = await _sut.GetSeriesMetricsAsync(series.SeriesId, OwnerUserId);

        result.Should().NotBeNull();
        result!.WarmAccounts.Should().HaveCount(2);
        result.WarmAccounts.Should().ContainSingle(w => w.AccountDomain == "acme.com" && w.WarmRule == "W1");
        result.WarmAccounts.Should().ContainSingle(w => w.AccountDomain == "contoso.com" && w.WarmRule == "W2");
    }

    // ─── GetSessionMetricsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetSessionMetrics_ReturnsNull_WhenSessionNotFound()
    {
        var result = await _sut.GetSessionMetricsAsync(Guid.NewGuid(), OwnerUserId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionMetrics_ReturnsNull_ForWrongOwner()
    {
        var (_, session) = await SeedSessionAsync();
        await SeedSessionMetricsAsync(session.SessionId);

        var result = await _sut.GetSessionMetricsAsync(session.SessionId, OtherUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionMetrics_ReturnsNull_WhenNoMetricsExist()
    {
        var (_, session) = await SeedSessionAsync();

        var result = await _sut.GetSessionMetricsAsync(session.SessionId, OwnerUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionMetrics_ReturnsDto_WithCorrectValues()
    {
        var (_, session) = await SeedSessionAsync();
        await SeedSessionMetricsAsync(session.SessionId,
            totalReg: 20, totalAtt: 12, uniqueRegDomains: 6, uniqueAttDomains: 4);

        var result = await _sut.GetSessionMetricsAsync(session.SessionId, OwnerUserId);

        result.Should().NotBeNull();
        result!.SessionId.Should().Be(session.SessionId);
        result.TotalRegistrations.Should().Be(20);
        result.TotalAttendees.Should().Be(12);
        result.UniqueRegistrantAccountDomains.Should().Be(6);
        result.UniqueAttendeeAccountDomains.Should().Be(4);
    }

    [Fact]
    public async Task GetSessionMetrics_ReturnsWarmAccountsTriggered()
    {
        var (_, session) = await SeedSessionAsync();
        var warmDomains = new List<string> { "acme.com", "widgets.io" };
        await SeedSessionMetricsAsync(session.SessionId, warmAccounts: warmDomains);

        var result = await _sut.GetSessionMetricsAsync(session.SessionId, OwnerUserId);

        result.Should().NotBeNull();
        result!.WarmAccountsTriggered.Should().BeEquivalentTo(warmDomains);
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<EdgeFront.Builder.Domain.Entities.Series> SeedSeriesAsync()
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "MetricsQuery Test Series",
            Status = SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Series.Add(series);
        await _db.SaveChangesAsync();
        return series;
    }

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session)> SeedSessionAsync()
    {
        var series = await SeedSeriesAsync();
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = series.SeriesId,
            OwnerUserId = OwnerUserId,
            Title = "MetricsQuery Session",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = SessionStatus.Draft
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return (series, session);
    }

    private async Task SeedSeriesMetricsAsync(
        Guid seriesId, int totalReg = 0, int totalAtt = 0, int influenced = 0,
        List<WarmAccountEntry>? warmAccounts = null)
    {
        _db.SeriesMetrics.Add(new SeriesMetrics
        {
            SeriesId = seriesId,
            TotalRegistrations = totalReg,
            TotalAttendees = totalAtt,
            UniqueAccountsInfluenced = influenced,
            WarmAccounts = warmAccounts ?? []
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedSessionMetricsAsync(
        Guid sessionId, int totalReg = 0, int totalAtt = 0,
        int uniqueRegDomains = 0, int uniqueAttDomains = 0,
        List<string>? warmAccounts = null)
    {
        _db.SessionMetrics.Add(new SessionMetrics
        {
            SessionId = sessionId,
            TotalRegistrations = totalReg,
            TotalAttendees = totalAtt,
            UniqueRegistrantAccountDomains = uniqueRegDomains,
            UniqueAttendeeAccountDomains = uniqueAttDomains,
            WarmAccountsTriggered = warmAccounts ?? []
        });
        await _db.SaveChangesAsync();
    }
}
