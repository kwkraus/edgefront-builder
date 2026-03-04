using EdgeFront.Builder.Common;
using EdgeFront.Builder.Common.Extensions;

namespace EdgeFront.Builder.Features.Metrics;

public static class MetricsEndpoints
{
    public static WebApplication MapMetricsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/series/{id:guid}/metrics",
            async (Guid id, MetricsService service, HttpContext ctx) =>
            {
                var userId = ctx.GetUserOid();
                if (userId is null)
                    return Results.Unauthorized();

                var result = await service.GetSeriesMetricsAsync(id, userId);
                return result is null
                    ? Results.NotFound(new ErrorEnvelope(
                        "metrics_not_found", "Series metrics not found.", ctx.TraceIdentifier))
                    : Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapGet("/api/v1/sessions/{id:guid}/metrics",
            async (Guid id, MetricsService service, HttpContext ctx) =>
            {
                var userId = ctx.GetUserOid();
                if (userId is null)
                    return Results.Unauthorized();

                var result = await service.GetSessionMetricsAsync(id, userId);
                return result is null
                    ? Results.NotFound(new ErrorEnvelope(
                        "metrics_not_found", "Session metrics not found.", ctx.TraceIdentifier))
                    : Results.Ok(result);
            })
            .RequireAuthorization();

        return app;
    }
}
