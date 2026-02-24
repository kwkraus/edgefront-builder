using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace EdgeFront.Builder.Infrastructure.Graph;

/// <summary>
/// Wraps Microsoft Graph SDK calls for virtual events (webinars), subscriptions,
/// registrations, and attendance.
/// App-credential calls use the injected <see cref="GraphServiceClient"/>.
/// OBO (delegated) calls build a short-lived client from the provided OBO token.
/// </summary>
public class TeamsGraphClient : ITeamsGraphClient
{
    private readonly GraphServiceClient _appClient;
    private readonly IConfiguration _config;

    public TeamsGraphClient(GraphServiceClient appGraphClient, IConfiguration config)
    {
        _appClient = appGraphClient;
        _config = config;
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    /// <summary>Builds a delegated <see cref="GraphServiceClient"/> from a raw OBO bearer token.</summary>
    private static GraphServiceClient BuildOboClient(string oboToken)
    {
        var tokenProvider = new StaticBearerTokenProvider(oboToken);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        var httpClient = GraphClientFactory.Create(authProvider);
        return new GraphServiceClient(httpClient);
    }

    private static bool IsLicenseError(ODataError err)
    {
        var code = err.Error?.Code ?? string.Empty;
        return code.Contains("Forbidden", StringComparison.OrdinalIgnoreCase)
            || code.Contains("licenseRequired", StringComparison.OrdinalIgnoreCase)
            || code.Contains("Authorization_RequestDenied", StringComparison.OrdinalIgnoreCase);
    }

    // ── OBO calls ───────────────────────────────────────────────────────────

    public async Task<string> CreateWebinarAsync(
        string title, DateTimeOffset startsAt, DateTimeOffset endsAt,
        string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        try
        {
            // TODO-SPEC-200: verify Graph API response shape
            var body = new Microsoft.Graph.Models.VirtualEventWebinar
            {
                DisplayName = title,
                StartDateTime = new Microsoft.Graph.Models.DateTimeTimeZone
                {
                    DateTime = startsAt.UtcDateTime.ToString("o"),
                    TimeZone = "UTC"
                },
                EndDateTime = new Microsoft.Graph.Models.DateTimeTimeZone
                {
                    DateTime = endsAt.UtcDateTime.ToString("o"),
                    TimeZone = "UTC"
                }
            };

            var result = await client.Solutions.VirtualEvents.Webinars.PostAsync(body, cancellationToken: ct);

            // TODO-SPEC-200: verify Graph API response shape for webinar ID field
            return result?.Id ?? throw new InvalidOperationException("Graph returned null webinar ID.");
        }
        catch (ODataError err) when (IsLicenseError(err))
        {
            throw new TeamsLicenseException($"Teams license check failed: {err.Error?.Code}");
        }
    }

    public async Task UpdateWebinarAsync(
        string teamsWebinarId, string title,
        DateTimeOffset startsAt, DateTimeOffset endsAt,
        string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        try
        {
            // TODO-SPEC-200: verify Graph API response shape for PATCH body
            var body = new Microsoft.Graph.Models.VirtualEventWebinar
            {
                DisplayName = title,
                StartDateTime = new Microsoft.Graph.Models.DateTimeTimeZone
                {
                    DateTime = startsAt.UtcDateTime.ToString("o"),
                    TimeZone = "UTC"
                },
                EndDateTime = new Microsoft.Graph.Models.DateTimeTimeZone
                {
                    DateTime = endsAt.UtcDateTime.ToString("o"),
                    TimeZone = "UTC"
                }
            };

            await client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
                .PatchAsync(body, cancellationToken: ct);
        }
        catch (ODataError err) when (IsLicenseError(err))
        {
            throw new TeamsLicenseException($"Teams license check failed: {err.Error?.Code}");
        }
    }

    public async Task DeleteWebinarAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        await client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
            .DeleteAsync(cancellationToken: ct);
    }

    // ── App-credential calls ─────────────────────────────────────────────────

    public async Task<TeamsWebinarInfo?> GetWebinarMetadataAsync(string teamsWebinarId, CancellationToken ct = default)
    {
        try
        {
            // TODO-SPEC-200: verify Graph API response shape for webinar metadata
            var result = await _appClient.Solutions.VirtualEvents.Webinars[teamsWebinarId]
                .GetAsync(cancellationToken: ct);

            if (result is null) return null;

            // Use DateTimeTimeZone extension helpers to convert
            var startsAt = result.StartDateTime is not null
                ? result.StartDateTime.ToDateTimeOffset()
                : DateTimeOffset.MinValue;

            var endsAt = result.EndDateTime is not null
                ? result.EndDateTime.ToDateTimeOffset()
                : DateTimeOffset.MinValue;

            return new TeamsWebinarInfo(
                result.Id ?? teamsWebinarId,
                result.DisplayName ?? string.Empty,
                startsAt,
                endsAt);
        }
        catch (ODataError err) when (err.ResponseStatusCode == 404)
        {
            return null;
        }
    }

