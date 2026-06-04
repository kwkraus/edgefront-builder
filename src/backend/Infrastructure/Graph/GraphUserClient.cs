using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace EdgeFront.Builder.Infrastructure.Graph;

/// <summary>
/// Wraps Microsoft Graph SDK calls for delegated directory user lookups.
/// All calls use delegated (OBO) tokens — no application credentials.
/// </summary>
public class GraphUserClient : IGraphUserClient
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

    /// <summary>Builds a delegated <see cref="GraphServiceClient"/> from a raw OBO bearer token.</summary>
    private static GraphServiceClient BuildOboClient(string oboToken)
    {
        var tokenProvider = new StaticBearerTokenProvider(oboToken);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        var httpClient = GraphClientFactory.Create(authProvider);
        return new GraphServiceClient(httpClient);
    }

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
