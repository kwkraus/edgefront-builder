using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EdgeFront.Builder.Features.Sessions;

public class SessionService
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SessionService(AppDbContext db, ILogger<SessionService>? logger = null)
    {
        _db = db;
        _logger = logger is not null ? logger : NullLogger.Instance;
    }

    public async Task<IEnumerable<SessionListItemDto>> GetBySeriesAsync(Guid seriesId, string ownerUserId)
    {
        var sessions = await _db.Sessions
            .Where(s => s.SeriesId == seriesId && s.OwnerUserId == ownerUserId)
            .OrderBy(s => s.StartsAt)
            .ToListAsync();

        var sessionIds = sessions.Select(s => s.SessionId).ToList();

        var metrics = await _db.SessionMetrics
            .Where(m => sessionIds.Contains(m.SessionId))
            .ToDictionaryAsync(m => m.SessionId);

        return sessions.Select(s =>
        {
            var m = metrics.TryGetValue(s.SessionId, out var sm) ? sm : null;
            return new SessionListItemDto(
                s.SessionId,
                s.Title,
                s.StartsAt,
                s.EndsAt,
                s.Status.ToString(),
                s.TeamsWebinarId,
                s.JoinWebUrl,
                s.ReconcileStatus.ToString(),
                s.DriftStatus.ToString(),
                m?.TotalRegistrations ?? 0,
                m?.TotalAttendees ?? 0,
                s.LastSyncAt);
        });
    }

    public async Task<SessionResponseDto?> GetByIdAsync(Guid sessionId, string ownerUserId)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        return session is null ? null : ToResponseDto(session);
    }

    public async Task<(SessionResponseDto? session, string? errorCode)> CreateAsync(
        Guid seriesId, CreateSessionRequest req, string ownerUserId)
    {
        if (req.EndsAt <= req.StartsAt)
            return (null, "invalid_time_range");

        var seriesExists = await _db.Series
            .AnyAsync(s => s.SeriesId == seriesId && s.OwnerUserId == ownerUserId);
        if (!seriesExists)
            return (null, "series_not_found");

        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = seriesId,
            OwnerUserId = ownerUserId,
            Title = req.Title,
            StartsAt = req.StartsAt.Kind == DateTimeKind.Utc ? req.StartsAt : req.StartsAt.ToUniversalTime(),
            EndsAt = req.EndsAt.Kind == DateTimeKind.Utc ? req.EndsAt : req.EndsAt.ToUniversalTime(),
            Status = SessionStatus.Draft,
            DriftStatus = DriftStatus.None,
            ReconcileStatus = ReconcileStatus.Synced
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return (ToResponseDto(session), null);
    }

    public async Task<(SessionResponseDto? session, string? errorCode)> UpdateAsync(
        Guid sessionId, UpdateSessionRequest req, string ownerUserId,
        string? oboToken = null, ITeamsGraphClient? graphClient = null)
    {
        if (req.EndsAt <= req.StartsAt)
            return (null, "invalid_time_range");

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null)
            return (null, "session_not_found");

        session.Title = req.Title;
        session.StartsAt = req.StartsAt.Kind == DateTimeKind.Utc ? req.StartsAt : req.StartsAt.ToUniversalTime();
        session.EndsAt = req.EndsAt.Kind == DateTimeKind.Utc ? req.EndsAt : req.EndsAt.ToUniversalTime();

        // SPEC-200: if Published and graph client provided, sync the Teams webinar
        if (session.Status == SessionStatus.Published
            && session.TeamsWebinarId is not null
            && graphClient is not null
            && !string.IsNullOrEmpty(oboToken))
        {
            try
            {
                await graphClient.UpdateWebinarAsync(
                    session.TeamsWebinarId,
                    session.Title,
                    new DateTimeOffset(session.StartsAt, TimeSpan.Zero),
                    new DateTimeOffset(session.EndsAt, TimeSpan.Zero),
                    oboToken);
            }
            catch (Exception)
            {
                return (null, "TEAMS_UPDATE_FAILED");
            }
        }

        await _db.SaveChangesAsync();
        return (ToResponseDto(session), null);
    }

    public async Task<bool> DeleteAsync(
        Guid sessionId, string ownerUserId,
        ITeamsGraphClient? graphClient = null, string? oboToken = null)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null) return false;

        // Best-effort delete Teams webinar if Published
        if (session.Status == SessionStatus.Published
            && session.TeamsWebinarId is not null
            && graphClient is not null
            && !string.IsNullOrEmpty(oboToken))
        {
            try { await graphClient.DeleteWebinarAsync(session.TeamsWebinarId, oboToken); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Best-effort webinar delete failed for TeamsWebinarId={TeamsWebinarId}. SessionId={SessionId}",
                    session.TeamsWebinarId, sessionId);
            }
        }

        _db.Sessions.Remove(session);
        await _db.SaveChangesAsync();
        return true;
    }

    // TODO-SPEC: Session-level publish not yet defined in SPEC-110; added to support
    // publishing individual Draft sessions after a series has already been published.
    /// <summary>
    /// Publish a single Draft session within an already-Published series.
    /// Creates a Teams webinar, publishes it, and transitions the session to Published.
    /// </summary>
    public async Task<(SessionResponseDto? session, string? errorCode)> PublishAsync(
        Guid sessionId, string ownerUserId,
        string? oboToken = null, ITeamsGraphClient? graphClient = null, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null)
            return (null, "session_not_found");

        if (session.Status == SessionStatus.Published)
            return (null, "SESSION_ALREADY_PUBLISHED");

        // Verify the parent series is Published
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == session.SeriesId && s.OwnerUserId == ownerUserId);
        if (series is null)
            return (null, "series_not_found");
        if (series.Status != SeriesStatus.Published)
            return (null, "SERIES_NOT_PUBLISHED");

        // Stub path: no graph client → flip status without Teams interaction
        if (graphClient is null || string.IsNullOrEmpty(oboToken))
        {
            session.Status = SessionStatus.Published;
            session.ReconcileStatus = ReconcileStatus.Synced;
            session.LastSyncAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (ToResponseDto(session), null);
        }

        // Create and publish the Teams webinar
        string? createdWebinarId = null;
        try
        {
            var webinarResult = await graphClient.CreateWebinarAsync(
                session.Title,
                new DateTimeOffset(session.StartsAt, TimeSpan.Zero),
                new DateTimeOffset(session.EndsAt, TimeSpan.Zero),
                oboToken);
            createdWebinarId = webinarResult.WebinarId;

            await graphClient.PublishWebinarAsync(createdWebinarId, oboToken);

            session.TeamsWebinarId = createdWebinarId;
            session.JoinWebUrl = webinarResult.JoinWebUrl;
            session.Status = SessionStatus.Published;
            session.ReconcileStatus = ReconcileStatus.Synced;
            session.LastSyncAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return (ToResponseDto(session), null);
        }
        catch (TeamsLicenseException lex)
        {
            logger.LogWarning(lex, "Teams license required during session publish. SessionId={SessionId}", sessionId);
            await RollbackWebinarAsync(graphClient, oboToken, createdWebinarId, logger);
            return (null, "TEAMS_LICENSE_REQUIRED");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Session publish failed. SessionId={SessionId}", sessionId);
            var rollbackOk = await RollbackWebinarAsync(graphClient, oboToken, createdWebinarId, logger);
            return (null, rollbackOk ? "SESSION_PUBLISH_FAILED" : "SESSION_PUBLISH_PARTIAL_FAILURE");
        }
    }

    private static async Task<bool> RollbackWebinarAsync(
        ITeamsGraphClient graphClient, string oboToken, string? webinarId, ILogger logger)
    {
        if (string.IsNullOrEmpty(webinarId)) return true;

        try
        {
            await graphClient.DeleteWebinarAsync(webinarId, oboToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rollback: failed to delete webinar {WebinarId}", webinarId);
            return false;
        }
    }

    private static SessionResponseDto ToResponseDto(Session s) =>
        new(s.SessionId, s.SeriesId, s.Title, s.StartsAt, s.EndsAt,
            s.Status.ToString(), s.TeamsWebinarId, s.JoinWebUrl,
            s.ReconcileStatus.ToString(), s.DriftStatus.ToString(),
            s.LastSyncAt, s.LastError);
}
