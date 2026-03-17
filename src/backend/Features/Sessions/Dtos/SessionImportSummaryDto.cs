namespace EdgeFront.Builder.Features.Sessions.Dtos;

public record SessionImportSummaryDto(
    string ImportType,
    string FileName,
    int RowCount,
    DateTime ImportedAt);
