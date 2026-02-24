namespace EdgeFront.Builder.Features.Metrics.Dtos;

public record SessionMetricsResponseDto(
    Guid SessionId,
    int TotalRegistrations,
    int TotalAttendees,
    int UniqueRegistrantAccountDomains,
    int UniqueAttendeeAccountDomains,
    List<string> WarmAccountsTriggered);
