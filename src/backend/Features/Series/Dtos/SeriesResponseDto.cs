namespace EdgeFront.Builder.Features.Series.Dtos;

public record SeriesResponseDto(
    Guid SeriesId,
    string Title,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
