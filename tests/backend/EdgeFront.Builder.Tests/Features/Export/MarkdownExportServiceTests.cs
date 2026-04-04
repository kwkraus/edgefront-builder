using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Export;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Tests.Features.Export;

public class MarkdownExportServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly MarkdownExportService _sut;

    private const string OwnerUserId = "user-oid-export-123";
    private const string OtherUserId = "user-oid-other-456";

    public MarkdownExportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .Options;

        _db = new AppDbContext(options);
        _sut = new MarkdownExportService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── 1. Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportSeriesAsync_ReturnsSeries_WhenOwnerMatches()
    {
        // Arrange
        var series = BuildSeries("My Webinar Series");
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.ExportSeriesAsync(series.SeriesId, OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().NotBeNullOrWhiteSpace();
        result.Content.Should().NotBeNullOrWhiteSpace();
    }

    // ── 2. Series not found ────────────────────────────────────────────────────

    [Fact]
    public async Task ExportSeriesAsync_ReturnsNull_WhenSeriesNotFound()
    {
        // Act — random ID that has no row
        var result = await _sut.ExportSeriesAsync(Guid.NewGuid(), OwnerUserId);

        // Assert
        result.Should().BeNull();
    }

    // ── 3. Wrong owner ────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportSeriesAsync_ReturnsNull_WhenOwnerDoesNotMatch()
    {
        // Arrange — series owned by a different user
        var series = BuildSeries("Series By Other User", ownerOverride: OtherUserId);
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        // Act — request made by the primary owner
        var result = await _sut.ExportSeriesAsync(series.SeriesId, OwnerUserId);

        // Assert
        result.Should().BeNull();
    }

    // ── 4. Sessions ordered by StartsAt ───────────────────────────────────────

    [Fact]
    public async Task ExportSeriesAsync_IncludesAllSessions_OrderedByStartsAt()
    {
        // Arrange — add sessions in reverse order so ordering is validated
        var series = BuildSeries("Ordered Series");
        _db.Series.Add(series);

        var baseTime = new DateTime(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc);

        var sessionC = BuildSession(series.SeriesId, "Session C", baseTime.AddDays(2));
        var sessionA = BuildSession(series.SeriesId, "Session A", baseTime.AddDays(0));
        var sessionB = BuildSession(series.SeriesId, "Session B", baseTime.AddDays(1));

        _db.Sessions.AddRange(sessionC, sessionA, sessionB);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.ExportSeriesAsync(series.SeriesId, OwnerUserId);

        // Assert — all three titles present
        result.Should().NotBeNull();
        result!.Content.Should().Contain("Session A");
        result.Content.Should().Contain("Session B");
        result.Content.Should().Contain("Session C");

        // Assert order: A < B < C
        var posA = result.Content.IndexOf("Session A", StringComparison.Ordinal);
        var posB = result.Content.IndexOf("Session B", StringComparison.Ordinal);
        var posC = result.Content.IndexOf("Session C", StringComparison.Ordinal);
        posA.Should().BeLessThan(posB, "Session A (earliest) should appear before Session B");
        posB.Should().BeLessThan(posC, "Session B should appear before Session C (latest)");
    }

    // ── 5. Zero sessions ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExportSeriesAsync_HandlesZeroSessions()
    {
        // Arrange — series with no sessions
        var series = BuildSeries("Empty Series");
        _db.Series.Add(series);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.ExportSeriesAsync(series.SeriesId, OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("No sessions are currently defined");
    }

    // ── 6. No presenters — line omitted ───────────────────────────────────────

    [Fact]
    public async Task ExportSeriesAsync_OmitsPresenterLine_WhenNoPresenters()
    {
        // Arrange — session with no presenters
        var series = BuildSeries("Presenter-free Series");
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId, "Solo Session");
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.ExportSeriesAsync(series.SeriesId, OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().NotContain("**Presenters:**");
    }

    // ── 7. No coordinators — line omitted ─────────────────────────────────────

    [Fact]
    public async Task ExportSeriesAsync_OmitsCoordinatorLine_WhenNoCoordinators()
    {
        // Arrange — session with no coordinators
        var series = BuildSeries("Coordinator-free Series");
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId, "Solo Session");
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.ExportSeriesAsync(series.SeriesId, OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().NotContain("**Coordinators:**");
    }

    // ── 8. Presenter display names appear in content ───────────────────────────

    [Fact]
    public async Task ExportSeriesAsync_IncludesPresenterDisplayNames()
    {
        // Arrange
        var series = BuildSeries("Presenter Series");
        _db.Series.Add(series);
        var session = BuildSession(series.SeriesId, "Presented Session");
        _db.Sessions.Add(session);

        _db.SessionPresenters.AddRange(
            new SessionPresenter
            {
                SessionPresenterId = Guid.NewGuid(),
                SessionId = session.SessionId,
                EntraUserId = "entra-p1",
                DisplayName = "Alice Smith",
                Email = "alice@example.com",
                CreatedAt = DateTime.UtcNow
            },
            new SessionPresenter
            {
                SessionPresenterId = Guid.NewGuid(),
                SessionId = session.SessionId,
                EntraUserId = "entra-p2",
                DisplayName = "Bob Jones",
                Email = "bob@example.com",
                CreatedAt = DateTime.UtcNow
            });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.ExportSeriesAsync(series.SeriesId, OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("Alice Smith");
        result.Content.Should().Contain("Bob Jones");
    }

    // ── 9. Sensitive fields excluded ──────────────────────────────────────────

    [Fact]
    public async Task ExportSeriesAsync_ExcludesSensitiveFields()
    {
        // Arrange — session populated with every sensitive field
        const string sensitiveWebinarId = "SENSITIVE-WEBINAR-ID-99";
        const string sensitiveJoinUrl    = "https://teams.microsoft.com/l/secret-url";
        const string sensitiveEmail      = "secret.user@internal-corp.com";
        const string sensitiveLastError  = "Token refresh failed: secret_context_xyz";

        var series = BuildSeries("Sensitive Data Series");
        _db.Series.Add(series);

        var session = BuildSession(series.SeriesId, "Sensitive Session");
        session.TeamsWebinarId = sensitiveWebinarId;
        session.JoinWebUrl     = sensitiveJoinUrl;
        session.LastError      = sensitiveLastError;
        _db.Sessions.Add(session);

        // Add a presenter with a sensitive email — only DisplayName should appear.
        _db.SessionPresenters.Add(new SessionPresenter
        {
            SessionPresenterId = Guid.NewGuid(),
            SessionId          = session.SessionId,
            EntraUserId        = "entra-secret",
            DisplayName        = "Visible Name Only",
            Email              = sensitiveEmail,
            CreatedAt          = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.ExportSeriesAsync(series.SeriesId, OwnerUserId);

        // Assert — none of the sensitive values must appear anywhere in the markdown
        result.Should().NotBeNull();
        result!.Content.Should().NotContain(sensitiveWebinarId,
            because: "TeamsWebinarId must never appear in the exported markdown");
        result.Content.Should().NotContain(sensitiveJoinUrl,
            because: "JoinWebUrl must never appear in the exported markdown");
        result.Content.Should().NotContain(sensitiveEmail,
            because: "presenter Email must never appear in the exported markdown");
        result.Content.Should().NotContain(sensitiveLastError,
            because: "LastError must never appear in the exported markdown");

        // Sanity-check: the non-sensitive DisplayName IS present.
        result.Content.Should().Contain("Visible Name Only");
    }

    // ── 10. Session with missing schedule ─────────────────────────────────────

    [Fact]
    public async Task ExportSeriesAsync_HandlesSessionWithMissingSchedule()
    {
        // Arrange — session with default (unset) StartsAt
        var series = BuildSeries("Unscheduled Series");
        _db.Series.Add(series);

        // Use DateTimeKind.Utc so AppDbContext's UTC value-converter does NOT
        // call ToUniversalTime() on the minimum value (which would apply the
        // local-timezone offset and change the Ticks, defeating the == default check).
        // Specifying Utc keeps Ticks == 0, which is what the service checks for.
        var unset = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

        var session = new Session
        {
            SessionId    = Guid.NewGuid(),
            SeriesId     = series.SeriesId,
            OwnerUserId  = OwnerUserId,
            Title        = "Unscheduled Session",
            StartsAt     = unset,
            EndsAt       = unset,
            Status       = SessionStatus.Draft,
            DriftStatus  = DriftStatus.None,
            ReconcileStatus = ReconcileStatus.Synced
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // Act — must not throw
        var act = async () => await _sut.ExportSeriesAsync(series.SeriesId, OwnerUserId);

        // Assert — completes without exception and includes the session title
        await act.Should().NotThrowAsync();
        var result = await _sut.ExportSeriesAsync(series.SeriesId, OwnerUserId);
        result.Should().NotBeNull();
        result!.Content.Should().Contain("Unscheduled Session");
        result.Content.Should().Contain("Not yet set",
            because: "sessions without a schedule should show 'Not yet set' instead of a date");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private EdgeFront.Builder.Domain.Entities.Series BuildSeries(
        string title,
        string? ownerOverride = null) =>
        new()
        {
            SeriesId    = Guid.NewGuid(),
            OwnerUserId = ownerOverride ?? OwnerUserId,
            Title       = title,
            Status      = SeriesStatus.Draft,
            CreatedAt   = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt   = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };

    private static Session BuildSession(
        Guid seriesId,
        string title = "Test Session",
        DateTime? startsAt = null)
    {
        var start = startsAt ?? new DateTime(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc);
        return new Session
        {
            SessionId       = Guid.NewGuid(),
            SeriesId        = seriesId,
            OwnerUserId     = OwnerUserId,
            Title           = title,
            StartsAt        = start,
            EndsAt          = start.AddHours(1),
            Status          = SessionStatus.Draft,
            DriftStatus     = DriftStatus.None,
            ReconcileStatus = ReconcileStatus.Synced
        };
    }
}
