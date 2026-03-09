using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Features.Sessions;

/// <summary>
/// User-initiated sync of registration and attendance data from Teams via Graph.
/// All Graph calls use delegated (OBO) tokens — no application credentials.
/// Replaces the former automatic webhook ingestion pipeline.
/// </summary>
public class SyncService
{
    private readonly AppDbContext _db;
    private readonly ITeamsGraphClient _graphClient;
    private readonly MetricsRecomputeService _metricsRecompute;
    private readonly ILogger _logger;

    public SyncService(
        AppDbContext db,
        ITeamsGraphClient graphClient,
        MetricsRecomputeService metricsRecompute,
        ILogger<SyncService> logger)
    {
        _db = db;
        _graphClient = graphClient;
        _metricsRecompute = metricsRecompute;
        _logger = logger;
    }

    /// <summary>
    /// Fetches registrations and attendance from Graph using a delegated (OBO) token,
    /// normalizes and upserts into the DB, and recomputes session + series metrics.
    /// </summary>
    public async Task<SyncResult> SyncSessionAsync(
        Guid sessionId, string ownerUserId, string oboToken, CancellationToken ct = default)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId, ct);

        if (session is null)
            return new SyncResult(false, "session_not_found");

        if (session.Status != SessionStatus.Published || session.TeamsWebinarId is null)
            return new SyncResult(false, "session_not_published");

        _logger.LogInformation(
            "User-initiated sync starting. SessionId={SessionId} TeamsWebinarId={TeamsWebinarId}",
            sessionId, session.TeamsWebinarId);

        session.ReconcileStatus = ReconcileStatus.Reconciling;
        await _db.SaveChangesAsync(ct);

        try
        {
            // Backfill JoinWebUrl for sessions published before this field was captured
            if (session.JoinWebUrl is null)
            {
                var metadata = await _graphClient.GetWebinarMetadataAsync(session.TeamsWebinarId!, oboToken, ct);
                if (metadata?.JoinWebUrl is not null)
                    session.JoinWebUrl = metadata.JoinWebUrl;
            }

            await SyncRegistrationsAsync(session, oboToken, ct);
            await SyncAttendanceAsync(session, oboToken, ct);

            // Best-effort: sync presenters and coordinators from Teams
            try
            {
                await SyncPresentersAsync(session, oboToken, ct);
                await SyncCoordinatorsAsync(session, oboToken, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Presenter/coordinator sync failed for SessionId={SessionId}. Registration/attendance data is still saved.",
                    sessionId);
            }

            session.ReconcileStatus = ReconcileStatus.Synced;
            session.LastSyncAt = DateTime.UtcNow;
            session.LastError = null;
            await _db.SaveChangesAsync(ct);

            // Recompute metrics
            await _metricsRecompute.RecomputeSessionMetricsAsync(session.SessionId, ct);
            await _metricsRecompute.RecomputeSeriesMetricsAsync(session.SeriesId, ct);

            _logger.LogInformation(
                "User-initiated sync completed. SessionId={SessionId}", sessionId);

            return new SyncResult(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "User-initiated sync failed. SessionId={SessionId}", sessionId);

            session.ReconcileStatus = ReconcileStatus.Synced;
            session.LastError = ex.Message;
            await _db.SaveChangesAsync(ct);

            return new SyncResult(false, "SYNC_FAILED");
        }
    }

    /// <summary>
    /// Syncs all published sessions in a series. Called automatically when the
    /// user navigates to the series detail page.
    /// </summary>
    public async Task<SyncSeriesResult> SyncSeriesAsync(
        Guid seriesId, string ownerUserId, string oboToken, CancellationToken ct = default)
    {
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == seriesId && s.OwnerUserId == ownerUserId, ct);

        if (series is null)
            return new SyncSeriesResult(false, 0, 0, "series_not_found");

        var sessions = await _db.Sessions
            .Where(s => s.SeriesId == seriesId
                && s.Status == SessionStatus.Published
                && s.TeamsWebinarId != null)
            .ToListAsync(ct);

        if (sessions.Count == 0)
            return new SyncSeriesResult(true, 0, 0, null);

        _logger.LogInformation(
            "Series-level sync starting. SeriesId={SeriesId} SessionCount={Count}",
            seriesId, sessions.Count);

        int synced = 0, failed = 0;
        foreach (var session in sessions)
        {
            var result = await SyncSessionAsync(session.SessionId, ownerUserId, oboToken, ct);
            if (result.Success)
                synced++;
            else
                failed++;
        }

        _logger.LogInformation(
            "Series-level sync completed. SeriesId={SeriesId} Synced={Synced} Failed={Failed}",
            seriesId, synced, failed);

        return new SyncSeriesResult(failed == 0, synced, failed, failed > 0 ? "PARTIAL_SYNC_FAILURE" : null);
    }

    private async Task SyncRegistrationsAsync(Session session, string oboToken, CancellationToken ct)
    {
        var graphRegistrations = (await _graphClient.GetRegistrationsAsync(
            session.TeamsWebinarId!, oboToken, ct)).ToList();

        var incoming = graphRegistrations
            .Select(r => new
            {
                Email = DomainNormalizer.NormalizeEmail(r.Email),
                Domain = DomainNormalizer.NormalizeEmailDomain(r.Email),
                r.RegisteredAt
            })
            .GroupBy(x => x.Email)
            .Select(g => g.First())
            .ToDictionary(x => x.Email, StringComparer.OrdinalIgnoreCase);

        var existing = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId)
            .ToListAsync(ct);

        var existingByEmail = existing
            .ToDictionary(r => r.Email, StringComparer.OrdinalIgnoreCase);

        foreach (var (email, item) in incoming)
        {
            if (existingByEmail.TryGetValue(email, out var existingReg))
            {
                existingReg.RegisteredAt = item.RegisteredAt.UtcDateTime;
                existingReg.EmailDomain = item.Domain;
            }
            else
            {
                _db.NormalizedRegistrations.Add(new NormalizedRegistration
                {
                    RegistrationId = Guid.NewGuid(),
                    SessionId = session.SessionId,
                    OwnerUserId = session.OwnerUserId,
                    Email = email,
                    EmailDomain = item.Domain,
                    RegisteredAt = item.RegisteredAt.UtcDateTime
                });
            }
        }

        var toDelete = existing
            .Where(r => !incoming.ContainsKey(r.Email))
            .ToList();

        if (toDelete.Count > 0)
            _db.NormalizedRegistrations.RemoveRange(toDelete);

        await _db.SaveChangesAsync(ct);
    }

    private async Task SyncAttendanceAsync(Session session, string oboToken, CancellationToken ct)
    {
        var graphAttendance = (await _graphClient.GetAttendanceAsync(
            session.TeamsWebinarId!, oboToken, ct)).ToList();

        var incoming = graphAttendance
            .Select(a => new
            {
                Email = DomainNormalizer.NormalizeEmail(a.Email),
                Domain = DomainNormalizer.NormalizeEmailDomain(a.Email),
                a.Attended,
                a.DurationSeconds,
                a.DurationPercent,
                FirstJoinAt = a.FirstJoinAt?.UtcDateTime,
                LastLeaveAt = a.LastLeaveAt?.UtcDateTime
            })
            .GroupBy(x => x.Email)
            .Select(g => g.First())
            .ToDictionary(x => x.Email, StringComparer.OrdinalIgnoreCase);

        var existing = await _db.NormalizedAttendances
            .Where(a => a.SessionId == session.SessionId)
            .ToListAsync(ct);

        var existingByEmail = existing
            .ToDictionary(a => a.Email, StringComparer.OrdinalIgnoreCase);

        foreach (var (email, item) in incoming)
        {
            if (existingByEmail.TryGetValue(email, out var existingAtt))
            {
                existingAtt.Attended = item.Attended;
                existingAtt.DurationSeconds = item.DurationSeconds;
                existingAtt.DurationPercent = item.DurationPercent;
                existingAtt.FirstJoinAt = item.FirstJoinAt;
                existingAtt.LastLeaveAt = item.LastLeaveAt;
                existingAtt.EmailDomain = item.Domain;
            }
            else
            {
                _db.NormalizedAttendances.Add(new NormalizedAttendance
                {
                    AttendanceId = Guid.NewGuid(),
                    SessionId = session.SessionId,
                    OwnerUserId = session.OwnerUserId,
                    Email = email,
                    EmailDomain = item.Domain,
                    Attended = item.Attended,
                    DurationSeconds = item.DurationSeconds,
                    DurationPercent = item.DurationPercent,
                    FirstJoinAt = item.FirstJoinAt,
                    LastLeaveAt = item.LastLeaveAt
                });
            }
        }

        var toDelete = existing
            .Where(a => !incoming.ContainsKey(a.Email))
            .ToList();

        if (toDelete.Count > 0)
            _db.NormalizedAttendances.RemoveRange(toDelete);

        await _db.SaveChangesAsync(ct);
    }

    private async Task SyncPresentersAsync(Session session, string oboToken, CancellationToken ct)
    {
        var teamsPresenters = (await _graphClient.GetWebinarPresentersAsync(
            session.TeamsWebinarId!, oboToken, ct))
            .Where(p => !string.IsNullOrEmpty(p.EntraUserId))
            .ToList();

        if (teamsPresenters.Count == 0)
        {
            // No presenters in Teams — clear local rows
            var toRemove = await _db.SessionPresenters
                .Where(p => p.SessionId == session.SessionId)
                .ToListAsync(ct);
            if (toRemove.Count > 0)
                _db.SessionPresenters.RemoveRange(toRemove);
            await _db.SaveChangesAsync(ct);
            return;
        }

        // Resolve emails via user info lookup (best-effort per user)
        var userInfoTasks = teamsPresenters.Select(async p =>
        {
            try
            {
                var info = await _graphClient.GetUserInfoAsync(p.EntraUserId, oboToken, ct);
                return (p.EntraUserId, DisplayName: info?.DisplayName ?? p.DisplayName, Email: info?.Email ?? string.Empty);
            }
            catch
            {
                return (p.EntraUserId, p.DisplayName, Email: string.Empty);
            }
        });
        var resolved = (await Task.WhenAll(userInfoTasks)).ToDictionary(x => x.EntraUserId, StringComparer.OrdinalIgnoreCase);

        // Full-replace local presenter rows
        var existing = await _db.SessionPresenters
            .Where(p => p.SessionId == session.SessionId)
            .ToListAsync(ct);
        _db.SessionPresenters.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var tp in teamsPresenters)
        {
            resolved.TryGetValue(tp.EntraUserId, out var info);
            _db.SessionPresenters.Add(new SessionPresenter
            {
                SessionPresenterId = Guid.NewGuid(),
                SessionId = session.SessionId,
                EntraUserId = tp.EntraUserId,
                DisplayName = info.DisplayName ?? tp.DisplayName,
                Email = info.Email ?? string.Empty,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SyncCoordinatorsAsync(Session session, string oboToken, CancellationToken ct)
    {
        var teamsCoOrganizers = (await _graphClient.GetWebinarCoOrganizersAsync(
            session.TeamsWebinarId!, oboToken, ct))
            .Where(c => !string.IsNullOrEmpty(c.EntraUserId))
            .ToList();

        if (teamsCoOrganizers.Count == 0)
        {
            var toRemove = await _db.SessionCoordinators
                .Where(c => c.SessionId == session.SessionId)
                .ToListAsync(ct);
            if (toRemove.Count > 0)
                _db.SessionCoordinators.RemoveRange(toRemove);
            await _db.SaveChangesAsync(ct);
            return;
        }

        // Resolve emails via user info lookup (best-effort per user)
        var userInfoTasks = teamsCoOrganizers.Select(async c =>
        {
            try
            {
                var info = await _graphClient.GetUserInfoAsync(c.EntraUserId, oboToken, ct);
                return (c.EntraUserId, DisplayName: info?.DisplayName ?? c.DisplayName, Email: info?.Email ?? string.Empty);
            }
            catch
            {
                return (c.EntraUserId, c.DisplayName, Email: string.Empty);
            }
        });
        var resolved = (await Task.WhenAll(userInfoTasks)).ToDictionary(x => x.EntraUserId, StringComparer.OrdinalIgnoreCase);

        // Full-replace local coordinator rows
        var existing = await _db.SessionCoordinators
            .Where(c => c.SessionId == session.SessionId)
            .ToListAsync(ct);
        _db.SessionCoordinators.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var co in teamsCoOrganizers)
        {
            resolved.TryGetValue(co.EntraUserId, out var info);
            _db.SessionCoordinators.Add(new SessionCoordinator
            {
                SessionCoordinatorId = Guid.NewGuid(),
                SessionId = session.SessionId,
                EntraUserId = co.EntraUserId,
                DisplayName = info.DisplayName ?? co.DisplayName,
                Email = info.Email ?? string.Empty,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}

public record SyncResult(bool Success, string? ErrorCode);
public record SyncSeriesResult(bool Success, int SyncedCount, int FailedCount, string? ErrorCode);
