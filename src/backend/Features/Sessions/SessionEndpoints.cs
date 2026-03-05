using EdgeFront.Builder.Common;
using EdgeFront.Builder.Common.Extensions;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Infrastructure.Graph;

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

        sessionGroup.MapGet("/{id:guid}", async (Guid id, SessionService service, DriftDetectionService driftService, IOboTokenService oboService, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var result = await service.GetByIdAsync(id, userId);
            if (result is null)
                return Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier));

            // Best-effort drift check with OBO token (delegated)
            var oboToken = await TryGetOboTokenAsync(ctx, oboService);
            await driftService.CheckDriftAsync(id, userId, oboToken);

            // Re-fetch to pick up any drift status update
            result = await service.GetByIdAsync(id, userId);
            return result is null
                ? Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier))
                : Results.Ok(result);
        });

        sessionGroup.MapPut("/{id:guid}", async (Guid id, UpdateSessionRequest req, SessionService service, IOboTokenService oboService, ITeamsGraphClient graphClient, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest(new ErrorEnvelope(
                    "validation_error", "Title is required.", ctx.TraceIdentifier));

            // Acquire OBO token so UpdateAsync can push changes to Teams for published sessions
            var oboToken = await TryGetOboTokenAsync(ctx, oboService);
            var (session, errorCode) = await service.UpdateAsync(id, req, userId, oboToken, graphClient);
            if (session is null)
            {
                if (errorCode == "TEAMS_UPDATE_FAILED")
                    return Results.UnprocessableEntity(new ErrorEnvelope(
                        "TEAMS_UPDATE_FAILED", "Teams webinar could not be updated.", ctx.TraceIdentifier));
                return errorCode == "invalid_time_range"
                    ? Results.BadRequest(new ErrorEnvelope(
                        "invalid_time_range", "EndsAt must be after StartsAt.", ctx.TraceIdentifier))
                    : Results.NotFound(new ErrorEnvelope(
                        "session_not_found", "Session not found.", ctx.TraceIdentifier));
            }

            return Results.Ok(session);
        });

        sessionGroup.MapDelete("/{id:guid}", async (Guid id, SessionService service, IOboTokenService oboService, ITeamsGraphClient graphClient, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            // Best-effort OBO token: if token acquisition fails (null), DeleteAsync will
            // still delete the local record but skip the Teams webinar deletion.
            var oboToken = await TryGetOboTokenAsync(ctx, oboService);
            var deleted = await service.DeleteAsync(id, userId, graphClient, oboToken);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier));
        });

        // TODO-SPEC: POST /sessions/{id}/publish not yet in SPEC-110; added for incremental session publish.
        sessionGroup.MapPost("/{id:guid}/publish", async (Guid id, SessionService service, ITeamsGraphClient graphClient, IOboTokenService oboService, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionEndpoints");
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var oboToken = await TryGetOboTokenAsync(ctx, oboService);

            var (session, errorCode) = await service.PublishAsync(id, userId, oboToken, graphClient, logger);
            if (session is null)
            {
                if (errorCode == "session_not_found")
                    return Results.NotFound(new ErrorEnvelope(
                        errorCode, "Session not found.", ctx.TraceIdentifier));
                if (errorCode is "SESSION_ALREADY_PUBLISHED" or "SERIES_NOT_PUBLISHED")
                    return Results.BadRequest(new ErrorEnvelope(
                        errorCode, errorCode == "SESSION_ALREADY_PUBLISHED"
                            ? "Session is already published."
                            : "Series must be published before publishing individual sessions.",
                        ctx.TraceIdentifier));

                return Results.UnprocessableEntity(new ErrorEnvelope(
                    errorCode ?? "SESSION_PUBLISH_FAILED", "Session publish failed.", ctx.TraceIdentifier));
            }

            return Results.Ok(session);
        });

        // Sync session data from Teams (user-initiated, delegated)
        sessionGroup.MapPost("/{id:guid}/sync", async (Guid id, SyncService syncService, IOboTokenService oboService, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionEndpoints");
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var oboToken = await TryGetOboTokenAsync(ctx, oboService);
            if (string.IsNullOrEmpty(oboToken))
            {
                return Results.UnprocessableEntity(new ErrorEnvelope(
                    "OBO_EXCHANGE_FAILED",
                    "Could not acquire a Graph API token. Verify the Entra ID app registration has the VirtualEvent.ReadWrite delegated permission with admin consent.",
                    ctx.TraceIdentifier));
            }

            var result = await syncService.SyncSessionAsync(id, userId, oboToken);
            if (!result.Success)
            {
                if (result.ErrorCode == "session_not_found")
                    return Results.NotFound(new ErrorEnvelope(
                        "session_not_found", "Session not found.", ctx.TraceIdentifier));
                if (result.ErrorCode == "session_not_published")
                    return Results.BadRequest(new ErrorEnvelope(
                        "session_not_published", "Only published sessions can be synced.", ctx.TraceIdentifier));

                return Results.UnprocessableEntity(new ErrorEnvelope(
                    result.ErrorCode ?? "SYNC_FAILED", "Sync failed.", ctx.TraceIdentifier));
            }

            return Results.Ok(new { synced = true });
        });

        // Check drift for a session (user-initiated, delegated)
        sessionGroup.MapPost("/{id:guid}/check-drift", async (Guid id, DriftDetectionService driftService, IOboTokenService oboService, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var oboToken = await TryGetOboTokenAsync(ctx, oboService);
            if (string.IsNullOrEmpty(oboToken))
            {
                return Results.UnprocessableEntity(new ErrorEnvelope(
                    "OBO_EXCHANGE_FAILED",
                    "Could not acquire a Graph API token.",
                    ctx.TraceIdentifier));
            }

            var driftStatus = await driftService.CheckDriftAsync(id, userId, oboToken);
            return Results.Ok(new { driftStatus = driftStatus.ToString() });
        });

        // --- Presenter / Coordinator role management (SPEC-210) ---

        sessionGroup.MapGet("/{id:guid}/presenters", async (Guid id, SessionService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            // Verify session exists and belongs to user
            var session = await service.GetByIdAsync(id, userId);
            if (session is null)
                return Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier));

            var presenters = await service.GetPresentersAsync(id);
            return Results.Ok(presenters);
        });

        sessionGroup.MapPut("/{id:guid}/presenters", async (Guid id, SetPresentersRequest req, SessionService service, IOboTokenService oboService, ITeamsGraphClient graphClient, IConfiguration config, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionEndpoints");
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var oboToken = await TryGetOboTokenAsync(ctx, oboService);
            var (presenters, errorCode) = await service.SetPresentersAsync(id, userId, req, oboToken, graphClient, config, logger);
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

            // Verify session exists and belongs to user
            var session = await service.GetByIdAsync(id, userId);
            if (session is null)
                return Results.NotFound(new ErrorEnvelope(
                    "session_not_found", "Session not found.", ctx.TraceIdentifier));

            var coordinators = await service.GetCoordinatorsAsync(id);
            return Results.Ok(coordinators);
        });

        sessionGroup.MapPut("/{id:guid}/coordinators", async (Guid id, SetCoordinatorsRequest req, SessionService service, IOboTokenService oboService, ITeamsGraphClient graphClient, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SessionEndpoints");
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var oboToken = await TryGetOboTokenAsync(ctx, oboService);
            var (coordinators, errorCode) = await service.SetCoordinatorsAsync(id, userId, req, oboToken, graphClient, logger);
            if (coordinators is null)
            {
                return Results.NotFound(new ErrorEnvelope(
                    errorCode ?? "session_not_found", "Session not found.", ctx.TraceIdentifier));
            }

            return Results.Ok(coordinators);
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
