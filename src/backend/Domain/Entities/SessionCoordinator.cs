namespace EdgeFront.Builder.Domain.Entities;

public class SessionCoordinator
{
    public Guid SessionCoordinatorId { get; set; }
    public Guid SessionId { get; set; }
    public string EntraUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
