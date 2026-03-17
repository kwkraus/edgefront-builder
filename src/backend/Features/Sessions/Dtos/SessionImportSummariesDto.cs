namespace EdgeFront.Builder.Features.Sessions.Dtos;

public record SessionImportSummariesDto(
    SessionImportSummaryDto? Registrations,
    SessionImportSummaryDto? Attendance,
    SessionImportSummaryDto? Qa);
