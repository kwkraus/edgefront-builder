using Azure.Core;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace EdgeFront.Builder.Infrastructure.Graph;

/// <summary>
/// Wraps Microsoft Graph SDK calls for virtual events (webinars), registrations,
/// and attendance. All calls use delegated (OBO) tokens — no application credentials.
/// </summary>
public class TeamsGraphClient : ITeamsGraphClient
{
    private const int MaxRetries = 2;
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];

    /// <summary>Retries an async operation on transient HttpRequestException (e.g. HTTP/2 INTERNAL_ERROR).</summary>
    private static async Task<T> WithTransientRetryAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                await Task.Delay(RetryDelays[attempt], ct);
            }
        }
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

    // ── Webinar lifecycle ───────────────────────────────────────────────────

    public async Task<CreateWebinarResult> CreateWebinarAsync(
        string title, DateTimeOffset startsAt, DateTimeOffset endsAt,
        string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        try
        {
            var body = new VirtualEventWebinar
            {
                DisplayName = title,
                Audience = MeetingAudience.Organization,
                StartDateTime = new DateTimeTimeZone
                {
                    DateTime = startsAt.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
                    TimeZone = "UTC"
                },
                EndDateTime = new DateTimeTimeZone
                {
                    DateTime = endsAt.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
                    TimeZone = "UTC"
                }
            };

            var result = await client.Solutions.VirtualEvents.Webinars.PostAsync(body, cancellationToken: ct);
            var id = result?.Id ?? throw new InvalidOperationException("Graph returned null webinar ID.");
            var joinWebUrl = ResolveWebinarUrl(result);
            return new CreateWebinarResult(id, joinWebUrl);
        }
        catch (ODataError err) when (IsLicenseError(err))
        {
            throw new TeamsLicenseException($"Teams license check failed: {err.Error?.Code}");
        }
        catch (ODataError err)
        {
            var code = err.Error?.Code ?? "unknown";
            var message = err.Error?.Message ?? err.Message;
            var innerCode = err.Error?.InnerError?.AdditionalData?.ContainsKey("code") == true
                ? err.Error.InnerError.AdditionalData["code"]?.ToString()
                : null;
            throw new InvalidOperationException(
                $"Graph CreateWebinar failed: Code={code}, Message={message}, InnerCode={innerCode}, " +
                $"Start={startsAt:O}, End={endsAt:O}, Title={title}", err);
        }
    }

    public async Task PublishWebinarAsync(
        string teamsWebinarId, string oboToken, CancellationToken ct = default)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", oboToken);
        var response = await httpClient.PostAsync(
            $"https://graph.microsoft.com/v1.0/solutions/virtualEvents/webinars/{teamsWebinarId}/publish",
            null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateWebinarAsync(
        string teamsWebinarId, string title,
        DateTimeOffset startsAt, DateTimeOffset endsAt,
        string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        try
        {
            var body = new VirtualEventWebinar
            {
                DisplayName = title,
                StartDateTime = new DateTimeTimeZone
                {
                    DateTime = startsAt.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
                    TimeZone = "UTC"
                },
                EndDateTime = new DateTimeTimeZone
                {
                    DateTime = endsAt.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
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

    // ── Read operations (delegated) ─────────────────────────────────────────

    /// <summary>Safely parses a <see cref="DateTimeTimeZone"/> without relying on the SDK's
    /// <c>ToDateTimeOffset</c> which uses <c>ParseExact</c> and fails on datetime strings
    /// that lack fractional seconds (e.g. "2026-03-05T22:00:00").</summary>
    private static DateTimeOffset ParseDateTimeTimeZone(DateTimeTimeZone? dtz)
    {
        if (dtz?.DateTime is null) return DateTimeOffset.MinValue;

        if (DateTimeOffset.TryParse(dtz.DateTime, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
            return parsed;

        return DateTimeOffset.MinValue;
    }

    /// <summary>
    /// Resolves the best available URL for a webinar. Prefers <c>joinWebUrl</c> from the
    /// base VirtualEvent type (the Teams deep link), then <c>registrationWebUrl</c> from the
    /// registration config, then constructs the standard Teams event page URL from the webinar ID.
    /// </summary>
    private static string? ResolveWebinarUrl(VirtualEventWebinar webinar)
    {
        // 1. Try joinWebUrl on the webinar itself (AdditionalData — not typed in SDK v5.68)
        if (webinar.AdditionalData?.TryGetValue("joinWebUrl", out var joinObj) == true
            && joinObj is string joinUrl && !string.IsNullOrWhiteSpace(joinUrl))
            return joinUrl;

        // 2. Try registrationWebUrl from the typed RegistrationConfiguration
        if (webinar.RegistrationConfiguration?.AdditionalData?
                .TryGetValue("registrationWebUrl", out var regObj) == true
            && regObj is string regUrl && !string.IsNullOrWhiteSpace(regUrl))
            return regUrl;

        // 3. Construct the standard Teams event page URL from the webinar ID
        if (!string.IsNullOrWhiteSpace(webinar.Id))
            return $"https://events.teams.microsoft.com/event/{webinar.Id}";

        return null;
    }

    public async Task<TeamsWebinarInfo?> GetWebinarMetadataAsync(
        string teamsWebinarId, string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        try
        {
            var result = await client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
                .GetAsync(cancellationToken: ct);

            if (result is null) return null;

            var startsAt = ParseDateTimeTimeZone(result.StartDateTime);
            var endsAt = ParseDateTimeTimeZone(result.EndDateTime);

            var joinWebUrl = ResolveWebinarUrl(result);

            return new TeamsWebinarInfo(
                result.Id ?? teamsWebinarId,
                result.DisplayName ?? string.Empty,
                startsAt,
                endsAt,
                joinWebUrl);
        }
        catch (ODataError err) when (err.ResponseStatusCode == 404)
        {
            return null;
        }
    }

    public async Task<IEnumerable<RegistrationRecord>> GetRegistrationsAsync(
        string teamsWebinarId, string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        var result = await WithTransientRetryAsync(
            () => client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
                .Registrations.GetAsync(cancellationToken: ct), ct);

        if (result?.Value is null) return [];

        return result.Value
            .Where(r => r.Email is not null)
            .Select(r => new RegistrationRecord(
                r.Email!,
                r.RegistrationDateTime ?? DateTimeOffset.UtcNow));
    }

    public async Task<IEnumerable<AttendanceRecord>> GetAttendanceAsync(
        string teamsWebinarId, string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        var sessionsResult = await WithTransientRetryAsync(
            () => client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
                .Sessions.GetAsync(cancellationToken: ct), ct);

        if (sessionsResult?.Value is null) return [];

        var records = new List<AttendanceRecord>();

        foreach (var session in sessionsResult.Value)
        {
            if (session.Id is null) continue;

            Microsoft.Graph.Models.MeetingAttendanceReportCollectionResponse? attendanceResult;
            try
            {
                attendanceResult = await WithTransientRetryAsync(
                    () => client.Solutions.VirtualEvents
                        .Webinars[teamsWebinarId].Sessions[session.Id]
                        .AttendanceReports.GetAsync(cancellationToken: ct), ct);
            }
            catch (ODataError ex) when (ex.ResponseStatusCode == 404)
            {
                // No attendance reports yet (webinar hasn't occurred) — skip
                continue;
            }

            if (attendanceResult?.Value is null) continue;

            foreach (var report in attendanceResult.Value)
            {
                if (report.Id is null) continue;

                var attendeesResult = await WithTransientRetryAsync(
                    () => client.Solutions.VirtualEvents
                        .Webinars[teamsWebinarId].Sessions[session.Id]
                        .AttendanceReports[report.Id].AttendanceRecords
                        .GetAsync(cancellationToken: ct), ct);

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
                        null,
                        null,
                        null
                    ));
                }
            }
        }

        return records;
    }

    // ── People search ───────────────────────────────────────────────────────

    public async Task<IEnumerable<PersonSearchResult>> SearchUsersAsync(
        string query, string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        var result = await WithTransientRetryAsync(
            () => client.Users.GetAsync(r =>
            {
                r.QueryParameters.Filter = $"startswith(displayName,'{EscapeODataString(query)}')";
                r.QueryParameters.Select = ["id", "displayName", "mail", "userPrincipalName"];
                r.QueryParameters.Top = 10;
            }, cancellationToken: ct),
            ct);

        if (result?.Value is null) return [];

        return result.Value
            .Select(u => new PersonSearchResult(
                u.Id ?? string.Empty,
                u.DisplayName ?? string.Empty,
                u.Mail ?? u.UserPrincipalName ?? string.Empty))
            .Where(p => !string.IsNullOrEmpty(p.EntraUserId));
    }

    // ── Presenter/co-organizer management ───────────────────────────────────

    public async Task<IEnumerable<TeamsPresenterInfo>> GetWebinarPresentersAsync(
        string teamsWebinarId, string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        var result = await WithTransientRetryAsync(
            () => client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
                .Presenters.GetAsync(cancellationToken: ct), ct);

        if (result?.Value is null) return [];

        return result.Value
            .Where(p => p.Id is not null)
            .Select(p =>
            {
                var entraUserId = string.Empty;
                var displayName = string.Empty;
                if (p.Identity is Microsoft.Graph.Models.CommunicationsUserIdentity userIdentity)
                {
                    entraUserId = userIdentity.Id ?? string.Empty;
                    displayName = userIdentity.DisplayName ?? string.Empty;
                }
                return new TeamsPresenterInfo(p.Id!, entraUserId, displayName);
            });
    }

    public async Task AddWebinarPresenterAsync(
        string teamsWebinarId, string entraUserId, string tenantId,
        string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        var presenter = new VirtualEventPresenter
        {
            Identity = new Microsoft.Graph.Models.CommunicationsUserIdentity
            {
                OdataType = "#microsoft.graph.communicationsUserIdentity",
                Id = entraUserId,
                TenantId = tenantId
            }
        };

        await client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
            .Presenters.PostAsync(presenter, cancellationToken: ct);
    }

    public async Task RemoveWebinarPresenterAsync(
        string teamsWebinarId, string presenterId,
        string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        await client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
            .Presenters[presenterId].DeleteAsync(cancellationToken: ct);
    }

    public async Task SetWebinarCoOrganizersAsync(
        string teamsWebinarId, IEnumerable<string> entraUserIds,
        string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        var body = new VirtualEventWebinar
        {
            CoOrganizers = entraUserIds.Select(id => new Microsoft.Graph.Models.CommunicationsUserIdentity
            {
                OdataType = "#microsoft.graph.communicationsUserIdentity",
                Id = id
            }).ToList()
        };

        await client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
            .PatchAsync(body, cancellationToken: ct);
    }

    public async Task<IEnumerable<TeamsCoOrganizerInfo>> GetWebinarCoOrganizersAsync(
        string teamsWebinarId, string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        try
        {
            var result = await WithTransientRetryAsync(
                () => client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
                    .GetAsync(cancellationToken: ct), ct);

            if (result?.CoOrganizers is null) return [];

            return result.CoOrganizers
                .Where(c => c.Id is not null && !string.IsNullOrEmpty(c.Id))
                .Select(c => new TeamsCoOrganizerInfo(
                    c.Id!,
                    c.DisplayName ?? string.Empty));
        }
        catch (ODataError err) when (err.ResponseStatusCode == 404)
        {
            return [];
        }
    }

    public async Task<TeamsUserInfo?> GetUserInfoAsync(
        string entraUserId, string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        try
        {
            var user = await client.Users[entraUserId]
                .GetAsync(r =>
                {
                    r.QueryParameters.Select = ["id", "displayName", "mail", "userPrincipalName"];
                }, cancellationToken: ct);

            if (user is null) return null;

            return new TeamsUserInfo(
                user.Id ?? entraUserId,
                user.DisplayName ?? string.Empty,
                user.Mail ?? user.UserPrincipalName ?? string.Empty);
        }
        catch (ODataError err) when (err.ResponseStatusCode == 404)
        {
            return null;
        }
    }

    /// <summary>Escapes single quotes in OData filter strings.</summary>
    private static string EscapeODataString(string value)
        => value.Replace("'", "''");
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
