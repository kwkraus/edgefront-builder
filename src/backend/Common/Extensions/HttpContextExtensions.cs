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

    /// <summary>
    /// Returns the current user's display name from the JWT claims.
    /// Falls back to preferred_username or empty string.
    /// </summary>
    public static string GetUserDisplayName(this HttpContext context) =>
        context.User.FindFirst("name")?.Value
        ?? context.User.FindFirst(ClaimTypes.Name)?.Value
        ?? context.User.FindFirst("preferred_username")?.Value
        ?? string.Empty;
}
