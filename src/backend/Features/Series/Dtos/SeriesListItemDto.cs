namespace EdgeFront.Builder.Features.Series.Dtos;

// TODO-SPEC: DraftSessionCount not yet defined in SPEC-110; added to support Partially Published display.
public record SeriesListItemDto(
    Guid SeriesId,
    string Title,
    string Status,
    int SessionCount,
    int DraftSessionCount,
    int TotalRegistrations,
    int TotalAttendees,
    int UniqueAccountsInfluenced,
    bool HasReconcileIssues,
    DateTime CreatedAt,
    DateTime UpdatedAt);
