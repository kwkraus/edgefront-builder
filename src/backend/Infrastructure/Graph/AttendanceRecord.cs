namespace EdgeFront.Builder.Infrastructure.Graph;

public record AttendanceRecord(
    string Email,
    bool Attended,
    int? DurationSeconds,
    decimal? DurationPercent,
    DateTimeOffset? FirstJoinAt,
    DateTimeOffset? LastLeaveAt);
