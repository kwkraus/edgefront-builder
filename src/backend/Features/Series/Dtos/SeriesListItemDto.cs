namespace EdgeFront.Builder.Features.Series.Dtos;

public record SeriesListItemDto(
    Guid SeriesId,
    string Title,
    string Status,
    int SessionCount,
    int TotalRegistrations,
    int TotalAttendees,
    int UniqueAccountsInfluenced,
    bool HasReconcileIssues,
    DateTime CreatedAt,
    DateTime UpdatedAt);
