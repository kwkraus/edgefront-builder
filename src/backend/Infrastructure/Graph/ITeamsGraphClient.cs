namespace EdgeFront.Builder.Infrastructure.Graph;

/// <summary>
/// All operations use delegated (OBO) tokens — no application credentials.
/// </summary>
public interface ITeamsGraphClient
{
    // Webinar lifecycle (delegated)
    Task<CreateWebinarResult> CreateWebinarAsync(string title, DateTimeOffset startsAt, DateTimeOffset endsAt, string oboToken, CancellationToken ct = default);
    Task PublishWebinarAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);
    Task UpdateWebinarAsync(string teamsWebinarId, string title, DateTimeOffset startsAt, DateTimeOffset endsAt, string oboToken, CancellationToken ct = default);
    Task DeleteWebinarAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);

    // Read operations (delegated)
    Task<TeamsWebinarInfo?> GetWebinarMetadataAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);
    Task<IEnumerable<RegistrationRecord>> GetRegistrationsAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);
    Task<IEnumerable<AttendanceRecord>> GetAttendanceAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);

    // People search (delegated — User.ReadBasic.All)
    Task<IEnumerable<PersonSearchResult>> SearchUsersAsync(string query, string oboToken, CancellationToken ct = default);

    // Presenter/co-organizer management (delegated — VirtualEvent.ReadWrite)
    Task<IEnumerable<TeamsPresenterInfo>> GetWebinarPresentersAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);
    Task<IEnumerable<TeamsCoOrganizerInfo>> GetWebinarCoOrganizersAsync(string teamsWebinarId, string oboToken, CancellationToken ct = default);
    Task AddWebinarPresenterAsync(string teamsWebinarId, string entraUserId, string tenantId, string oboToken, CancellationToken ct = default);
    Task RemoveWebinarPresenterAsync(string teamsWebinarId, string presenterId, string oboToken, CancellationToken ct = default);
    Task SetWebinarCoOrganizersAsync(string teamsWebinarId, IEnumerable<string> entraUserIds, string oboToken, CancellationToken ct = default);

    // User info lookup (delegated — User.ReadBasic.All)
    Task<TeamsUserInfo?> GetUserInfoAsync(string entraUserId, string oboToken, CancellationToken ct = default);
}