    public async Task<string> CreateSubscriptionAsync(
        string resource, string changeType, string clientState,
        DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        var notificationUrl = _config["Graph:WebhookNotificationUrl"]
            ?? "https://localhost:7000/api/v1/webhooks/graph";

        // TODO-SPEC-200: verify Graph API response shape for subscription creation
        var body = new Microsoft.Graph.Models.Subscription
        {
            ChangeType = changeType,
            NotificationUrl = notificationUrl,
            Resource = resource,
            ExpirationDateTime = expiresAt,
            ClientState = clientState
        };

        var result = await _appClient.Subscriptions.PostAsync(body, cancellationToken: ct);

        return result?.Id ?? throw new InvalidOperationException("Graph returned null subscription ID.");
    }

    public async Task RenewSubscriptionAsync(
        string graphSubscriptionId, DateTimeOffset newExpiration, CancellationToken ct = default)
    {
        var body = new Microsoft.Graph.Models.Subscription
        {
            ExpirationDateTime = newExpiration
        };
        await _appClient.Subscriptions[graphSubscriptionId].PatchAsync(body, cancellationToken: ct);
    }

    public async Task DeleteSubscriptionAsync(string graphSubscriptionId, CancellationToken ct = default)
    {
        await _appClient.Subscriptions[graphSubscriptionId].DeleteAsync(cancellationToken: ct);
    }

    public async Task<IEnumerable<RegistrationRecord>> GetRegistrationsAsync(
        string teamsWebinarId, CancellationToken ct = default)
    {
        // TODO-SPEC-200: verify Graph API response shape for registrations
        var result = await _appClient.Solutions.VirtualEvents.Webinars[teamsWebinarId]
            .Registrations.GetAsync(cancellationToken: ct);

        if (result?.Value is null) return [];

        return result.Value
            .Where(r => r.Email is not null)
            .Select(r => new RegistrationRecord(
                r.Email!,
                r.RegistrationDateTime ?? DateTimeOffset.UtcNow));
    }

    public async Task<IEnumerable<AttendanceRecord>> GetAttendanceAsync(
        string teamsWebinarId, CancellationToken ct = default)
    {
        // TODO-SPEC-200: verify Graph API response shape for attendance (sessions → attendees)
        var sessionsResult = await _appClient.Solutions.VirtualEvents.Webinars[teamsWebinarId]
            .Sessions.GetAsync(cancellationToken: ct);

        if (sessionsResult?.Value is null) return [];

        var records = new List<AttendanceRecord>();

        foreach (var session in sessionsResult.Value)
        {
            if (session.Id is null) continue;

            var attendanceResult = await _appClient.Solutions.VirtualEvents
                .Webinars[teamsWebinarId].Sessions[session.Id]
                .AttendanceReports.GetAsync(cancellationToken: ct);

            if (attendanceResult?.Value is null) continue;

            foreach (var report in attendanceResult.Value)
            {
                if (report.Id is null) continue;

                var attendeesResult = await _appClient.Solutions.VirtualEvents
                    .Webinars[teamsWebinarId].Sessions[session.Id]
                    .AttendanceReports[report.Id].AttendanceRecords
                    .GetAsync(cancellationToken: ct);

                if (attendeesResult?.Value is null) continue;

                foreach (var a in attendeesResult.Value)
                {
                    var email = a.EmailAddress;
                    if (string.IsNullOrWhiteSpace(email)) continue;

                    var attended = a.AttendanceIntervals?.Count > 0;
                    var totalDurationSeconds = a.AttendanceIntervals?
                        .Sum(i => (int?)i.DurationInSeconds) ?? 0;

                    records.Add(new AttendanceRecord(
                        email,
                        attended,
                        totalDurationSeconds > 0 ? totalDurationSeconds : null,
                        null, // DurationPercent: TODO-SPEC-200 — compute from session duration
                        null, // FirstJoinAt: TODO-SPEC-200 — derive from attendance intervals
                        null  // LastLeaveAt: TODO-SPEC-200 — derive from attendance intervals
                    ));
                }
            }
        }

        return records;
    }
}

/// <summary>
/// Kiota token provider that returns a static pre-acquired OBO bearer token.
/// </summary>
internal sealed class StaticBearerTokenProvider : IAccessTokenProvider
{
    private readonly string _token;

    public StaticBearerTokenProvider(string token) => _token = token;

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_token);

    public AllowedHostsValidator AllowedHostsValidator { get; } = new();
}
