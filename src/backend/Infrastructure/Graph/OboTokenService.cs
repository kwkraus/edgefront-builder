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
        // VirtualEvent.ReadWrite: webinar CRUD + registrations
        // OnlineMeetingArtifact.Read.All: attendance reports
        // TODO-SPEC: SPEC-200 lists only VirtualEvent.ReadWrite but Graph requires
        //   OnlineMeetingArtifact.Read.All for attendance report access.
        var token = await _tokenAcquisition.GetAccessTokenForUserAsync(
            new[]
            {
                "https://graph.microsoft.com/VirtualEvent.ReadWrite",
                "https://graph.microsoft.com/OnlineMeetingArtifact.Read.All"
            });
        return token;
    }
}
