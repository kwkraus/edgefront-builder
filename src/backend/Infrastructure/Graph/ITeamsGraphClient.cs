namespace EdgeFront.Builder.Infrastructure.Graph;

public interface ITeamsGraphClient
{
    // OBO (delegated) — user must be present
    Task<string> CreateWebinarAsync(string title, DateTimeOffset startsAt, DateTimeOffset endsAt, string oboToken, CancellationToken ct = default);
    Task UpdateWebinarAsync(string teamsWebinarId, string title, DateTimeOffset startsAt, DateTimeOffset endsAt, string oboToken, CancellationToken ct = default);
    Task DeleteWebinarAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);

    // Application (client credentials)
    Task<TeamsWebinarInfo?> GetWebinarMetadataAsync(string teamsWebinarId, CancellationToken ct = default);
    Task<string> CreateSubscriptionAsync(string resource, string changeType, string clientState, DateTimeOffset expiresAt, CancellationToken ct = default);
    Task RenewSubscriptionAsync(string graphSubscriptionId, DateTimeOffset newExpiration, CancellationToken ct = default);
    Task DeleteSubscriptionAsync(string graphSubscriptionId, CancellationToken ct = default);
    Task<IEnumerable<RegistrationRecord>> GetRegistrationsAsync(string teamsWebinarId, CancellationToken ct = default);
    Task<IEnumerable<AttendanceRecord>> GetAttendanceAsync(string teamsWebinarId, CancellationToken ct = default);
}
