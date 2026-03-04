using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Series.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EdgeFront.Builder.Features.Series;

public class SeriesService
{
    private readonly AppDbContext _db;

    public SeriesService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SeriesListItemDto>> GetAllAsync(string ownerUserId)
    {
        var series = await _db.Series
            .Where(s => s.OwnerUserId == ownerUserId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var seriesIds = series.Select(s => s.SeriesId).ToList();

        var metrics = await _db.SeriesMetrics
            .Where(m => seriesIds.Contains(m.SeriesId))
            .ToDictionaryAsync(m => m.SeriesId);

        var sessionCounts = await _db.Sessions
            .Where(s => seriesIds.Contains(s.SeriesId))
            .GroupBy(s => s.SeriesId)
            .Select(g => new { SeriesId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SeriesId, x => x.Count);

        var draftSessionCounts = await _db.Sessions
            .Where(s => seriesIds.Contains(s.SeriesId) && s.Status == SessionStatus.Draft)
            .GroupBy(s => s.SeriesId)
            .Select(g => new { SeriesId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SeriesId, x => x.Count);

        return series.Select(s =>
        {
            var m = metrics.TryGetValue(s.SeriesId, out var sm) ? sm : null;
            return new SeriesListItemDto(
                s.SeriesId,
                s.Title,
                s.Status.ToString(),
                sessionCounts.TryGetValue(s.SeriesId, out var count) ? count : 0,
                draftSessionCounts.TryGetValue(s.SeriesId, out var draftCount) ? draftCount : 0,
                m?.TotalRegistrations ?? 0,
                m?.TotalAttendees ?? 0,
                m?.UniqueAccountsInfluenced ?? 0,
                false,
                s.CreatedAt,
                s.UpdatedAt);
        });
    }

    public async Task<SeriesResponseDto?> GetByIdAsync(Guid id, string ownerUserId)
    {
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == id && s.OwnerUserId == ownerUserId);
        if (series is null) return null;

        var draftCount = await _db.Sessions
            .CountAsync(s => s.SeriesId == id && s.Status == SessionStatus.Draft);
        return ToResponseDto(series, draftCount);
    }

    public async Task<SeriesResponseDto> CreateAsync(CreateSeriesRequest req, string ownerUserId)
    {
        var series = new Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Title = req.Title,
            Status = SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Series.Add(series);
        await _db.SaveChangesAsync();
        return ToResponseDto(series, draftSessionCount: 0);
    }

    public async Task<SeriesResponseDto?> UpdateAsync(Guid id, UpdateSeriesRequest req, string ownerUserId)
    {
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == id && s.OwnerUserId == ownerUserId);
        if (series is null) return null;

        series.Title = req.Title;
        series.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var draftCount = await _db.Sessions
            .CountAsync(s => s.SeriesId == id && s.Status == SessionStatus.Draft);
        return ToResponseDto(series, draftCount);
    }

    public async Task<bool> DeleteAsync(Guid id, string ownerUserId)
    {
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == id && s.OwnerUserId == ownerUserId);
        if (series is null) return false;

        _db.Series.Remove(series);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Backward-compatible overload used by existing tests and call-sites that have not yet
    /// been updated to pass a graph client. When <paramref name="graphClient"/> is null the
    /// series is simply promoted to Published without creating any Teams webinars.
    /// </summary>
    public Task<(SeriesResponseDto? series, string? errorCode)> PublishAsync(Guid id, string ownerUserId)
        => PublishAsync(id, ownerUserId, oboToken: null, graphClient: null, logger: null);

    /// <summary>
    /// Publish flow: creates Teams webinars for every Draft session in the series,
    /// then transitions the series and all sessions to Published.
    /// All Graph calls use delegated (OBO) tokens — no application credentials.
    /// </summary>
    public async Task<(SeriesResponseDto? series, string? errorCode)> PublishAsync(
        Guid id,
        string ownerUserId,
        string? oboToken = null,
        ITeamsGraphClient? graphClient = null,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        // 1. Load series with its Draft sessions
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == id && s.OwnerUserId == ownerUserId);
        if (series is null) return (null, "series_not_found");

        var sessions = await _db.Sessions
            .Where(s => s.SeriesId == id && s.Status == SessionStatus.Draft)
            .ToListAsync();

        // 2. If no graph client, just flip status (stub / unconfigured-Teams path)
        if (graphClient is null || string.IsNullOrEmpty(oboToken))
        {
            series.Status = SeriesStatus.Published;
            series.UpdatedAt = DateTime.UtcNow;
            foreach (var s in sessions)
            {
                s.Status = SessionStatus.Published;
                s.ReconcileStatus = ReconcileStatus.Synced;
                s.LastSyncAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            return (ToResponseDto(series, draftSessionCount: 0), null);
        }

        // 3. Create and publish webinars in Teams; track created IDs for rollback
        var createdWebinarIds = new List<(Session Session, string WebinarId)>();

        try
        {
            foreach (var session in sessions)
            {
                var webinarResult = await graphClient.CreateWebinarAsync(
                    session.Title,
                    new DateTimeOffset(session.StartsAt, TimeSpan.Zero),
                    new DateTimeOffset(session.EndsAt, TimeSpan.Zero),
                    oboToken);

                await graphClient.PublishWebinarAsync(webinarResult.WebinarId, oboToken);

                createdWebinarIds.Add((session, webinarResult.WebinarId));
                session.TeamsWebinarId = webinarResult.WebinarId;
                session.JoinWebUrl = webinarResult.JoinWebUrl;
            }

            // 4. All Teams calls succeeded — commit the publish
            series.Status = SeriesStatus.Published;
            series.UpdatedAt = DateTime.UtcNow;

            foreach (var session in sessions)
            {
                session.Status = SessionStatus.Published;
                session.ReconcileStatus = ReconcileStatus.Synced;
                session.LastSyncAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return (ToResponseDto(series, draftSessionCount: 0), null);
        }
        catch (TeamsLicenseException lex)
        {
            logger.LogWarning(lex, "Teams license required during publish. SeriesId={SeriesId}", id);
            await RollbackWebinarsAsync(graphClient, oboToken, createdWebinarIds, logger);
            return (null, "TEAMS_LICENSE_REQUIRED");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Publish failed. SeriesId={SeriesId}", id);
            var rollbackOk = await RollbackWebinarsAsync(
                graphClient, oboToken, createdWebinarIds, logger);
            return rollbackOk
                ? (null, "PUBLISH_FAILED")
                : (null, "PUBLISH_PARTIAL_FAILURE");
        }
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private static async Task<bool> RollbackWebinarsAsync(
        ITeamsGraphClient graphClient,
        string oboToken,
        List<(Session Session, string WebinarId)> created,
        ILogger logger)
    {
        var allOk = true;

        foreach (var (_, webinarId) in created)
        {
            try { await graphClient.DeleteWebinarAsync(webinarId, oboToken); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Rollback: failed to delete webinar {WebinarId}", webinarId);
                allOk = false;
            }
        }

        return allOk;
    }

    private static SeriesResponseDto ToResponseDto(Domain.Entities.Series s, int draftSessionCount = 0) =>
        new(s.SeriesId, s.Title, s.Status.ToString(), draftSessionCount, s.CreatedAt, s.UpdatedAt);
}
