using System.Text.Json.Serialization;

namespace EdgeFront.Builder.Features.Webhook.Dtos;

public class GraphNotificationEnvelope
{
    [JsonPropertyName("value")]
    public List<GraphNotification> Value { get; set; } = [];
}
