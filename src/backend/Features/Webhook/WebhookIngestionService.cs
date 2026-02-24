using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Features.Webhook;

/// <summary>
/// Handles inbound Graph change notifications by fetching the current state from
/// Graph and reconciling NormalizedRegistration / NormalizedAttendance rows.
/// After reconciliation, metrics are recomputed atomically.
///
/// SPEC-200 §3 (ingestion) / SPEC-300 §7–§8 (metrics trigger).
/// </summary>
public class WebhookIngestionService
{
    private readonly AppDbContext _db;
    private readonly ITeamsGraphClient _graphClient;
    private readonly InternalDomainFilter _filter;
    private readonly MetricsRecomputeService _metricsRecompute;
    private readonly ILogger _logger;

    public WebhookIngestionService(
        AppDbContext db,
        ITeamsGraphClient graphClient,
        InternalDomainFilter filter,
        MetricsRecomputeService metricsRecompute,
        ILogger<WebhookIngestionService> logger)
    {
        _db = db;
        _graphClient = graphClient;
        _filter = filter;
        _metricsRecompute = metricsRecompute;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Registration change notification
    // ─────────────────────────────────────────────────────────────────────────

    public async Task HandleRegistrationAsync(
        string teamsWebinarId, string correlationId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Handling registration notification. TeamsWebinarId={TeamsWebinarId} CorrelationId={CorrelationId}",
            teamsWebinarId, correlationId);

        // 1. Find session by TeamsWebinarId
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.TeamsWebinarId == teamsWebinarId, ct);

        if (session is null)
        {
            _logger.LogWarning(
                "No session found for TeamsWebinarId={TeamsWebinarId}. CorrelationId={CorrelationId}",
                teamsWebinarId, correlationId);
            return;
        }

        // 2. Fetch registrations from Graph (app credentials)
        var graphRegistrations = (await _graphClient.GetRegistrationsAsync(teamsWebinarId, ct)).ToList();

        // 3. Normalize and build the canonical set
        var incoming = graphRegistrations
            .Select(r => new
            {
                Email = DomainNormalizer.NormalizeEmail(r.Email),
                Domain = DomainNormalizer.NormalizeEmailDomain(r.Email),
                r.RegisteredAt
            })
            .GroupBy(x => x.Email)
            .Select(g => g.First())       // deduplicate by email
            .ToDictionary(x => x.Email, StringComparer.OrdinalIgnoreCase);

        // 4. Load existing registrations for this session
        var existing = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == session.SessionId)
            .ToListAsync(ct);

        var existingByEmail = existing
            .ToDictionary(r => r.Email, StringComparer.OrdinalIgnoreCase);

        // 5. Upsert (insert missing, update existing)
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

        // 6. Delete registrations no longer present in the fetched set (reconcile)
        var toDelete = existing
            .Where(r => !incoming.ContainsKey(r.Email))
            .ToList();

        if (toDelete.Count > 0)
            _db.NormalizedRegistrations.RemoveRange(toDelete);

        await _db.SaveChangesAsync(ct);

        // 7. Recompute metrics
        await _metricsRecompute.RecomputeSessionMetricsAsync(session.SessionId, ct);
        await _metricsRecompute.RecomputeSeriesMetricsAsync(session.SeriesId, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attendance report change notification
    // ─────────────────────────────────────────────────────────────────────────

    public async Task HandleAttendanceReportAsync(
        string teamsWebinarId, string correlationId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Handling attendance notification. TeamsWebinarId={TeamsWebinarId} CorrelationId={CorrelationId}",
            teamsWebinarId, correlationId);

        // 1. Find session by TeamsWebinarId
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.TeamsWebinarId == teamsWebinarId, ct);

        if (session is null)
        {
            _logger.LogWarning(
                "No session found for TeamsWebinarId={TeamsWebinarId}. CorrelationId={CorrelationId}",
                teamsWebinarId, correlationId);
            return;
        }

        // 2. Fetch attendance from Graph (app credentials)
        var graphAttendance = (await _graphClient.GetAttendanceAsync(teamsWebinarId, ct)).ToList();

        // 3. Normalize and build canonical set
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

        // 4. Load existing attendance for this session
        var existing = await _db.NormalizedAttendances
            .Where(a => a.SessionId == session.SessionId)
            .ToListAsync(ct);

        var existingByEmail = existing
            .ToDictionary(a => a.Email, StringComparer.OrdinalIgnoreCase);

        // 5. Upsert
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

        // 6. Delete attendance not in fetched set
        var toDelete = existing
            .Where(a => !incoming.ContainsKey(a.Email))
            .ToList();

        if (toDelete.Count > 0)
            _db.NormalizedAttendances.RemoveRange(toDelete);

        await _db.SaveChangesAsync(ct);

        // 7. Recompute metrics
        await _metricsRecompute.RecomputeSessionMetricsAsync(session.SessionId, ct);
        await _metricsRecompute.RecomputeSeriesMetricsAsync(session.SeriesId, ct);

        // 8. Delete GraphSubscriptions for this session (reconciliation complete)
        var subscriptions = await _db.GraphSubscriptions
            .Where(s => s.SessionId == session.SessionId)
            .ToListAsync(ct);

        if (subscriptions.Count > 0)
        {
            _db.GraphSubscriptions.RemoveRange(subscriptions);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Cleared {Count} subscriptions for session {SessionId} after attendance reconciliation.",
                subscriptions.Count, session.SessionId);
        }
    }
}
