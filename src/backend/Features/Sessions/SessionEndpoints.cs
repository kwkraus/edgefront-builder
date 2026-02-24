using EdgeFront.Builder.Common;
using EdgeFront.Builder.Common.Extensions;
using EdgeFront.Builder.Features.Sessions.Dtos;

namespace EdgeFront.Builder.Features.Sessions;

public static class SessionEndpoints
{
    public static WebApplication MapSessionEndpoints(this WebApplication app)
    {
        // Sessions nested under series
        app.MapGet("/api/v1/series/{id:guid}/sessions",
            async (Guid id, SessionService service, HttpContext ctx) =>
            {
                var userId = ctx.GetUserOid();
                if (userId is null)
                    return Results.Unauthorized();

                var result = await service.GetBySeriesAsync(id, userId);
                return Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapPost("/api/v1/series/{id:guid}/sessions",
            async (Guid id, CreateSessionRequest req, SessionService service, HttpContext ctx) =>
            {
                var userId = ctx.GetUserOid();
                if (userId is null)
                    return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(req.Title))
                    return Results.BadRequest(new ErrorEnvelope(
                        "validation_error", "Title is required.", ctx.TraceIdentifier));

                var (session, errorCode) = await service.CreateAsync(id, req, userId);
                if (session is null)
                {
                    return errorCode == "series_not_found"
                        ? Results.NotFound(new ErrorEnvelope(
                            "series_not_found", "Series not found.", ctx.TraceIdentifier))
                        : Results.BadRequest(new ErrorEnvelope(
                            errorCode ?? "invalid_request",
                            "EndsAt must be after StartsAt.", ctx.TraceIdentifier));
                }

                return Results.Created($"/api/v1/sessions/{session.SessionId}", session);
            })
            .RequireAuthorization();

        // Sessions direct access
        var sessionGroup = app.MapGroup("/api/v1/sessions").RequireAuthorization();

        sessionGroup.MapGet("/{id:guid}", async (Guid id, SessionService service, DriftDetectionService driftService, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var result = await service.GetByIdAsync(id, userId);
            if (result is null)
                return Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier));

            // SPEC-200: check for drift before returning
            await driftService.CheckDriftAsync(id, userId);

            // Re-fetch to pick up any drift status update
            result = await service.GetByIdAsync(id, userId);
            return result is null
                ? Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier))
                : Results.Ok(result);
        });

        sessionGroup.MapPut("/{id:guid}", async (Guid id, UpdateSessionRequest req, SessionService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest(new ErrorEnvelope(
                    "validation_error", "Title is required.", ctx.TraceIdentifier));

            var (session, errorCode) = await service.UpdateAsync(id, req, userId);
            if (session is null)
            {
                return errorCode == "invalid_time_range"
                    ? Results.BadRequest(new ErrorEnvelope(
                        "invalid_time_range", "EndsAt must be after StartsAt.", ctx.TraceIdentifier))
                    : Results.NotFound(new ErrorEnvelope(
                        "session_not_found", "Session not found.", ctx.TraceIdentifier));
            }

            return Results.Ok(session);
        });

        sessionGroup.MapDelete("/{id:guid}", async (Guid id, SessionService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var deleted = await service.DeleteAsync(id, userId);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier));
        });

        return app;
    }
}
