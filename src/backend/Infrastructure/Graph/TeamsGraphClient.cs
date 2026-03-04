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

    public async Task<string> CreateWebinarAsync(
        string title, DateTimeOffset startsAt, DateTimeOffset endsAt,
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

            var result = await client.Solutions.VirtualEvents.Webinars.PostAsync(body, cancellationToken: ct);
            return result?.Id ?? throw new InvalidOperationException("Graph returned null webinar ID.");
        }
        catch (ODataError err) when (IsLicenseError(err))
        {
            throw new TeamsLicenseException($"Teams license check failed: {err.Error?.Code}");
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

    public async Task<TeamsWebinarInfo?> GetWebinarMetadataAsync(
        string teamsWebinarId, string oboToken, CancellationToken ct = default)
    {
        var client = BuildOboClient(oboToken);
        try
        {
            var result = await client.Solutions.VirtualEvents.Webinars[teamsWebinarId]
                .GetAsync(cancellationToken: ct);

            if (result is null) return null;

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
