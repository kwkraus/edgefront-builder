namespace EdgeFront.Builder.Domain.Entities;

public class NormalizedRegistration
{
    public Guid RegistrationId { get; set; }
    public Guid SessionId { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmailDomain { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
