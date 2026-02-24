namespace EdgeFront.Builder.Domain.Entities;

public class GraphSubscription
{
    public Guid GraphSubscriptionId { get; set; }
    public Guid SessionId { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public ChangeType ChangeType { get; set; }
    public string ClientStateHash { get; set; } = string.Empty;
    public DateTime ExpirationDateTime { get; set; }
    public DateTime CreatedAt { get; set; }
}
