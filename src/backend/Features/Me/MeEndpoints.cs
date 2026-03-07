using EdgeFront.Builder.Common.Extensions;
using EdgeFront.Builder.Infrastructure.Graph;

namespace EdgeFront.Builder.Features.Me;

public static class MeEndpoints
{
    public static WebApplication MapMeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/me").RequireAuthorization();

        group.MapGet("/photo", async (IOboTokenService oboService, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var oboToken = await TryGetOboTokenAsync(ctx, oboService);
            if (string.IsNullOrEmpty(oboToken))
                return Results.StatusCode(502);

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", oboToken);

                var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me/photo/$value");

                if (!response.IsSuccessStatusCode)
                    return Results.NoContent();

                var photoBytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

                return Results.File(photoBytes, contentType);
            }
            catch
            {
                return Results.NoContent();
            }
        });

        return app;
    }

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
