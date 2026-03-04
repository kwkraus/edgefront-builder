namespace EdgeFront.Builder.Features.Series.Dtos;

// TODO-SPEC: DraftSessionCount not yet defined in SPEC-110; added to support Partially Published display.
public record SeriesResponseDto(
    Guid SeriesId,
    string Title,
    string Status,
    int DraftSessionCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
