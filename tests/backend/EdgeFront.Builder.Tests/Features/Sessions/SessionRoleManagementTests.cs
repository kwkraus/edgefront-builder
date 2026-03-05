using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.People;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EdgeFront.Builder.Tests.Features.Sessions;

/// <summary>
/// Tests for SPEC-210 role management: SetPresentersAsync and SetCoordinatorsAsync —
/// covering input validation, Teams sync behavior, and error paths.
/// </summary>
public class SessionRoleManagementTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SessionService _sut;
    private const string OwnerUserId = "role-mgmt-user";
    private const string OtherUserId = "other-user";
    private const string OboToken = "obo-token";
    private const string TenantId = "test-tenant-id";

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

    // ─── SetPresentersAsync — validation ────────────────────────────────────

    [Fact]
    public async Task SetPresentersAsync_NullPeople_ReturnsError()
    {
        var (_, session) = await SeedSessionAsync();
        var req = new SetPresentersRequest(null!);

        var (result, errorCode) = await _sut.SetPresentersAsync(
            session.SessionId, OwnerUserId, req,
            null, null, BuildConfig(), NullLogger.Instance);

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

        var (result, errorCode) = await _sut.SetPresentersAsync(
            session.SessionId, OwnerUserId, req,
            null, null, BuildConfig(), NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("duplicate_entra_user_id");
    }

    [Fact]
    public async Task SetPresentersAsync_SessionNotFound_ReturnsError()
    {
        var req = new SetPresentersRequest(new List<PersonInput>());

        var (result, errorCode) = await _sut.SetPresentersAsync(
            Guid.NewGuid(), OwnerUserId, req,
            null, null, BuildConfig(), NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    [Fact]
    public async Task SetPresentersAsync_WrongOwner_ReturnsError()
    {
        var (_, session) = await SeedSessionAsync();
        var req = new SetPresentersRequest(new List<PersonInput>());

        var (result, errorCode) = await _sut.SetPresentersAsync(
            session.SessionId, OtherUserId, req,
            null, null, BuildConfig(), NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    // ─── SetPresentersAsync — happy path (no Teams sync) ────────────────────

    [Fact]
    public async Task SetPresentersAsync_DraftSession_SavesLocallyNoTeamsSync()
    {
        var (_, session) = await SeedSessionAsync();
        var people = new List<PersonInput>
        {
            new("user-p1", "Presenter One", "p1@example.com"),
        };
        var req = new SetPresentersRequest(people);
        var graphMock = new Mock<ITeamsGraphClient>();

        var (result, errorCode) = await _sut.SetPresentersAsync(
            session.SessionId, OwnerUserId, req,
            OboToken, graphMock.Object, BuildConfig(), NullLogger.Instance);

        errorCode.Should().BeNull();
        result.Should().HaveCount(1);
        result![0].EntraUserId.Should().Be("user-p1");
        result[0].DisplayName.Should().Be("Presenter One");

        // No Teams calls expected for a Draft session
        graphMock.Verify(g => g.AddWebinarPresenterAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    // ─── SetPresentersAsync — published session Teams sync ──────────────────

    [Fact]
    public async Task SetPresentersAsync_PublishedSession_SyncsPresentersToTeams()
    {
        var (_, session) = await SeedPublishedSessionAsync();

        var people = new List<PersonInput>
        {
            new("user-p1", "Presenter One", "p1@example.com"),
            new("user-p2", "Presenter Two", "p2@example.com"),
        };
        var req = new SetPresentersRequest(people);

        var graphMock = new Mock<ITeamsGraphClient>();
        graphMock
            .Setup(g => g.GetWebinarPresentersAsync(session.TeamsWebinarId!, OboToken, default))
            .ReturnsAsync(new List<TeamsPresenterInfo>());

        var (result, errorCode) = await _sut.SetPresentersAsync(
            session.SessionId, OwnerUserId, req,
            OboToken, graphMock.Object, BuildConfig(), NullLogger.Instance);

        errorCode.Should().BeNull();
        result.Should().HaveCount(2);

        // Both presenters should have been added to Teams
        graphMock.Verify(g => g.AddWebinarPresenterAsync(
            session.TeamsWebinarId!, "user-p1", TenantId, OboToken, default), Times.Once);
        graphMock.Verify(g => g.AddWebinarPresenterAsync(
            session.TeamsWebinarId!, "user-p2", TenantId, OboToken, default), Times.Once);
    }

    [Fact]
    public async Task SetPresentersAsync_TeamsFailure_LogsWarning_LocalChangesSaved()
    {
        var (_, session) = await SeedPublishedSessionAsync();
        var people = new List<PersonInput> { new("user-p1", "Presenter One", "p1@example.com") };
        var req = new SetPresentersRequest(people);

        var graphMock = new Mock<ITeamsGraphClient>();
        graphMock
            .Setup(g => g.GetWebinarPresentersAsync(session.TeamsWebinarId!, OboToken, default))
            .ThrowsAsync(new InvalidOperationException("Graph error"));

        var (result, errorCode) = await _sut.SetPresentersAsync(
            session.SessionId, OwnerUserId, req,
            OboToken, graphMock.Object, BuildConfig(), NullLogger.Instance);

        // Local changes should still be saved
        errorCode.Should().BeNull();
        result.Should().HaveCount(1);

        var saved = await _db.SessionPresenters
            .Where(p => p.SessionId == session.SessionId)
            .ToListAsync();
        saved.Should().HaveCount(1);
        saved[0].EntraUserId.Should().Be("user-p1");
    }

    // ─── SetCoordinatorsAsync — validation ──────────────────────────────────

    [Fact]
    public async Task SetCoordinatorsAsync_NullPeople_ReturnsError()
    {
        var (_, session) = await SeedSessionAsync();
        var req = new SetCoordinatorsRequest(null!);

        var (result, errorCode) = await _sut.SetCoordinatorsAsync(
            session.SessionId, OwnerUserId, req,
            null, null, NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("people_required");
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

        var (result, errorCode) = await _sut.SetCoordinatorsAsync(
            session.SessionId, OwnerUserId, req,
            null, null, NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("duplicate_entra_user_id");
    }

    [Fact]
    public async Task SetCoordinatorsAsync_SessionNotFound_ReturnsError()
    {
        var req = new SetCoordinatorsRequest(new List<PersonInput>());

        var (result, errorCode) = await _sut.SetCoordinatorsAsync(
            Guid.NewGuid(), OwnerUserId, req,
            null, null, NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    [Fact]
    public async Task SetCoordinatorsAsync_WrongOwner_ReturnsError()
    {
        var (_, session) = await SeedSessionAsync();
        var req = new SetCoordinatorsRequest(new List<PersonInput>());

        var (result, errorCode) = await _sut.SetCoordinatorsAsync(
            session.SessionId, OtherUserId, req,
            null, null, NullLogger.Instance);

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    // ─── SetCoordinatorsAsync — happy path ──────────────────────────────────

    [Fact]
    public async Task SetCoordinatorsAsync_DraftSession_SavesLocallyNoTeamsSync()
    {
        var (_, session) = await SeedSessionAsync();
        var people = new List<PersonInput>
        {
            new("user-c1", "Coordinator One", "c1@example.com"),
        };
        var req = new SetCoordinatorsRequest(people);
        var graphMock = new Mock<ITeamsGraphClient>();

        var (result, errorCode) = await _sut.SetCoordinatorsAsync(
            session.SessionId, OwnerUserId, req,
            OboToken, graphMock.Object, NullLogger.Instance);

        errorCode.Should().BeNull();
        result.Should().HaveCount(1);
        result![0].EntraUserId.Should().Be("user-c1");

        // No Teams calls expected for a Draft session
        graphMock.Verify(g => g.SetWebinarCoOrganizersAsync(
            It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task SetCoordinatorsAsync_PublishedSession_SyncsCoOrganizersToTeams()
    {
        var (_, session) = await SeedPublishedSessionAsync();
        var people = new List<PersonInput>
        {
            new("user-c1", "Coordinator One", "c1@example.com"),
        };
        var req = new SetCoordinatorsRequest(people);

        var graphMock = new Mock<ITeamsGraphClient>();

        var (result, errorCode) = await _sut.SetCoordinatorsAsync(
            session.SessionId, OwnerUserId, req,
            OboToken, graphMock.Object, NullLogger.Instance);

        errorCode.Should().BeNull();
        result.Should().HaveCount(1);

        graphMock.Verify(g => g.SetWebinarCoOrganizersAsync(
            session.TeamsWebinarId!,
            It.Is<IEnumerable<string>>(ids => ids.Single() == "user-c1"),
            OboToken, default), Times.Once);
    }

    [Fact]
    public async Task SetCoordinatorsAsync_TeamsFailure_LogsWarning_LocalChangesSaved()
    {
        var (_, session) = await SeedPublishedSessionAsync();
        var people = new List<PersonInput> { new("user-c1", "Coordinator One", "c1@example.com") };
        var req = new SetCoordinatorsRequest(people);

        var graphMock = new Mock<ITeamsGraphClient>();
        graphMock
            .Setup(g => g.SetWebinarCoOrganizersAsync(session.TeamsWebinarId!, It.IsAny<IEnumerable<string>>(), OboToken, default))
            .ThrowsAsync(new InvalidOperationException("Graph error"));

        var (result, errorCode) = await _sut.SetCoordinatorsAsync(
            session.SessionId, OwnerUserId, req,
            OboToken, graphMock.Object, NullLogger.Instance);

        // Local changes should still be saved
        errorCode.Should().BeNull();
        result.Should().HaveCount(1);

        var saved = await _db.SessionCoordinators
            .Where(c => c.SessionId == session.SessionId)
            .ToListAsync();
        saved.Should().HaveCount(1);
        saved[0].EntraUserId.Should().Be("user-c1");
    }

    // ─── SetPresentersAsync — replaces existing presenters ──────────────────

    [Fact]
    public async Task SetPresentersAsync_ReplacesExistingPresenters()
    {
        var (_, session) = await SeedSessionAsync();

        // Seed existing presenter
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

        // Replace with a new set
        var req = new SetPresentersRequest(new List<PersonInput>
        {
            new("new-user", "New User", "new@example.com"),
        });

        var (result, errorCode) = await _sut.SetPresentersAsync(
            session.SessionId, OwnerUserId, req,
            null, null, BuildConfig(), NullLogger.Instance);

        errorCode.Should().BeNull();
        result.Should().HaveCount(1);
        result![0].EntraUserId.Should().Be("new-user");

        var saved = await _db.SessionPresenters
            .Where(p => p.SessionId == session.SessionId)
            .ToListAsync();
        saved.Should().HaveCount(1, "old presenters should be removed");
        saved[0].EntraUserId.Should().Be("new-user");
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session)> SeedSessionAsync(
        SessionStatus status = SessionStatus.Draft)
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
            Status = status,
            DriftStatus = DriftStatus.None,
            ReconcileStatus = ReconcileStatus.Synced
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return (series, session);
    }

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session)> SeedPublishedSessionAsync()
    {
        var (series, session) = await SeedSessionAsync();
        session.Status = SessionStatus.Published;
        session.TeamsWebinarId = "webinar-test-id";
        session.JoinWebUrl = "https://teams.microsoft.com/l/meetup-join/test";
        await _db.SaveChangesAsync();
        return (series, session);
    }

    private static IConfiguration BuildConfig()
    {
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["AzureAd:TenantId"]).Returns(TenantId);
        return configMock.Object;
    }
}
