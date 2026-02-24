using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Infrastructure.Background;

/// <summary>
/// Background service that renews Graph subscriptions before they expire.
/// Runs on a 1-hour cadence and targets subscriptions expiring within 24 hours.
///
/// SPEC-200 §4 (subscription lifecycle).
/// </summary>
public class SubscriptionRenewalService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionRenewalService> _logger;

    public SubscriptionRenewalService(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionRenewalService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionRenewalService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoRenewalAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in subscription renewal loop.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("SubscriptionRenewalService stopping.");
    }

    private async Task DoRenewalAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var graphClient = scope.ServiceProvider.GetRequiredService<ITeamsGraphClient>();

        var window = DateTime.UtcNow.AddHours(24);
        var expiring = await db.GraphSubscriptions
            .Where(s => s.ExpirationDateTime <= window)
            .ToListAsync(ct);

        if (expiring.Count == 0)
        {
            _logger.LogDebug("No subscriptions expiring within 24 hours.");
            return;
        }

        _logger.LogInformation(
            "Renewing {Count} expiring subscriptions.", expiring.Count);

        foreach (var sub in expiring)
        {
            var newExpiration = DateTimeOffset.UtcNow.AddDays(2);

            try
            {
                await graphClient.RenewSubscriptionAsync(sub.SubscriptionId, newExpiration, ct);

                sub.ExpirationDateTime = newExpiration.UtcDateTime;
                _logger.LogInformation(
                    "Renewed subscription {SubscriptionId} for session {SessionId}.",
                    sub.SubscriptionId, sub.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to renew subscription {SubscriptionId} for session {SessionId}.",
                    sub.SubscriptionId, sub.SessionId);

                // If already past the expiry window, disable the session reconciliation
                if (sub.ExpirationDateTime < DateTime.UtcNow)
                {
                    var session = await db.Sessions.FindAsync([sub.SessionId], ct);
                    if (session is not null)
                    {
                        session.ReconcileStatus = ReconcileStatus.Disabled;
                        _logger.LogWarning(
                            "Session {SessionId} reconciliation disabled due to expired subscription.",
                            sub.SessionId);
                    }
                }
                else
                {
                    var session = await db.Sessions.FindAsync([sub.SessionId], ct);
                    if (session is not null)
                    {
                        session.ReconcileStatus = ReconcileStatus.Retrying;
                        session.LastError = ex.Message;
                    }
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
