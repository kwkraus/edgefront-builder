namespace EdgeFront.Builder.Features.Sessions.Dtos;

public record ImportResultDto(
    int TotalRows,
    int ImportedCount,
    int SkippedCount,
    int InvalidCount,
    List<RowErrorDto> Errors);

public record RowErrorDto(int Row, string Reason);
