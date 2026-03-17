namespace EdgeFront.Builder.Features.Metrics.Dtos;

public record SessionMetricsResponseDto(
    Guid SessionId,
    int TotalRegistrations,
    int TotalAttendees,
    int TotalQaQuestions,
    int AnsweredQaQuestions,
    int UniqueRegistrantAccountDomains,
    int UniqueAttendeeAccountDomains,
    List<string> WarmAccountsTriggered);
