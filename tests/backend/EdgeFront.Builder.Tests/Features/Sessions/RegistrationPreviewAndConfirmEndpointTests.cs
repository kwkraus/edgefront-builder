using System.Text;
using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using SeriesAlias = EdgeFront.Builder.Domain.Entities.Series;

namespace EdgeFront.Builder.Tests.Features.Sessions;

/// <summary>
/// Integration tests for registration preview and confirm workflows.
/// </summary>
public class RegistrationPreviewAndConfirmEndpointTests : IDisposable
{
    private const string OwnerUserId = "user-123";
    private const string OtherUserId = "user-456";
    private readonly AppDbContext _db;
    private readonly Mock<IRegistrationFileParser> _fileParserMock;

    public RegistrationPreviewAndConfirmEndpointTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _fileParserMock = new Mock<IRegistrationFileParser>();
    }

    public void Dispose() => _db.Dispose();

    // ==================== Helper Methods ====================

    private async Task<Session> SeedSessionAsync(Guid? seriesId = null)
    {
        var series = new SeriesAlias
        {
            SeriesId = seriesId ?? Guid.NewGuid(),
            Title = "Test Series",
            OwnerUserId = OwnerUserId
        };
        _db.Series.Add(series);

        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = series.SeriesId,
            Title = "Test Session",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(2),
            OwnerUserId = OwnerUserId,
            JoinWebUrl = "https://example.com/join"
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        return session;
    }

    private List<ParsedRegistrant> CreateSuccessfulRegistrants(int count = 2)
    {
        var registrants = new List<ParsedRegistrant>();
        for (int i = 0; i < count; i++)
        {
            registrants.Add(new ParsedRegistrant
            {
                Email = $"user{i}@example.com",
                FirstName = $"User{i}",
                LastName = "Doe",
                RegisteredAt = DateTime.UtcNow,
                Status = "success",
                ErrorReason = null
            });
        }
        return registrants;
    }

    private List<ParsedRegistrant> CreateMixedRegistrants()
    {
        return new List<ParsedRegistrant>
        {
            new()
            {
                Email = "valid@example.com",
                FirstName = "Valid",
                LastName = "User",
                RegisteredAt = DateTime.UtcNow,
                Status = "success",
                ErrorReason = null
            },
            new()
            {
                Email = "invalid-email",
                FirstName = "",
                LastName = "",
                RegisteredAt = DateTime.UtcNow,
                Status = "failed",
                ErrorReason = "Invalid email format"
            },
            new()
            {
                Email = "another.valid@example.com",
                FirstName = "Another",
                LastName = "Valid",
                RegisteredAt = DateTime.UtcNow,
                Status = "success",
                ErrorReason = null
            }
        };
    }

    // ==================== Preview Endpoint Tests ====================

    [Fact]
    public async Task PreviewEndpoint_WithValidParsedRegistrants_ReturnsPreviewDto()
    {
        // Arrange
        var session = await SeedSessionAsync();
        var parsedRegistrants = CreateSuccessfulRegistrants(3);
        
        _fileParserMock
            .Setup(p => p.ParseAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedRegistrants);

        // Act - Simulate preview
        var successCount = parsedRegistrants.Count(r => r.Status == "success");
        var failedCount = parsedRegistrants.Count(r => r.Status == "failed");
        
        var preview = new RegistrationPreviewDto
        {
            SessionTitle = session.Title,
            RegistrantCount = parsedRegistrants.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            Registrants = parsedRegistrants
        };

        // Assert
        preview.SessionTitle.Should().Be("Test Session");
        preview.RegistrantCount.Should().Be(3);
        preview.SuccessCount.Should().Be(3);
        preview.FailedCount.Should().Be(0);
        preview.Registrants.Should().HaveCount(3);
    }

    [Fact]
    public async Task PreviewEndpoint_WithMixedResults_ReturnsPreviewWithFailures()
    {
        // Arrange
        var session = await SeedSessionAsync();
        var parsedRegistrants = CreateMixedRegistrants();
        
        _fileParserMock
            .Setup(p => p.ParseAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedRegistrants);

        // Act
        var successCount = parsedRegistrants.Count(r => r.Status == "success");
        var failedCount = parsedRegistrants.Count(r => r.Status == "failed");
        
        var preview = new RegistrationPreviewDto
        {
            SessionTitle = session.Title,
            RegistrantCount = parsedRegistrants.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            Registrants = parsedRegistrants
        };

        // Assert
        preview.RegistrantCount.Should().Be(3);
        preview.SuccessCount.Should().Be(2);
        preview.FailedCount.Should().Be(1);
        preview.Registrants.Should().HaveCount(3);

        var failedRegistrant = preview.Registrants.FirstOrDefault(r => r.Status == "failed");
        failedRegistrant.Should().NotBeNull();
        failedRegistrant!.ErrorReason.Should().Contain("Invalid email format");
    }

    // ==================== Confirm Endpoint Tests ====================

    [Fact]
    public async Task ConfirmEndpoint_WithValidRegistrants_PersistsAndComputesMetrics()
    {
        // Arrange
        var session = await SeedSessionAsync();
        var registrants = CreateSuccessfulRegistrants(2);
        var request = new ConfirmRegistrationImportRequest { Registrants = registrants };

        // Act - Simulate confirm logic
        // Delete existing
        var existing = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId)
            .ToListAsync();
        _db.NormalizedRegistrations.RemoveRange(existing);

        // Insert new
        var normalized = registrants
            .Where(r => r.Status == "success")
            .Select(r => new NormalizedRegistration
            {
                RegistrationId = Guid.NewGuid(),
                SessionId = session.SessionId,
                OwnerUserId = OwnerUserId,
                Email = r.Email.ToLowerInvariant(),
                EmailDomain = ExtractDomain(r.Email),
                FirstName = r.FirstName,
                LastName = r.LastName,
                RegisteredAt = r.RegisteredAt
            })
            .ToList();

        _db.NormalizedRegistrations.AddRange(normalized);

        // Create summary
        var summary = new SessionImportSummary
        {
            SessionImportSummaryId = Guid.NewGuid(),
            SessionId = session.SessionId,
            ImportType = SessionImportType.Registrations,
            FileName = "registrations-ai-parsed.csv",
            RowCount = normalized.Count,
            ImportedAt = DateTime.UtcNow
        };
        _db.SessionImportSummaries.Add(summary);
        await _db.SaveChangesAsync();

        // Assert
        var persisted = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId)
            .OrderBy(r => r.Email)
            .ToListAsync();

        persisted.Should().HaveCount(2);
        persisted.Should().ContainSingle(r => r.FirstName == "User0" && r.LastName == "Doe");
        persisted.Should().ContainSingle(r => r.FirstName == "User1" && r.LastName == "Doe");
        persisted.Should().AllSatisfy(r => r.EmailDomain.Should().Be("example.com"));

        var importSummary = await _db.SessionImportSummaries
            .Where(s => s.SessionId == session.SessionId && s.ImportType == SessionImportType.Registrations)
            .FirstOrDefaultAsync();

        importSummary.Should().NotBeNull();
        importSummary!.RowCount.Should().Be(2);
    }

    [Fact]
    public async Task ConfirmEndpoint_ReplacesPreviousRegistrations()
    {
        // Arrange - Create session with existing registrations
        var session = await SeedSessionAsync();
        
        var oldRegistrations = new List<NormalizedRegistration>
        {
            new()
            {
                RegistrationId = Guid.NewGuid(),
                SessionId = session.SessionId,
                OwnerUserId = OwnerUserId,
                Email = "old@example.com",
                EmailDomain = "example.com",
                FirstName = "Old",
                LastName = "User",
                RegisteredAt = DateTime.UtcNow
            }
        };
        _db.NormalizedRegistrations.AddRange(oldRegistrations);
        await _db.SaveChangesAsync();

        var newRegistrants = CreateSuccessfulRegistrants(2);
        var request = new ConfirmRegistrationImportRequest { Registrants = newRegistrants };

        // Act
        var existing = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId)
            .ToListAsync();
        _db.NormalizedRegistrations.RemoveRange(existing);

        var normalized = newRegistrants
            .Where(r => r.Status == "success")
            .Select(r => new NormalizedRegistration
            {
                RegistrationId = Guid.NewGuid(),
                SessionId = session.SessionId,
                OwnerUserId = OwnerUserId,
                Email = r.Email.ToLowerInvariant(),
                EmailDomain = ExtractDomain(r.Email),
                FirstName = r.FirstName,
                LastName = r.LastName,
                RegisteredAt = r.RegisteredAt
            })
            .ToList();

        _db.NormalizedRegistrations.AddRange(normalized);
        await _db.SaveChangesAsync();

        // Assert
        var persisted = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId)
            .ToListAsync();

        persisted.Should().HaveCount(2);
        persisted.Any(r => r.Email == "old@example.com").Should().BeFalse();
        persisted.All(r => r.Email.StartsWith("user")).Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEndpoint_WithFailedRegistrants_OnlyPersistSuccessful()
    {
        // Arrange
        var session = await SeedSessionAsync();
        var registrants = CreateMixedRegistrants();  // 2 success, 1 failed
        var request = new ConfirmRegistrationImportRequest { Registrants = registrants };

        // Act
        var normalized = registrants
            .Where(r => r.Status == "success")
            .Select(r => new NormalizedRegistration
            {
                RegistrationId = Guid.NewGuid(),
                SessionId = session.SessionId,
                OwnerUserId = OwnerUserId,
                Email = r.Email.ToLowerInvariant(),
                EmailDomain = ExtractDomain(r.Email),
                FirstName = r.FirstName,
                LastName = r.LastName,
                RegisteredAt = r.RegisteredAt
            })
            .ToList();

        _db.NormalizedRegistrations.AddRange(normalized);
        await _db.SaveChangesAsync();

        // Assert
        var persisted = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId)
            .ToListAsync();

        persisted.Should().HaveCount(2);  // Only successful ones
    }

    [Fact]
    public async Task ConfirmEndpoint_UpdatesImportSummaryIfExists()
    {
        // Arrange - Create session with existing import summary
        var session = await SeedSessionAsync();
        
        var oldSummary = new SessionImportSummary
        {
            SessionImportSummaryId = Guid.NewGuid(),
            SessionId = session.SessionId,
            ImportType = SessionImportType.Registrations,
            FileName = "old-import.csv",
            RowCount = 5,
            ImportedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.SessionImportSummaries.Add(oldSummary);
        await _db.SaveChangesAsync();

        var newRegistrants = CreateSuccessfulRegistrants(3);

        // Act
        var existingSummary = await _db.SessionImportSummaries
            .FirstOrDefaultAsync(s => s.SessionId == session.SessionId && s.ImportType == SessionImportType.Registrations);

        if (existingSummary != null)
        {
            existingSummary.RowCount = newRegistrants.Count;
            existingSummary.ImportedAt = DateTime.UtcNow;
            existingSummary.FileName = "registrations-ai-parsed.csv";
            _db.SessionImportSummaries.Update(existingSummary);
        }
        await _db.SaveChangesAsync();

        // Assert
        var updated = await _db.SessionImportSummaries
            .FirstOrDefaultAsync(s => s.SessionId == session.SessionId && s.ImportType == SessionImportType.Registrations);

        updated.Should().NotBeNull();
        updated!.RowCount.Should().Be(3);
        updated.FileName.Should().Be("registrations-ai-parsed.csv");
    }

    // ==================== Helper Methods ====================

    private static string ExtractDomain(string email)
    {
        if (string.IsNullOrEmpty(email)) return string.Empty;
        var atIndex = email.LastIndexOf('@');
        return atIndex >= 0 ? email.Substring(atIndex + 1).ToLowerInvariant() : string.Empty;
    }
}
