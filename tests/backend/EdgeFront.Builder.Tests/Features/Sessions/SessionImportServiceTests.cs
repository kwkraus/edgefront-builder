using System.Text;
using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EdgeFront.Builder.Tests.Features.Sessions;

public class SessionImportServiceTests : IDisposable
{
    private const string OwnerUserId = "import-user";
    private readonly AppDbContext _db;
    private readonly SessionImportService _sut;

    public SessionImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        var filter = new InternalDomainFilter(["acme.com"]);
        var warmEvaluator = new WarmRuleEvaluator(filter);
        var recompute = new MetricsRecomputeService(_db, filter, warmEvaluator);
        _sut = new SessionImportService(_db, recompute);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ReplaceRegistrationsAsync_PersistsRows_Summary_AndMetrics()
    {
        var (_, session) = await SeedSessionAsync();
        var file = CreateCsvFile(
            "registrations.csv",
            """
            Email,RegisteredAt
            alice@external.com,2025-01-01T10:00:00Z
            alice@external.com,2025-01-01T11:00:00Z
            bob@acme.com,2025-01-02T12:00:00Z
            """);

        var outcome = await _sut.ReplaceRegistrationsAsync(session.SessionId, OwnerUserId, file);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Summary.Should().NotBeNull();
        outcome.Summary!.ImportType.Should().Be(SessionImportType.Registrations.ToString());
        outcome.Summary.RowCount.Should().Be(2);

        var registrations = await _db.NormalizedRegistrations
            .Where(x => x.SessionId == session.SessionId)
            .OrderBy(x => x.Email)
            .ToListAsync();

        registrations.Should().HaveCount(2);
        registrations[0].Email.Should().Be("alice@external.com");
        registrations[0].RegisteredAt.Should().Be(DateTime.Parse("2025-01-01T10:00:00Z").ToUniversalTime());
        registrations[1].Email.Should().Be("bob@acme.com");

        var summary = await _db.SessionImportSummaries
            .SingleAsync(x => x.SessionId == session.SessionId && x.ImportType == SessionImportType.Registrations);
        summary.FileName.Should().Be("registrations.csv");
        summary.RowCount.Should().Be(2);

        var sessionMetrics = await _db.SessionMetrics.FindAsync(session.SessionId);
        sessionMetrics.Should().NotBeNull();
        sessionMetrics!.TotalRegistrations.Should().Be(2);
        sessionMetrics.UniqueRegistrantAccountDomains.Should().Be(1);

        var seriesMetrics = await _db.SeriesMetrics.FindAsync(session.SeriesId);
        seriesMetrics.Should().NotBeNull();
        seriesMetrics!.TotalRegistrations.Should().Be(2);
        seriesMetrics.UniqueRegistrantAccountDomains.Should().Be(1);
    }

    [Fact]
    public async Task ReplaceQaAsync_PersistsRows_Summary_AndQaMetrics()
    {
        var (_, session) = await SeedSessionAsync();
        var file = CreateCsvFile(
            "qa.csv",
            """
            Question,Answer,AskedByEmail,AskedAt,AnsweredAt
            What is EdgeFront?,A webinar builder,alex@external.com,2025-01-01T10:00:00Z,2025-01-01T10:05:00Z
            Do you support imports?,,jamie@external.com,2025-01-01T10:10:00Z,
            """);

        var outcome = await _sut.ReplaceQaAsync(session.SessionId, OwnerUserId, file);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Summary.Should().NotBeNull();
        outcome.Summary!.ImportType.Should().Be(SessionImportType.Qa.ToString());
        outcome.Summary.RowCount.Should().Be(2);

        var qaEntries = await _db.NormalizedQaEntries
            .Where(x => x.SessionId == session.SessionId)
            .OrderBy(x => x.QuestionText)
            .ToListAsync();

        qaEntries.Should().HaveCount(2);
        qaEntries.Count(x => x.IsAnswered).Should().Be(1);
        qaEntries.Select(x => x.AskedByEmail).Should().Contain(["alex@external.com", "jamie@external.com"]);

        var sessionMetrics = await _db.SessionMetrics.FindAsync(session.SessionId);
        sessionMetrics.Should().NotBeNull();
        sessionMetrics!.TotalQaQuestions.Should().Be(2);
        sessionMetrics.AnsweredQaQuestions.Should().Be(1);

        var seriesMetrics = await _db.SeriesMetrics.FindAsync(session.SeriesId);
        seriesMetrics.Should().NotBeNull();
        seriesMetrics!.TotalQaQuestions.Should().Be(2);
        seriesMetrics.AnsweredQaQuestions.Should().Be(1);
    }

    private async Task<(EdgeFront.Builder.Domain.Entities.Series series, Session session)> SeedSessionAsync()
    {
        var series = new EdgeFront.Builder.Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = OwnerUserId,
            Title = "Import Series",
            Status = SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = series.SeriesId,
            OwnerUserId = OwnerUserId,
            Title = "Import Session",
            StartsAt = DateTime.UtcNow.AddDays(7),
            EndsAt = DateTime.UtcNow.AddDays(7).AddHours(1),
            Status = SessionStatus.Draft,
            DriftStatus = DriftStatus.None,
            ReconcileStatus = ReconcileStatus.Synced
        };

        _db.Series.Add(series);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return (series, session);
    }

    private static IFormFile CreateCsvFile(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content.Replace("\r", string.Empty));
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }
}
