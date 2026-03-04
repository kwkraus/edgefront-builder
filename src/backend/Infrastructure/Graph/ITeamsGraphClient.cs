namespace EdgeFront.Builder.Infrastructure.Graph;

/// <summary>
/// All operations use delegated (OBO) tokens — no application credentials.
/// </summary>
public interface ITeamsGraphClient
{
    // Webinar lifecycle (delegated)
    Task<string> CreateWebinarAsync(string title, DateTimeOffset startsAt, DateTimeOffset endsAt, string oboToken, CancellationToken ct = default);
    Task PublishWebinarAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);
    Task UpdateWebinarAsync(string teamsWebinarId, string title, DateTimeOffset startsAt, DateTimeOffset endsAt, string oboToken, CancellationToken ct = default);
    Task DeleteWebinarAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);

    // Read operations (delegated)
    Task<TeamsWebinarInfo?> GetWebinarMetadataAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);
    Task<IEnumerable<RegistrationRecord>> GetRegistrationsAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);
    Task<IEnumerable<AttendanceRecord>> GetAttendanceAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);
}
