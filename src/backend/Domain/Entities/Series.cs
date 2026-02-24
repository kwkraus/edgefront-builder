namespace EdgeFront.Builder.Domain.Entities;

public class Series
{
    public Guid SeriesId { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public SeriesStatus Status { get; set; } = SeriesStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
