using System.Security.Cryptography;
using System.Text;
using EdgeFront.Builder.Features.Webhook.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Features.Webhook;

public class WebhookService
{
    private readonly AppDbContext _db;
    private readonly ILogger<WebhookService> _logger;
    private readonly WebhookIngestionService? _ingestionService;

    public WebhookService(AppDbContext db, ILogger<WebhookService> logger,
        WebhookIngestionService? ingestionService = null)
    {
        _db = db;
        _logger = logger;
        _ingestionService = ingestionService;
    }

    /// <summary>
    /// Validates the provided clientState against the stored SHA-256 hash for the subscription.
    /// Returns true if the hash matches, false if the subscription is unknown or the hash mismatches.
    /// </summary>
    public async Task<bool> ValidateClientStateAsync(string subscriptionId, string clientState)
    {
        var subscription = await _db.GraphSubscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

        if (subscription is null)
            return false;

        var hash = ComputeSha256Hex(clientState);
        return string.Equals(hash, subscription.ClientStateHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Dispatches Graph change notifications to <see cref="WebhookIngestionService"/>
    /// based on the changeType of each notification (SPEC-200 §3).
    /// </summary>
    public async Task HandleAsync(GraphNotificationEnvelope notification, string correlationId)
    {
        _logger.LogInformation(
            "WebhookService.HandleAsync called. CorrelationId={CorrelationId}, NotificationCount={Count}",
            correlationId,
            notification.Value.Count);

        if (_ingestionService is null)
        {
            _logger.LogDebug(
                "WebhookIngestionService not configured; skipping ingestion. CorrelationId={CorrelationId}",
                correlationId);
            return;
        }

        foreach (var item in notification.Value)
        {
            // Extract the teamsWebinarId from the resource path
            // Resource format: solutions/virtualEvents/webinars/{id}/registrations (or /attendanceReports)
            var teamsWebinarId = ExtractWebinarId(item.Resource);
            if (teamsWebinarId is null)
            {
                _logger.LogWarning(
                    "Could not extract TeamsWebinarId from resource '{Resource}'. CorrelationId={CorrelationId}",
                    item.Resource, correlationId);
                continue;
            }

            if (item.ChangeType.Contains("attendanceReport", StringComparison.OrdinalIgnoreCase)
                || item.Resource.Contains("attendanceReport", StringComparison.OrdinalIgnoreCase))
            {
                await _ingestionService.HandleAttendanceReportAsync(teamsWebinarId, correlationId);
            }
            else
            {
                await _ingestionService.HandleRegistrationAsync(teamsWebinarId, correlationId);
            }
        }
    }

    /// <summary>
    /// Extracts the webinar ID from a Graph resource path such as
    /// <c>solutions/virtualEvents/webinars/{id}/registrations</c>.
    /// </summary>
    private static string? ExtractWebinarId(string resource)
    {
        const string marker = "webinars/";
        var idx = resource.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var after = resource[(idx + marker.Length)..];
        var slash = after.IndexOf('/');
        return slash >= 0 ? after[..slash] : after;
    }

    /// <summary>
    /// Computes the lowercase hex-encoded SHA-256 hash of the input string (UTF-8 encoded).
    /// </summary>
    public static string ComputeSha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
