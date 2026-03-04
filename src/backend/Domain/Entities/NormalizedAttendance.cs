namespace EdgeFront.Builder.Domain.Entities;

public class NormalizedAttendance
{
    public Guid AttendanceId { get; set; }
    public Guid SessionId { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmailDomain { get; set; } = string.Empty;
    public bool Attended { get; set; }
    public int? DurationSeconds { get; set; }
    public decimal? DurationPercent { get; set; }
    public DateTime? FirstJoinAt { get; set; }
    public DateTime? LastLeaveAt { get; set; }
}
