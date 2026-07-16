namespace EdgeFront.Builder.Features.Series.Dtos;

public record SeriesResponseDto(
    Guid SeriesId,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt);
