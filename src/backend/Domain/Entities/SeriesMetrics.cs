namespace EdgeFront.Builder.Domain.Entities;

public class SeriesMetrics
{
    public Guid SeriesId { get; set; }
    public int TotalRegistrations { get; set; }
    public int TotalAttendees { get; set; }
    public int UniqueRegistrantAccountDomains { get; set; }
    public int UniqueAccountsInfluenced { get; set; }
    public List<WarmAccountEntry> WarmAccounts { get; set; } = [];
}
