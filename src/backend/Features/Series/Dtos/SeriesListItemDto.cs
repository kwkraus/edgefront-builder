namespace EdgeFront.Builder.Features.Series.Dtos;

public record SeriesListItemDto(
    Guid SeriesId,
    string Title,
    int SessionCount,
    int TotalRegistrations,
    int TotalAttendees,
    int UniqueAccountsInfluenced,
    DateTime CreatedAt,
    DateTime UpdatedAt);
