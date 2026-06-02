using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.People;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Tests.Features.Sessions;

public class SessionRoleManagementTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SessionService _sut;
    private const string OwnerUserId = "role-mgmt-user";
    private const string OtherUserId = "other-user";

    public SessionRoleManagementTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .Options;
        _db = new AppDbContext(options);
        _sut = new SessionService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SetPresentersAsync_NullPeople_ReturnsError()
    {
        var (_, session) = await SeedSessionAsync();
        var req = new SetPresentersRequest(null!);

        var (result, errorCode) = await _sut.SetPresentersAsync(session.SessionId, OwnerUserId, req);

        result.Should().BeNull();
        errorCode.Should().Be("people_required");
    }

    [Fact]
    public async Task SetPresentersAsync_DuplicateEntraUserId_ReturnsError()
    {
        var (_, session) = await SeedSessionAsync();
        var people = new List<PersonInput>
        {
            new("user-aaa", "Alice", "alice@example.com"),
            new("user-aaa", "Alice Duplicate", "alice2@example.com"),
        };
        var req = new SetPresentersRequest(people);

        var (result, errorCode) = await _sut.SetPresentersAsync(session.SessionId, OwnerUserId, req);

        result.Should().BeNull();
        errorCode.Should().Be("duplicate_entra_user_id");
    }

    [Fact]
    public async Task SetPresentersAsync_SavesAndReplacesRows()
    {
        var (_, session) = await SeedSessionAsync();
        _db.SessionPresenters.Add(new SessionPresenter
        {
            SessionPresenterId = Guid.NewGuid(),
            SessionId = session.SessionId,
            EntraUserId = "old-user",
            DisplayName = "Old User",
            Email = "old@example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await _db.SaveChangesAsync();

        var req = new SetPresentersRequest(new List<PersonInput>
        {
            new("new-user", "New User", "new@example.com"),
        });

        var (result, errorCode) = await _sut.SetPresentersAsync(session.SessionId, OwnerUserId, req);

        errorCode.Should().BeNull();
        result.Should().HaveCount(1);
        result![0].EntraUserId.Should().Be("new-user");

        var saved = await _db.SessionPresenters.Where(p => p.SessionId == session.SessionId).ToListAsync();
        saved.Should().HaveCount(1);
        saved[0].EntraUserId.Should().Be("new-user");
    }

    [Fact]
    public async Task SetCoordinatorsAsync_DuplicateEntraUserId_ReturnsError()
    {
        var (_, session) = await SeedSessionAsync();
        var people = new List<PersonInput>
        {
            new("user-bbb", "Bob", "bob@example.com"),
            new("user-bbb", "Bob Duplicate", "bob2@example.com"),
        };
        var req = new SetCoordinatorsRequest(people);

        var (result, errorCode) = await _sut.SetCoordinatorsAsync(session.SessionId, OwnerUserId, req);

        result.Should().BeNull();
        errorCode.Should().Be("duplicate_entra_user_id");
    }

    [Fact]
    public async Task SetCoordinatorsAsync_WrongOwner_ReturnsError()
    {
        var (_, session) = await SeedSessionAsync();
        var req = new SetCoordinatorsRequest(new List<PersonInput>());

        var (result, errorCode) = await _sut.SetCoordinatorsAsync(session.SessionId, OtherUserId, req);

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    [Fact]
    public async Task SetCoordinatorsAsync_SavesRows()
    {
        var (_, session) = await SeedSessionAsync();
        var req = new SetCoordinatorsRequest(new List<PersonInput>
        {
            new("user-c1", "Coordinator One", "c1@example.com"),
        });

        var (result, errorCode) = await _sut.SetCoordinatorsAsync(session.SessionId, OwnerUserId, req);

        errorCode.Should().BeNull();
        result.Should().HaveCount(1);
        result![0].EntraUserId.Should().Be("user-c1");

        var saved = await _db.SessionCoordinators.Where(c => c.SessionId == session.SessionId).ToListAsync();
        saved.Should().HaveCount(1);
        saved[0].EntraUserId.Should().Be("user-c1");
    }

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session)> SeedSessionAsync()
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Test Series",
            Status = SeriesStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Series.Add(series);

        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = series.SeriesId,
            OwnerUserId = OwnerUserId,
            Title = "Test Session",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = SessionStatus.Draft,
            DriftStatus = DriftStatus.None,
            ReconcileStatus = ReconcileStatus.Synced
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return (series, session);
    }
}
