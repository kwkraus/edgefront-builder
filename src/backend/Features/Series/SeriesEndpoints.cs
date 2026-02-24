using EdgeFront.Builder.Common;
using EdgeFront.Builder.Common.Extensions;
using EdgeFront.Builder.Features.Series.Dtos;
using EdgeFront.Builder.Infrastructure.Graph;

namespace EdgeFront.Builder.Features.Series;

public static class SeriesEndpoints
{
    public static WebApplication MapSeriesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/series").RequireAuthorization();

        group.MapGet("/", async (SeriesService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var result = await service.GetAllAsync(userId);
            return Results.Ok(result);
        });

        group.MapPost("/", async (CreateSeriesRequest req, SeriesService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest(new ErrorEnvelope(
                    "validation_error", "Title is required.", ctx.TraceIdentifier));

            var result = await service.CreateAsync(req, userId);
            return Results.Created($"/api/v1/series/{result.SeriesId}", result);
        });

        group.MapGet("/{id:guid}", async (Guid id, SeriesService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var result = await service.GetByIdAsync(id, userId);
            return result is null
                ? Results.NotFound(new ErrorEnvelope(
                    "series_not_found", "Series not found.", ctx.TraceIdentifier))
                : Results.Ok(result);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateSeriesRequest req, SeriesService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest(new ErrorEnvelope(
                    "validation_error", "Title is required.", ctx.TraceIdentifier));

            var result = await service.UpdateAsync(id, req, userId);
            return result is null
                ? Results.NotFound(new ErrorEnvelope(
                    "series_not_found", "Series not found.", ctx.TraceIdentifier))
                : Results.Ok(result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, SeriesService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var deleted = await service.DeleteAsync(id, userId);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new ErrorEnvelope(
                    "series_not_found", "Series not found.", ctx.TraceIdentifier));
        });

        group.MapPost("/{id:guid}/publish", async (Guid id, SeriesService service, ITeamsGraphClient graphClient, IOboTokenService oboService, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SeriesEndpoints");
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            // Per SPEC-200: exchange the incoming Bearer token for a Graph-scoped OBO
            // token so delegated Graph calls (webinar create/subscribe) succeed.
            var authHeader = ctx.Request.Headers.Authorization.ToString();
            var rawToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..]
                : string.Empty;

            string? oboToken = null;
            if (!string.IsNullOrEmpty(rawToken))
            {
                try
                {
                    oboToken = await oboService.GetOboTokenAsync(rawToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "OBO token exchange failed for Graph API. SeriesId={SeriesId}", id);
                    return Results.UnprocessableEntity(new ErrorEnvelope(
                        "OBO_EXCHANGE_FAILED",
                        "Could not acquire a Graph API token. Verify the Entra ID app registration has the VirtualEvent.ReadWrite delegated permission with admin consent.",
                        ctx.TraceIdentifier));
                }
            }

            var (series, errorCode) = await service.PublishAsync(id, userId, oboToken, graphClient, logger);
            if (series is null)
            {
                if (errorCode == "series_not_found")
                    return Results.NotFound(new ErrorEnvelope(
                        errorCode, "Series not found.", ctx.TraceIdentifier));

                return Results.UnprocessableEntity(new ErrorEnvelope(
                    errorCode ?? "PUBLISH_FAILED", "Publish failed.", ctx.TraceIdentifier));
            }

            return Results.Ok(series);
        });

        return app;
    }
}
