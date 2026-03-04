using System.Security.Claims;

namespace EdgeFront.Builder.Common.Extensions;

public static class HttpContextExtensions
{
    private const string OidClaimType = "oid";
    private const string OidAltClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>
    /// Returns the current user's OID (Object ID) from the JWT claims.
    /// Returns null if the claim is not present.
    /// </summary>
    public static string? GetUserOid(this HttpContext context) =>
        context.User.FindFirst(OidClaimType)?.Value
        ?? context.User.FindFirst(OidAltClaimType)?.Value;
}
