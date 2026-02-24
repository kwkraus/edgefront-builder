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

    public WebhookService(AppDbContext db, ILogger<WebhookService> logger)
    {
        _db = db;
        _logger = logger;
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
    /// Stub handler for Graph change notifications.
    /// TODO-SPEC-200: implement in Phase 3 — process registration/attendance change notifications.
    /// </summary>
    public Task HandleAsync(GraphNotificationEnvelope notification, string correlationId)
    {
        _logger.LogInformation(
            "WebhookService.HandleAsync called. CorrelationId={CorrelationId}, NotificationCount={Count}",
            correlationId,
            notification.Value.Count);

        // TODO-SPEC-200: implement in Phase 3 — reconcile registrations and attendance
        return Task.CompletedTask;
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
