namespace EdgeFront.Builder.Domain.Entities;

public class Session
{
    public Guid SessionId { get; set; }
    public Guid SeriesId { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Draft;
    public string? TeamsWebinarId { get; set; }
    public DriftStatus DriftStatus { get; set; } = DriftStatus.None;
    public ReconcileStatus ReconcileStatus { get; set; } = ReconcileStatus.Synced;
    public DateTime? LastSyncAt { get; set; }
    public string? LastError { get; set; }
}
