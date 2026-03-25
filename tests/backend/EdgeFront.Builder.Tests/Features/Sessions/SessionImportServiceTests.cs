using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace EdgeFront.Builder.Tests.Features.Sessions;

/// <summary>
/// Tests for SessionImportService — CSV-based registration and attendance import
/// with email normalisation, idempotency, and metrics recomputation.
/// </summary>
public class SessionImportServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SessionImportService _sut;
    private readonly MetricsRecomputeService _metricsRecompute;
    private const string OwnerUserId = "import-user-oid";

    public SessionImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);

        var internalDomains = new InternalDomainFilter(new[] { "internal.com" });
        var warmEvaluator = new WarmRuleEvaluator(internalDomains);
        _metricsRecompute = new MetricsRecomputeService(_db, internalDomains, warmEvaluator);

        _sut = new SessionImportService(
            _db, _metricsRecompute,
            NullLogger<SessionImportService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ─── Registration Import Tests ─────────────────────────────────────────

    [Fact]
    public async Task Import_Registrations_ValidCsv_ImportsRows()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Email\nalice@example.com\nbob@example.com\ncharlie@test.org";
        var (result, errorCode) = await _sut.ImportRegistrationsAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(3);
        result.TotalRows.Should().Be(3);
        result.SkippedCount.Should().Be(0);
        result.InvalidCount.Should().Be(0);
        result.Errors.Should().BeEmpty();

        var regs = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId).ToListAsync();
        regs.Should().HaveCount(3);
        regs.Select(r => r.Email).Should().BeEquivalentTo(
            "alice@example.com", "bob@example.com", "charlie@test.org");
        regs.Select(r => r.EmailDomain).Should().BeEquivalentTo(
            "example.com", "example.com", "test.org");
    }

    [Fact]
    public async Task Import_Registrations_SessionNotFound_ReturnsError()
    {
        var (result, errorCode) = await _sut.ImportRegistrationsAsync(
            Guid.NewGuid(), OwnerUserId, CsvStream("Email\nalice@test.com"));

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    [Fact]
    public async Task Import_Registrations_WrongOwner_ReturnsError()
    {
        var (_, session) = await SeedSessionAsync();

        var (result, errorCode) = await _sut.ImportRegistrationsAsync(
            session.SessionId, "user-wrong-owner", CsvStream("Email\nalice@test.com"));

        result.Should().BeNull();
        errorCode.Should().Be("session_not_found");
    }

    [Fact]
    public async Task Import_Registrations_MissingEmailColumn_ReturnsError()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Name,Date\nAlice,2024-01-01";
        var (result, errorCode) = await _sut.ImportRegistrationsAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        result.Should().BeNull();
        errorCode.Should().Be("missing_required_column_email");
    }

    [Fact]
    public async Task Import_Registrations_InvalidEmail_ReportsRowError()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Email\nalice@example.com\nbademail-no-at";
        var (result, errorCode) = await _sut.ImportRegistrationsAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(1);
        result.InvalidCount.Should().Be(1);
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<RowErrorDto>(e =>
                e.Row == 3 && e.Reason.Contains("Invalid email"));

        var regs = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId).ToListAsync();
        regs.Should().HaveCount(1);
        regs[0].Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Import_Registrations_DuplicateReplay_IsIdempotent()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Email\nalice@example.com\nbob@example.com\ncharlie@test.org";

        // First import
        var (result1, error1) = await _sut.ImportRegistrationsAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));
        error1.Should().BeNull();
        result1!.ImportedCount.Should().Be(3);
        result1.SkippedCount.Should().Be(0);

        // Second import (same data)
        var (result2, error2) = await _sut.ImportRegistrationsAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));
        error2.Should().BeNull();
        result2!.ImportedCount.Should().Be(0);
        result2.SkippedCount.Should().Be(3);

        // DB still has exactly 3 rows
        var regs = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId).ToListAsync();
        regs.Should().HaveCount(3);
    }

    [Fact]
    public async Task Import_Registrations_EmailNormalization()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Email\n USER@Example.COM ";
        var (result, errorCode) = await _sut.ImportRegistrationsAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(1);

        var reg = await _db.NormalizedRegistrations
            .SingleAsync(r => r.SessionId == session.SessionId);
        reg.Email.Should().Be("user@example.com");
        reg.EmailDomain.Should().Be("example.com");
    }

    [Fact]
    public async Task Import_Registrations_MetricsRecomputed()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Email\nalice@example.com\nbob@test.org";
        await _sut.ImportRegistrationsAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        var metrics = await _db.SessionMetrics.FindAsync(session.SessionId);
        metrics.Should().NotBeNull();
        metrics!.TotalRegistrations.Should().Be(2);
    }

    [Fact]
    public async Task Import_Registrations_EmptyFile_ReturnsZeroCounts()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "";
        var (result, errorCode) = await _sut.ImportRegistrationsAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.TotalRows.Should().Be(0);
        result.ImportedCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.InvalidCount.Should().Be(0);
    }

    [Fact]
    public async Task Import_Registrations_PartialSuccess()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Email\nalice@example.com\ninvalid-no-at\nbob@test.org\nalso-bad";
        var (result, errorCode) = await _sut.ImportRegistrationsAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.TotalRows.Should().Be(4);
        result.ImportedCount.Should().Be(2);
        result.InvalidCount.Should().Be(2);
        result.Errors.Should().HaveCount(2);
        result.Errors.Select(e => e.Row).Should().BeEquivalentTo(new[] { 3, 5 });

        var regs = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId).ToListAsync();
        regs.Should().HaveCount(2);
    }

    // ─── Attendance Import Tests ───────────────────────────────────────────

    [Fact]
    public async Task Import_Attendance_ValidCsv_ImportsRows()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Email,Attended\nalice@example.com,true\nbob@test.org,false";
        var (result, errorCode) = await _sut.ImportAttendanceAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(2);
        result.TotalRows.Should().Be(2);
        result.SkippedCount.Should().Be(0);
        result.InvalidCount.Should().Be(0);

        var atts = await _db.NormalizedAttendances
            .Where(a => a.SessionId == session.SessionId).ToListAsync();
        atts.Should().HaveCount(2);

        var alice = atts.Single(a => a.Email == "alice@example.com");
        alice.Attended.Should().BeTrue();
        alice.EmailDomain.Should().Be("example.com");

        var bob = atts.Single(a => a.Email == "bob@test.org");
        bob.Attended.Should().BeFalse();
    }

    [Fact]
    public async Task Import_Attendance_DefaultAttendedTrue()
    {
        var (_, session) = await SeedSessionAsync();

        // CSV with only Email column — no Attended column
        var csv = "Email\nalice@example.com\nbob@test.org";
        var (result, errorCode) = await _sut.ImportAttendanceAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(2);

        var atts = await _db.NormalizedAttendances
            .Where(a => a.SessionId == session.SessionId).ToListAsync();
        atts.Should().HaveCount(2);
        atts.Should().AllSatisfy(a => a.Attended.Should().BeTrue());
    }

    [Fact]
    public async Task Import_Attendance_DuplicateReplay_IsIdempotent()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Email,Attended\nalice@example.com,true\nbob@test.org,true";

        // First import
        var (result1, error1) = await _sut.ImportAttendanceAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));
        error1.Should().BeNull();
        result1!.ImportedCount.Should().Be(2);
        result1.SkippedCount.Should().Be(0);

        // Second import (same data)
        var (result2, error2) = await _sut.ImportAttendanceAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));
        error2.Should().BeNull();
        result2!.ImportedCount.Should().Be(0);
        result2.SkippedCount.Should().Be(2);

        // DB still has exactly 2 rows
        var atts = await _db.NormalizedAttendances
            .Where(a => a.SessionId == session.SessionId).ToListAsync();
        atts.Should().HaveCount(2);
    }

    [Fact]
    public async Task Import_Attendance_InvalidEmail_ReportsError()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Email,Attended\nalice@example.com,true\nno-at-sign,true";
        var (result, errorCode) = await _sut.ImportAttendanceAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(1);
        result.InvalidCount.Should().Be(1);
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<RowErrorDto>(e =>
                e.Row == 3 && e.Reason.Contains("Invalid email"));
    }

    [Fact]
    public async Task Import_Attendance_MetricsAndWarmAccounts()
    {
        var (_, session) = await SeedSessionAsync();

        // Two attendees from the same external domain → W1 warm account triggered
        var csv = "Email,Attended\nalice@warmcorp.com,true\nbob@warmcorp.com,true";
        await _sut.ImportAttendanceAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        var metrics = await _db.SessionMetrics.FindAsync(session.SessionId);
        metrics.Should().NotBeNull();
        metrics!.TotalAttendees.Should().Be(2);
        metrics.WarmAccountsTriggered.Should().Contain("warmcorp.com");
    }

    [Fact]
    public async Task Import_Attendance_InternalDomainExcludedFromWarmAccounts()
    {
        var (_, session) = await SeedSessionAsync();

        // Two attendees from internal.com (configured as internal domain) → should NOT trigger W1
        var csv = "Email,Attended\nalice@internal.com,true\nbob@internal.com,true";
        await _sut.ImportAttendanceAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        var metrics = await _db.SessionMetrics.FindAsync(session.SessionId);
        metrics.Should().NotBeNull();
        metrics!.TotalAttendees.Should().Be(2);
        metrics.WarmAccountsTriggered.Should().NotContain("internal.com");
    }

    // ─── Mixed / Edge Cases ────────────────────────────────────────────────

    [Fact]
    public async Task Import_Registrations_WithOptionalRegisteredAt()
    {
        var (_, session) = await SeedSessionAsync();

        var csv = "Email,RegisteredAt\nalice@example.com,2024-06-15T10:30:00Z";
        var (result, errorCode) = await _sut.ImportRegistrationsAsync(
            session.SessionId, OwnerUserId, CsvStream(csv));

        errorCode.Should().BeNull();
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(1);

        var reg = await _db.NormalizedRegistrations
            .SingleAsync(r => r.SessionId == session.SessionId);
        reg.RegisteredAt.Should().Be(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    private async Task<(EdgeFront.Builder.Domain.Entities.Series, Session)> SeedSessionAsync()
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Import Test Series",
            Status = SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Series.Add(series);

        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = series.SeriesId,
            OwnerUserId = OwnerUserId,
            Title = "Import Test Session",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1),
            Status = SessionStatus.Draft
        };
        _db.Sessions.Add(session);

        await _db.SaveChangesAsync();
        return (series, session);
    }

    private static Stream CsvStream(string csv) =>
        new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
}
