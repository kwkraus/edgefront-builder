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

                var displayName = ctx.GetUserDisplayName();
                var result = await service.GetBySeriesAsync(id, userId, displayName);
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

        sessionGroup.MapGet("/{id:guid}", async (Guid id, SessionService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var result = await service.GetByIdAsync(id, userId);
            if (result is null)
                return Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier));

            return Results.Ok(result);
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

        sessionGroup.MapPut("/{id:guid}/title", async (Guid id, UpdateSessionTitleRequest req, SessionService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest(new ErrorEnvelope(
                    "validation_error", "Title is required.", ctx.TraceIdentifier));

            var (session, errorCode) = await service.UpdateTitleAsync(id, req, userId);
            if (session is null)
            {
                return Results.NotFound(new ErrorEnvelope(
                    errorCode ?? "session_not_found", "Session not found.", ctx.TraceIdentifier));
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

        // --- Presenter / Coordinator role management (SPEC-210) ---

        sessionGroup.MapGet("/{id:guid}/presenters", async (Guid id, SessionService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            // Verify session exists and belongs to user; roles are already loaded as part of the response
            var session = await service.GetByIdAsync(id, userId);
            if (session is null)
                return Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier));

            return Results.Ok(session.Presenters);
        });

        sessionGroup.MapPut("/{id:guid}/presenters", async (Guid id, SetPresentersRequest req, SessionService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var (presenters, errorCode) = await service.SetPresentersAsync(id, userId, req);
            if (presenters is null)
            {
                return Results.NotFound(new ErrorEnvelope(
                    errorCode ?? "session_not_found", "Session not found.", ctx.TraceIdentifier));
            }

            return Results.Ok(presenters);
        });

        sessionGroup.MapGet("/{id:guid}/coordinators", async (Guid id, SessionService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            // Verify session exists and belongs to user; roles are already loaded as part of the response
            var session = await service.GetByIdAsync(id, userId);
            if (session is null)
                return Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier));

            return Results.Ok(session.Coordinators);
        });

        sessionGroup.MapPut("/{id:guid}/coordinators", async (Guid id, SetCoordinatorsRequest req, SessionService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var (coordinators, errorCode) = await service.SetCoordinatorsAsync(id, userId, req);
            if (coordinators is null)
            {
                return Results.NotFound(new ErrorEnvelope(
                    errorCode ?? "session_not_found", "Session not found.", ctx.TraceIdentifier));
            }

            return Results.Ok(coordinators);
        });

        return app;
    }
}
