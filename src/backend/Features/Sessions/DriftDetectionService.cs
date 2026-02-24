using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EdgeFront.Builder.Features.Sessions;

/// <summary>
/// Detects configuration drift between the local session record and the live
/// Teams webinar state fetched via Graph.
///
/// SPEC-200 §5 (drift detection).
/// </summary>
public class DriftDetectionService
{
    private readonly AppDbContext _db;
    private readonly ITeamsGraphClient _graphClient;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public DriftDetectionService(
        AppDbContext db,
        ITeamsGraphClient graphClient,
        IMemoryCache cache)
    {
        _db = db;
        _graphClient = graphClient;
        _cache = cache;
    }

    /// <summary>
    /// Checks whether the live Teams webinar state matches the local session record.
    /// </summary>
    /// <returns>The current <see cref="DriftStatus"/> for the session.</returns>
    public async Task<DriftStatus> CheckDriftAsync(
        Guid sessionId, string ownerUserId, CancellationToken ct = default)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId, ct);

        if (session is null)
            return DriftStatus.None;

        // Only Published sessions with a known webinar ID can drift
        if (session.Status != SessionStatus.Published || session.TeamsWebinarId is null)
            return DriftStatus.None;

        // Return cached result if available
        var cacheKey = $"drift_{sessionId}";
        if (_cache.TryGetValue(cacheKey, out DriftStatus cached))
            return cached;

        TeamsWebinarInfo? info;
        try
        {
            info = await _graphClient.GetWebinarMetadataAsync(session.TeamsWebinarId, ct);
        }
        catch (Exception)
        {
            // If the Graph call fails, return current status without updating
            return session.DriftStatus;
        }

        DriftStatus result;

        if (info is null)
        {
            // Webinar not found in Graph — treat as drift
            result = DriftStatus.DriftDetected;
        }
        else
        {
            var titleMatches = string.Equals(
                info.Title, session.Title, StringComparison.Ordinal);

            var startsAtMatches = Math.Abs(
                (info.StartsAt.UtcDateTime - session.StartsAt).TotalSeconds) < 1;

            var endsAtMatches = Math.Abs(
                (info.EndsAt.UtcDateTime - session.EndsAt).TotalSeconds) < 1;

            result = (titleMatches && startsAtMatches && endsAtMatches)
                ? DriftStatus.None
                : DriftStatus.DriftDetected;
        }

        // Persist the detected drift status
        if (session.DriftStatus != result)
        {
            session.DriftStatus = result;
            await _db.SaveChangesAsync(ct);
        }

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }
}
