namespace EdgeFront.Builder.Features.Sessions.Dtos;

public record SessionListItemDto(
    Guid SessionId,
    string Title,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    string? TeamsWebinarId,
    string? JoinWebUrl,
    string ReconcileStatus,
    string DriftStatus,
    int TotalRegistrations,
    int TotalAttendees,
    DateTime? LastSyncAt,
    int PresenterCount,
    int CoordinatorCount,
    string OwnerDisplayName,
    List<PersonSummary> Presenters,
    List<PersonSummary> Coordinators);
