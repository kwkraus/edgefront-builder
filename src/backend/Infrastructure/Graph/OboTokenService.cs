using Microsoft.Identity.Web;

namespace EdgeFront.Builder.Infrastructure.Graph;

public interface IOboTokenService
{
    Task<string> GetOboTokenAsync(string userAccessToken, IReadOnlyCollection<string> scopes, CancellationToken ct = default);
}

public class OboTokenService : IOboTokenService
{
    private readonly ITokenAcquisition _tokenAcquisition;

    public OboTokenService(ITokenAcquisition tokenAcquisition)
        => _tokenAcquisition = tokenAcquisition;

    public async Task<string> GetOboTokenAsync(string userAccessToken, IReadOnlyCollection<string> scopes, CancellationToken ct = default)
    {
        var token = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        return token;
    }
}
