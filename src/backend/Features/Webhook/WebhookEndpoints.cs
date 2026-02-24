using EdgeFront.Builder.Common;
using EdgeFront.Builder.Features.Webhook.Dtos;

namespace EdgeFront.Builder.Features.Webhook;

public static class WebhookEndpoints
{
    public static WebApplication MapWebhookEndpoints(this WebApplication app)
    {
        // POST /api/v1/webhooks/graph
        // NO .RequireAuthorization() — machine-authenticated via clientState (per SPEC-210)
        app.MapPost("/api/v1/webhooks/graph", async (
            HttpContext ctx,
            WebhookService webhookService,
            ILogger<WebhookService> logger) =>
        {
            var correlationId = ctx.TraceIdentifier;

            // Validation token handshake (per Microsoft Graph subscription model)
            if (ctx.Request.Query.TryGetValue("validationToken", out var validationToken)
                && !string.IsNullOrEmpty(validationToken))
            {
                logger.LogInformation(
                    "Graph webhook validation handshake. CorrelationId={CorrelationId}", correlationId);
                return Results.Text(validationToken!, "text/plain");
            }

            GraphNotificationEnvelope? envelope;
            try
            {
                envelope = await ctx.Request.ReadFromJsonAsync<GraphNotificationEnvelope>();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to deserialize Graph notification. CorrelationId={CorrelationId}", correlationId);
                return Results.BadRequest(new ErrorEnvelope(
                    "invalid_payload", "Request body could not be parsed.", correlationId));
            }

            if (envelope is null || envelope.Value.Count == 0)
            {
                return Results.BadRequest(new ErrorEnvelope(
                    "empty_payload", "Notification envelope contains no items.", correlationId));
            }

            // Validate clientState for each notification (SPEC-210 replay protection)
            foreach (var notification in envelope.Value)
            {
                var isValid = await webhookService.ValidateClientStateAsync(
                    notification.SubscriptionId, notification.ClientState);

                if (!isValid)
                {
                    logger.LogWarning(
                        "Invalid clientState for subscription {SubscriptionId}. CorrelationId={CorrelationId}",
                        notification.SubscriptionId, correlationId);

                    return Results.BadRequest(new ErrorEnvelope(
                        "invalid_client_state",
                        $"ClientState validation failed for subscription {notification.SubscriptionId}.",
                        correlationId));
                }
            }

            await webhookService.HandleAsync(envelope, correlationId);

            return Results.Accepted();
        });

        return app;
    }
}
