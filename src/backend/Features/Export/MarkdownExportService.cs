using EdgeFront.Builder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EdgeFront.Builder.Features.Export;

/// <summary>
/// Builds a share-safe markdown export for a single series and all its sessions.
/// </summary>
/// <remarks>
/// <para><strong>Allow-list policy</strong> — only the following fields are ever written
/// into the markdown output:</para>
/// <list type="bullet">
///   <item>Series: <c>Title</c>, <c>Status</c>, <c>CreatedAt</c></item>
///   <item>Session: <c>Title</c>, <c>StartsAt</c>, <c>EndsAt</c>, <c>Status</c></item>
///   <item>Presenters / Coordinators: <c>DisplayName</c> only</item>
/// </list>
/// <para>Fields that are deliberately <em>excluded</em>:
/// <c>TeamsWebinarId</c>, <c>JoinWebUrl</c>, <c>DriftStatus</c>, <c>ReconcileStatus</c>,
/// <c>LastSyncAt</c>, <c>LastError</c>, <c>Email</c>, <c>EntraUserId</c>,
/// registration counts, attendance counts, and all metrics.</para>
/// </remarks>
public sealed class MarkdownExportService
{
    private readonly AppDbContext _db;

    public MarkdownExportService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Exports the specified series as a markdown document.
    /// </summary>
    /// <param name="seriesId">The series to export.</param>
    /// <param name="ownerUserId">
    /// The authenticated user's object ID.  The series must belong to this owner;
    /// if it does not exist or belongs to a different user, <see langword="null"/>
    /// is returned so the caller can issue a 404.
    /// </param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <returns>
    /// A <see cref="MarkdownExportResult"/> on success, or <see langword="null"/>
    /// when the series is not found / not owned by <paramref name="ownerUserId"/>.
    /// </returns>
    public async Task<MarkdownExportResult?> ExportSeriesAsync(
        Guid seriesId,
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Fetch the series ──────────────────────────────────────────────────
        // Project only the allow-listed fields so that EF never materialises
        // sensitive columns into managed memory.
        var series = await _db.Series
            .Where(s => s.SeriesId == seriesId && s.OwnerUserId == ownerUserId)
            .Select(s => new
            {
                s.Title,
                s.Status,
                s.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (series is null)
            return null;

        // ── 2. Fetch sessions ordered chronologically ────────────────────────────
        // Project only allow-listed fields; sensitive sync/Teams fields are never
        // selected, so they never appear in the markdown regardless of serialisation.
        var sessions = await _db.Sessions
            .Where(s => s.SeriesId == seriesId)
            .OrderBy(s => s.StartsAt)
            .Select(s => new
            {
                s.SessionId,
                s.Title,
                s.StartsAt,
                s.EndsAt,
                s.Status
                // Deliberately omitted: TeamsWebinarId, JoinWebUrl, DriftStatus,
                // ReconcileStatus, LastSyncAt, LastError, OwnerUserId, SeriesId
            })
            .ToListAsync(cancellationToken);

        // ── 3. Fetch presenters & coordinators (DisplayName only) ────────────────
        // Materialise all rows first, then group client-side to avoid EF Core
        // GroupBy-to-SQL translation ambiguity when projecting to a list.
        var sessionIds = sessions.Select(s => s.SessionId).ToList();

        // Presenters — project away Email and EntraUserId at the query level.
        var presenterRows = await _db.SessionPresenters
            .Where(p => sessionIds.Contains(p.SessionId))
            .Select(p => new { p.SessionId, p.DisplayName })   // Email / EntraUserId intentionally excluded
            .ToListAsync(cancellationToken);

        var presentersBySession = presenterRows
            .GroupBy(p => p.SessionId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.DisplayName).ToList());

        // Coordinators — same allow-list as presenters.
        var coordinatorRows = await _db.SessionCoordinators
            .Where(c => sessionIds.Contains(c.SessionId))
            .Select(c => new { c.SessionId, c.DisplayName })   // Email / EntraUserId intentionally excluded
            .ToListAsync(cancellationToken);

        var coordinatorsBySession = coordinatorRows
            .GroupBy(c => c.SessionId)
            .ToDictionary(g => g.Key, g => g.Select(c => c.DisplayName).ToList());

        // ── 4. Build the markdown document ──────────────────────────────────────
        var sb = new StringBuilder();

        // Document header
        sb.AppendLine($"# {series.Title}");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {series.Status}");
        sb.AppendLine($"**Created:** {series.CreatedAt:MMMM d, yyyy}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Sessions section
        sb.AppendLine("## Sessions");
        sb.AppendLine();

        if (sessions.Count == 0)
        {
            sb.AppendLine("*No sessions are currently defined for this series.*");
        }
        else
        {
            foreach (var session in sessions)
            {
                sb.AppendLine($"### {session.Title}");
                sb.AppendLine();

                // Schedule lines — omit Date/Time and replace with a single
                // "Not yet set" note when StartsAt has never been configured.
                var hasSchedule = session.StartsAt != default && session.StartsAt != DateTime.MinValue;

                if (hasSchedule)
                {
                    sb.AppendLine($"- **Date:** {session.StartsAt:MMMM d, yyyy}");
                    sb.AppendLine($"- **Time:** {session.StartsAt:h:mm tt} \u2013 {session.EndsAt:h:mm tt} (UTC)");
                }
                else
                {
                    sb.AppendLine("- **Schedule:** Not yet set");
                }

                sb.AppendLine($"- **Status:** {session.Status}");

                // Presenters — omit entire line when none are assigned.
                if (presentersBySession.TryGetValue(session.SessionId, out var presenters)
                    && presenters.Count > 0)
                {
                    sb.AppendLine($"- **Presenters:** {string.Join(", ", presenters)}");
                }

                // Coordinators — omit entire line when none are assigned.
                if (coordinatorsBySession.TryGetValue(session.SessionId, out var coordinators)
                    && coordinators.Count > 0)
                {
                    sb.AppendLine($"- **Coordinators:** {string.Join(", ", coordinators)}");
                }

                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }

        // Footer — capture the export timestamp once so it is consistent
        // even if the method is ever made more complex in the future.
        sb.AppendLine();
        sb.Append($"*Exported from EdgeFront Builder on {DateTime.UtcNow:MMMM d, yyyy}*");

        // ── 5. Generate the sanitized filename ──────────────────────────────────
        var fileName = FileNameSanitizer.Sanitize(series.Title);

        // ── 6. Return the result ─────────────────────────────────────────────────
        return new MarkdownExportResult(fileName, sb.ToString());
    }
}
