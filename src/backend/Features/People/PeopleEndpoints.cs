using EdgeFront.Builder.Common;
using EdgeFront.Builder.Common.Extensions;
using EdgeFront.Builder.Infrastructure.Graph;

namespace EdgeFront.Builder.Features.People;

public static class PeopleEndpoints
{
    public static WebApplication MapPeopleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/people").RequireAuthorization();

        group.MapGet("/search", async (string? q, ITeamsGraphClient graphClient, IOboTokenService oboService, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Results.BadRequest(new ErrorEnvelope(
                    "validation_error", "Query parameter 'q' must be at least 2 characters.", ctx.TraceIdentifier));

            var oboToken = await TryGetOboTokenAsync(ctx, oboService);
            if (string.IsNullOrEmpty(oboToken))
            {
                return Results.UnprocessableEntity(new ErrorEnvelope(
                    "OBO_EXCHANGE_FAILED",
                    "Could not acquire a Graph API token. Verify the Entra ID app registration has the User.ReadBasic.All delegated permission with admin consent.",
                    ctx.TraceIdentifier));
            }

            var results = await graphClient.SearchUsersAsync(q.Trim(), oboToken);
            return Results.Ok(results);
        });

        return app;
    }

    /// <summary>
    /// Best-effort extraction of OBO token from the request's Bearer token.
    /// Returns null if the token exchange fails (e.g. no Graph consent).
    /// </summary>
    private static async Task<string?> TryGetOboTokenAsync(HttpContext ctx, IOboTokenService oboService)
    {
        var authHeader = ctx.Request.Headers.Authorization.ToString();
        var rawToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..]
            : string.Empty;

        if (string.IsNullOrEmpty(rawToken))
            return null;

        try
        {
            return await oboService.GetOboTokenAsync(rawToken);
        }
        catch
        {
            return null;
        }
    }
}
