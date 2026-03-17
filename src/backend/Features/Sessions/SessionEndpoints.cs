using EdgeFront.Builder.Common;
using EdgeFront.Builder.Common.Extensions;
using EdgeFront.Builder.Features.Sessions.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EdgeFront.Builder.Features.Sessions;

public static class SessionEndpoints
{
    public static WebApplication MapSessionEndpoints(this WebApplication app)
    {
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

        var sessionGroup = app.MapGroup("/api/v1/sessions").RequireAuthorization();

        sessionGroup.MapGet("/{id:guid}", async (Guid id, SessionService service, HttpContext ctx) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var result = await service.GetByIdAsync(id, userId);
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

        sessionGroup.MapPost("/{id:guid}/imports/registrations",
            async (Guid id, [FromForm] SessionImportFileRequest req, SessionImportService service, HttpContext ctx, CancellationToken ct) =>
            {
                var userId = ctx.GetUserOid();
                if (userId is null)
                    return Results.Unauthorized();

                return MapImportResult(
                    await service.ReplaceRegistrationsAsync(id, userId, req.File, ct),
                    ctx);
            })
            .Accepts<SessionImportFileRequest>("multipart/form-data");

        sessionGroup.MapPost("/{id:guid}/imports/attendance",
            async (Guid id, [FromForm] SessionImportFileRequest req, SessionImportService service, HttpContext ctx, CancellationToken ct) =>
            {
                var userId = ctx.GetUserOid();
                if (userId is null)
                    return Results.Unauthorized();

                return MapImportResult(
                    await service.ReplaceAttendanceAsync(id, userId, req.File, ct),
                    ctx);
            })
            .Accepts<SessionImportFileRequest>("multipart/form-data");

        sessionGroup.MapPost("/{id:guid}/imports/qa",
            async (Guid id, [FromForm] SessionImportFileRequest req, SessionImportService service, HttpContext ctx, CancellationToken ct) =>
            {
                var userId = ctx.GetUserOid();
                if (userId is null)
                    return Results.Unauthorized();

                return MapImportResult(
                    await service.ReplaceQaAsync(id, userId, req.File, ct),
                    ctx);
            })
            .Accepts<SessionImportFileRequest>("multipart/form-data");

        return app;
    }

    private static IResult MapImportResult(SessionImportOutcome outcome, HttpContext ctx)
    {
        if (outcome.IsSuccess && outcome.Summary is not null)
            return Results.Ok(outcome.Summary);

        if (outcome.ErrorCode == "session_not_found")
        {
            return Results.NotFound(new ErrorEnvelope(
                outcome.ErrorCode,
                outcome.Message ?? "Session not found.",
                ctx.TraceIdentifier,
                outcome.Details));
        }

        return Results.BadRequest(new ErrorEnvelope(
            outcome.ErrorCode ?? "validation_error",
            outcome.Message ?? "The uploaded CSV file is invalid.",
            ctx.TraceIdentifier,
            outcome.Details));
    }
}
