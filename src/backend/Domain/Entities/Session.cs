namespace EdgeFront.Builder.Domain.Entities;

public class Session
{
    public Guid SessionId { get; set; }
    public Guid SeriesId { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
}
