namespace EdgeFront.Builder.Features.Metrics.Dtos;

public record SeriesMetricsResponseDto(
    Guid SeriesId,
    int TotalRegistrations,
    int TotalAttendees,
    int UniqueRegistrantAccountDomains,
    int UniqueAccountsInfluenced,
    List<WarmAccountEntryDto> WarmAccounts);
