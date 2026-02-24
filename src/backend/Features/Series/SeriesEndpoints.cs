using EdgeFront.Builder.Common;
using EdgeFront.Builder.Common.Extensions;
using EdgeFront.Builder.Features.Series.Dtos;

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

        group.MapPost("/{id:guid}/publish", async (Guid id, SeriesService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var (series, errorCode) = await service.PublishAsync(id, userId);
            if (series is null)
                return Results.NotFound(new ErrorEnvelope(
                    errorCode ?? "series_not_found", "Series not found.", ctx.TraceIdentifier));

            return Results.Ok(series);
        });

        return app;
    }
}
