using Microsoft.Identity.Web;

namespace EdgeFront.Builder.Infrastructure.Graph;

public interface IOboTokenService
{
    Task<string> GetOboTokenAsync(string userAccessToken, CancellationToken ct = default);
}

public class OboTokenService : IOboTokenService
{
    private readonly ITokenAcquisition _tokenAcquisition;

    public OboTokenService(ITokenAcquisition tokenAcquisition)
        => _tokenAcquisition = tokenAcquisition;

    public async Task<string> GetOboTokenAsync(string userAccessToken, CancellationToken ct = default)
    {
        // User.ReadBasic.All: people search
        // User.Read: profile photo access
        var token = await _tokenAcquisition.GetAccessTokenForUserAsync(
            new[]
            {
                "https://graph.microsoft.com/User.ReadBasic.All",
                "https://graph.microsoft.com/User.Read"
            });
        return token;
    }
}
