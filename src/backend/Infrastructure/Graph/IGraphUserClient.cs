namespace EdgeFront.Builder.Infrastructure.Graph;

/// <summary>
/// Delegated (OBO) Microsoft Graph access for directory user lookups.
/// All operations use delegated tokens — no application credentials.
/// </summary>
public interface IGraphUserClient
{
    // People search (delegated — User.ReadBasic.All)
    Task<IEnumerable<PersonSearchResult>> SearchUsersAsync(string query, string oboToken, CancellationToken ct = default);
}
