using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.People;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    public async Task<IEnumerable<SessionListItemDto>> GetBySeriesAsync(Guid seriesId, string ownerUserId, string ownerDisplayName = "")
    {
        var sessions = await _db.Sessions
            .Where(s => s.SeriesId == seriesId && s.OwnerUserId == ownerUserId)
            .OrderBy(s => s.StartsAt)
            .ToListAsync();

        var sessionIds = sessions.Select(s => s.SessionId).ToList();

        var metrics = await _db.SessionMetrics
            .Where(m => sessionIds.Contains(m.SessionId))
            .ToDictionaryAsync(m => m.SessionId);

        var presentersBySession = await _db.Set<SessionPresenter>()
            .Where(p => sessionIds.Contains(p.SessionId))
            .ToListAsync();

        var coordinatorsBySession = await _db.Set<SessionCoordinator>()
            .Where(c => sessionIds.Contains(c.SessionId))
            .ToListAsync();

        var presenterLookup = presentersBySession
            .GroupBy(p => p.SessionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var coordinatorLookup = coordinatorsBySession
            .GroupBy(c => c.SessionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return sessions.Select(s =>
        {
            var m = metrics.TryGetValue(s.SessionId, out var sm) ? sm : null;
            var presenters = presenterLookup.TryGetValue(s.SessionId, out var pl) ? pl : [];
            var coordinators = coordinatorLookup.TryGetValue(s.SessionId, out var cl) ? cl : [];

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
                s.LastSyncAt,
                presenters.Count,
                coordinators.Count,
                ownerDisplayName,
                presenters.Select(p => new PersonSummary(p.DisplayName, p.Email)).ToList(),
                coordinators.Select(c => new PersonSummary(c.DisplayName, c.Email)).ToList());
        });
    }

    public async Task<SessionResponseDto?> GetByIdAsync(Guid sessionId, string ownerUserId)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null) return null;

        var (presenters, coordinators) = await GetRolesAsync(sessionId);
        return ToResponseDto(session, presenters, coordinators);
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

        var (presenters, coordinators) = await GetRolesAsync(sessionId);
        return (ToResponseDto(session, presenters, coordinators), null);
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

        var seriesId = session.SeriesId;
        _db.Sessions.Remove(session);

        // If the parent series is Published, check whether any published sessions remain.
        // If none remain, revert the series to Draft so it doesn't show as "Partially Published".
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == seriesId && s.OwnerUserId == ownerUserId);
        if (series is not null && series.Status == SeriesStatus.Published)
        {
            var hasPublishedSessions = await _db.Sessions
                .AnyAsync(s => s.SeriesId == seriesId
                            && s.SessionId != sessionId
                            && s.Status == SessionStatus.Published);
            if (!hasPublishedSessions)
            {
                series.Status = SeriesStatus.Draft;
                series.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }

    // TODO-SPEC: Session-level publish not yet defined in SPEC-110; added to support
    // publishing individual Draft sessions after a series has already been published.
    /// <summary>
    /// Publish a single Draft session within an already-Published series.
    /// Creates a Teams webinar, publishes it, and transitions the session to Published.
    /// </summary>
    public async Task<(SessionResponseDto? session, string? errorCode, string? errorMessage)> PublishAsync(
        Guid sessionId, string ownerUserId,
        string? oboToken = null, ITeamsGraphClient? graphClient = null, ILogger? logger = null,
        IConfiguration? config = null)
    {
        logger ??= NullLogger.Instance;

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null)
            return (null, "session_not_found", null);

        if (session.Status == SessionStatus.Published)
            return (null, "SESSION_ALREADY_PUBLISHED", null);

        // Verify the parent series is Published
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == session.SeriesId && s.OwnerUserId == ownerUserId);
        if (series is null)
            return (null, "series_not_found", null);
        if (series.Status != SeriesStatus.Published)
            return (null, "SERIES_NOT_PUBLISHED", null);

        // Stub path: no graph client → flip status without Teams interaction
        if (graphClient is null || string.IsNullOrEmpty(oboToken))
        {
            session.Status = SessionStatus.Published;
            session.ReconcileStatus = ReconcileStatus.Synced;
            session.LastSyncAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var (p, c) = await GetRolesAsync(sessionId);
            return (ToResponseDto(session, p, c), null, null);
        }

        // Guard: Teams cannot create a webinar whose start time is in the past
        if (session.StartsAt < DateTime.UtcNow)
        {
            logger.LogWarning(
                "Cannot publish session with past start date. SessionId={SessionId}, StartsAt={StartsAt}",
                sessionId, session.StartsAt);
            return (null, "SESSION_DATES_IN_PAST",
                "Cannot create a Teams webinar for a session that has already started. Update the session dates to a future time, then retry.");
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

            var (p, c) = await GetRolesAsync(sessionId);

            // SPEC-210: best-effort sync of local presenters/co-organizers to the new Teams webinar
            if (config is null && (p.Count > 0 || c.Count > 0))
            {
                logger.LogWarning(
                    "Skipping role sync after publish: IConfiguration not provided. SessionId={SessionId}",
                    sessionId);
            }
            else if (config is not null && (p.Count > 0 || c.Count > 0))
            {
                var tenantId = config["AzureAd:TenantId"];
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try
                    {
                        await Task.WhenAll(p.Select(presenter =>
                            graphClient.AddWebinarPresenterAsync(createdWebinarId, presenter.EntraUserId, tenantId, oboToken)));
                        if (c.Count > 0)
                            await graphClient.SetWebinarCoOrganizersAsync(createdWebinarId, c.Select(co => co.EntraUserId), oboToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Role sync after publish failed for SessionId={SessionId}. Local roles are correct.",
                            sessionId);
                    }
                }
            }

            return (ToResponseDto(session, p, c), null, null);
        }
        catch (TeamsLicenseException lex)
        {
            logger.LogWarning(lex, "Teams license required during session publish. SessionId={SessionId}", sessionId);
            await RollbackWebinarAsync(graphClient, oboToken, createdWebinarId, logger);
            return (null, "TEAMS_LICENSE_REQUIRED", null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Session publish failed. SessionId={SessionId}", sessionId);
            var rollbackOk = await RollbackWebinarAsync(graphClient, oboToken, createdWebinarId, logger);
            var errorCode = rollbackOk ? "SESSION_PUBLISH_FAILED" : "SESSION_PUBLISH_PARTIAL_FAILURE";
            return (null, errorCode, ex.Message);
        }
    }

    // --- Presenter / Coordinator role management (SPEC-210) ---

    public async Task<List<SessionPresenterDto>> GetPresentersAsync(Guid sessionId)
    {
        return await _db.SessionPresenters
            .Where(p => p.SessionId == sessionId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new SessionPresenterDto(p.SessionPresenterId, p.EntraUserId, p.DisplayName, p.Email))
            .ToListAsync();
    }

    public async Task<List<SessionCoordinatorDto>> GetCoordinatorsAsync(Guid sessionId)
    {
        return await _db.SessionCoordinators
            .Where(c => c.SessionId == sessionId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new SessionCoordinatorDto(c.SessionCoordinatorId, c.EntraUserId, c.DisplayName, c.Email))
            .ToListAsync();
    }

    /// <summary>
    /// Replace all presenters for a session. If the session is Published with a Teams webinar,
    /// performs a diff-based sync (add new, remove old) against Teams. Teams sync failures are
    /// logged as warnings; local changes are still committed.
    /// </summary>
    public async Task<(List<SessionPresenterDto>? presenters, string? errorCode)> SetPresentersAsync(
        Guid sessionId, string ownerUserId, SetPresentersRequest req,
        string? oboToken, ITeamsGraphClient? graphClient, IConfiguration config, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        if (req.People is null)
            return (null, "people_required");

        var duplicates = req.People
            .GroupBy(p => p.EntraUserId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();
        if (duplicates.Count > 0)
            return (null, "duplicate_entra_user_id");

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null)
            return (null, "session_not_found");

        // Remove existing presenters
        var existing = await _db.SessionPresenters
            .Where(p => p.SessionId == sessionId)
            .ToListAsync();
        _db.SessionPresenters.RemoveRange(existing);

        // Insert new presenters
        var now = DateTime.UtcNow;
        var newPresenters = req.People.Select(p => new SessionPresenter
        {
            SessionPresenterId = Guid.NewGuid(),
            SessionId = sessionId,
            EntraUserId = p.EntraUserId,
            DisplayName = p.DisplayName,
            Email = p.Email,
            CreatedAt = now
        }).ToList();
        _db.SessionPresenters.AddRange(newPresenters);
        await _db.SaveChangesAsync();

        // SPEC-210: diff-based sync to Teams if published
        if (session.Status == SessionStatus.Published
            && session.TeamsWebinarId is not null
            && graphClient is not null
            && !string.IsNullOrEmpty(oboToken))
        {
            try
            {
                var tenantId = config["AzureAd:TenantId"]!;
                var teamsPresenters = await graphClient.GetWebinarPresentersAsync(session.TeamsWebinarId, oboToken);
                var teamsEntraIds = teamsPresenters.Select(tp => tp.EntraUserId).ToHashSet();
                var desiredEntraIds = req.People.Select(p => p.EntraUserId).ToHashSet();

                // Add new presenters to Teams
                foreach (var entraUserId in desiredEntraIds.Except(teamsEntraIds))
                {
                    await graphClient.AddWebinarPresenterAsync(session.TeamsWebinarId, entraUserId, tenantId, oboToken);
                }

                // Remove old presenters from Teams
                foreach (var tp in teamsPresenters.Where(tp => !desiredEntraIds.Contains(tp.EntraUserId)))
                {
                    await graphClient.RemoveWebinarPresenterAsync(session.TeamsWebinarId, tp.PresenterId, oboToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Teams presenter sync failed for SessionId={SessionId}. Local changes saved.",
                    sessionId);
            }
        }

        return (newPresenters.Select(p =>
            new SessionPresenterDto(p.SessionPresenterId, p.EntraUserId, p.DisplayName, p.Email)).ToList(), null);
    }

    /// <summary>
    /// Replace all coordinators for a session. If the session is Published with a Teams webinar,
    /// performs a full replacement via SetWebinarCoOrganizersAsync. Teams sync failures are
    /// logged as warnings; local changes are still committed.
    /// </summary>
    public async Task<(List<SessionCoordinatorDto>? coordinators, string? errorCode)> SetCoordinatorsAsync(
        Guid sessionId, string ownerUserId, SetCoordinatorsRequest req,
        string? oboToken, ITeamsGraphClient? graphClient, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        if (req.People is null)
            return (null, "people_required");

        var duplicates = req.People
            .GroupBy(p => p.EntraUserId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();
        if (duplicates.Count > 0)
            return (null, "duplicate_entra_user_id");

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null)
            return (null, "session_not_found");

        // Remove existing coordinators
        var existing = await _db.SessionCoordinators
            .Where(c => c.SessionId == sessionId)
            .ToListAsync();
        _db.SessionCoordinators.RemoveRange(existing);

        // Insert new coordinators
        var now = DateTime.UtcNow;
        var newCoordinators = req.People.Select(p => new SessionCoordinator
        {
            SessionCoordinatorId = Guid.NewGuid(),
            SessionId = sessionId,
            EntraUserId = p.EntraUserId,
            DisplayName = p.DisplayName,
            Email = p.Email,
            CreatedAt = now
        }).ToList();
        _db.SessionCoordinators.AddRange(newCoordinators);
        await _db.SaveChangesAsync();

        // SPEC-210: full replacement sync to Teams if published
        if (session.Status == SessionStatus.Published
            && session.TeamsWebinarId is not null
            && graphClient is not null
            && !string.IsNullOrEmpty(oboToken))
        {
            try
            {
                var entraUserIds = req.People.Select(p => p.EntraUserId);
                await graphClient.SetWebinarCoOrganizersAsync(session.TeamsWebinarId, entraUserIds, oboToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Teams coordinator sync failed for SessionId={SessionId}. Local changes saved.",
                    sessionId);
            }
        }

        return (newCoordinators.Select(c =>
            new SessionCoordinatorDto(c.SessionCoordinatorId, c.EntraUserId, c.DisplayName, c.Email)).ToList(), null);
    }

    // --- Helpers ---

    private async Task<(List<SessionPresenterDto>, List<SessionCoordinatorDto>)> GetRolesAsync(Guid sessionId)
    {
        var presenters = await GetPresentersAsync(sessionId);
        var coordinators = await GetCoordinatorsAsync(sessionId);
        return (presenters, coordinators);
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

    private static SessionResponseDto ToResponseDto(
        Session s,
        List<SessionPresenterDto>? presenters = null,
        List<SessionCoordinatorDto>? coordinators = null) =>
        new(s.SessionId, s.SeriesId, s.Title, s.StartsAt, s.EndsAt,
            s.Status.ToString(), s.TeamsWebinarId, s.JoinWebUrl,
            s.ReconcileStatus.ToString(), s.DriftStatus.ToString(),
            s.LastSyncAt, s.LastError,
            presenters ?? new List<SessionPresenterDto>(),
            coordinators ?? new List<SessionCoordinatorDto>());
}
