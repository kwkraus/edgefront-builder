namespace EdgeFront.Builder.Domain.Entities;

public class SessionMetrics
{
    public Guid SessionId { get; set; }
    public int TotalRegistrations { get; set; }
    public int TotalAttendees { get; set; }
    public int TotalQaQuestions { get; set; }
    public int AnsweredQaQuestions { get; set; }
    public int UniqueRegistrantAccountDomains { get; set; }
    public int UniqueAttendeeAccountDomains { get; set; }
    public List<string> WarmAccountsTriggered { get; set; } = [];
}
