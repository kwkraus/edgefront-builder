using EdgeFront.Builder.Common;
using EdgeFront.Builder.Common.Extensions;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Features.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            .Accepts<SessionImportFileRequest>("multipart/form-data")
            .DisableAntiforgery();

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
            .Accepts<SessionImportFileRequest>("multipart/form-data")
            .DisableAntiforgery();

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
            .Accepts<SessionImportFileRequest>("multipart/form-data")
            .DisableAntiforgery();

        // AI-powered registration preview endpoint
        // POST /api/v1/sessions/{id:guid}/imports/registrations/preview
        sessionGroup.MapPost("/{id:guid}/imports/registrations/preview",
            async (Guid id, [FromForm] SessionImportFileRequest req, SessionService sessionService, IRegistrationFileParser fileParser, HttpContext ctx, CancellationToken ct) =>
            {
                var userId = ctx.GetUserOid();
                if (userId is null)
                    return Results.Unauthorized();

                if (req.File == null || req.File.Length == 0)
                    return Results.BadRequest(new ErrorEnvelope(
                        "invalid_file",
                        "File must not be empty.",
                        ctx.TraceIdentifier));

                // Verify session ownership
                var session = await sessionService.GetByIdAsync(id, userId);
                if (session is null)
                    return Results.NotFound(new ErrorEnvelope(
                        "session_not_found",
                        "Session not found.",
                        ctx.TraceIdentifier));

                try
                {
                    // Parse the CSV file
                    var registrants = await fileParser.ParseAsync(req.File, ct);

                    // Build preview response
                    var successCount = registrants.Count(r => r.Status == "success");
                    var failedCount = registrants.Count(r => r.Status == "failed");

                    var preview = new RegistrationPreviewDto
                    {
                        SessionTitle = session.Title,
                        RegistrantCount = registrants.Count,
                        SuccessCount = successCount,
                        FailedCount = failedCount,
                        Registrants = registrants
                    };

                    return Results.Ok(preview);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ErrorEnvelope(
                        "validation_error",
                        ex.Message,
                        ctx.TraceIdentifier));
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new ErrorEnvelope(
                        "parsing_error",
                        ex.Message,
                        ctx.TraceIdentifier));
                }
            })
            .Accepts<SessionImportFileRequest>("multipart/form-data")
            .DisableAntiforgery();

        // AI-powered registration confirm endpoint
        // POST /api/v1/sessions/{id:guid}/imports/registrations/confirm
        sessionGroup.MapPost("/{id:guid}/imports/registrations/confirm",
            async (Guid id, ConfirmRegistrationImportRequest req, SessionService sessionService, AppDbContext db, MetricsRecomputeService metricsService, HttpContext ctx, CancellationToken ct) =>
            {
                var userId = ctx.GetUserOid();
                if (userId is null)
                    return Results.Unauthorized();

                if (req.Registrants == null || req.Registrants.Count == 0)
                    return Results.BadRequest(new ErrorEnvelope(
                        "validation_error",
                        "Registrants list must not be empty.",
                        ctx.TraceIdentifier));

                // Verify session ownership
                var session = await sessionService.GetByIdAsync(id, userId);
                if (session is null)
                    return Results.NotFound(new ErrorEnvelope(
                        "session_not_found",
                        "Session not found.",
                        ctx.TraceIdentifier));

                try
                {
                    // Validate all registrants
                    var validationErrors = new List<string>();
                    foreach (var (registrant, index) in req.Registrants.Select((r, i) => (r, i)))
                    {
                        if (registrant.Status != "success")
                        {
                            validationErrors.Add($"Registrant {index} has status '{registrant.Status}': {registrant.ErrorReason}");
                        }

                        if (string.IsNullOrWhiteSpace(registrant.Email))
                            validationErrors.Add($"Registrant {index} has missing email.");

                        if (!System.Net.Mail.MailAddress.TryCreate(registrant.Email ?? "", out _))
                            validationErrors.Add($"Registrant {index} has invalid email format: {registrant.Email}");

                        if (string.IsNullOrWhiteSpace(registrant.FirstName))
                            validationErrors.Add($"Registrant {index} has missing first name.");

                        if (string.IsNullOrWhiteSpace(registrant.LastName))
                            validationErrors.Add($"Registrant {index} has missing last name.");
                    }

                    if (validationErrors.Count > 0)
                    {
                        return Results.BadRequest(new ErrorEnvelope(
                            "validation_error",
                            "Registrant validation failed.",
                            ctx.TraceIdentifier,
                            string.Join("; ", validationErrors)));
                    }

                    // Begin transaction
                    using var transaction = await db.Database.BeginTransactionAsync(ct);
                    try
                    {
                        // Delete existing registrations for this session
                        var existingRegistrations = await db.NormalizedRegistrations
                            .Where(r => r.SessionId == id)
                            .ToListAsync(ct);
                        db.NormalizedRegistrations.RemoveRange(existingRegistrations);

                        // Insert new registrations
                        var normalizedRegistrations = req.Registrants
                            .Where(r => r.Status == "success")
                            .Select(r => new NormalizedRegistration
                            {
                                RegistrationId = Guid.NewGuid(),
                                SessionId = id,
                                OwnerUserId = userId,
                                Email = r.Email.ToLowerInvariant(),
                                EmailDomain = ExtractDomain(r.Email),
                                FirstName = r.FirstName,
                                LastName = r.LastName,
                                RegisteredAt = r.RegisteredAt
                            })
                            .ToList();

                        db.NormalizedRegistrations.AddRange(normalizedRegistrations);

                        // Create or update import summary
                        var existingSummary = await db.SessionImportSummaries
                            .FirstOrDefaultAsync(s => s.SessionId == id && s.ImportType == SessionImportType.Registrations, ct);

                        if (existingSummary != null)
                        {
                            existingSummary.RowCount = normalizedRegistrations.Count;
                            existingSummary.ImportedAt = DateTime.UtcNow;
                            existingSummary.FileName = "registrations-ai-parsed.csv";
                            db.SessionImportSummaries.Update(existingSummary);
                        }
                        else
                        {
                            db.SessionImportSummaries.Add(new SessionImportSummary
                            {
                                SessionImportSummaryId = Guid.NewGuid(),
                                SessionId = id,
                                ImportType = SessionImportType.Registrations,
                                FileName = "registrations-ai-parsed.csv",
                                RowCount = normalizedRegistrations.Count,
                                ImportedAt = DateTime.UtcNow
                            });
                        }

                        // Save changes
                        await db.SaveChangesAsync(ct);

                        // Recompute metrics
                        await metricsService.RecomputeSessionMetricsAsync(id, ct);
                        await metricsService.RecomputeSeriesMetricsAsync(session.SeriesId, ct);

                        await transaction.CommitAsync(ct);

                        // Return import summary
                        var summary = await db.SessionImportSummaries
                            .Where(s => s.SessionId == id && s.ImportType == SessionImportType.Registrations)
                            .FirstOrDefaultAsync(ct);

                        if (summary != null)
                        {
                            var result = new SessionImportSummaryDto(
                                summary.ImportType.ToString(),
                                summary.FileName,
                                summary.RowCount,
                                summary.ImportedAt);
                            return Results.Ok(result);
                        }

                        return Results.BadRequest(new ErrorEnvelope(
                            "import_failed",
                            "Import summary not found after import.",
                            ctx.TraceIdentifier));
                    }
                    catch
                    {
                        await transaction.RollbackAsync(ct);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new ErrorEnvelope(
                        "import_error",
                        $"Import failed: {ex.Message}",
                        ctx.TraceIdentifier));
                }
            });

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

    private static string ExtractDomain(string email)
    {
        if (string.IsNullOrEmpty(email)) return string.Empty;
        var atIndex = email.LastIndexOf('@');
        return atIndex >= 0 ? email.Substring(atIndex + 1).ToLowerInvariant() : string.Empty;
    }
}
