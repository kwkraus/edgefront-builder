namespace EdgeFront.Builder.Features.Sessions.Dtos;

public record SessionListItemDto(
    Guid SessionId,
    string Title,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    int TotalRegistrations,
    int TotalAttendees,
    int PresenterCount,
    int CoordinatorCount,
    string OwnerDisplayName,
    List<PersonSummary> Presenters,
    List<PersonSummary> Coordinators,
    SessionImportSummariesDto Imports);
