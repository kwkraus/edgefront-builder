namespace EdgeFront.Builder.Features.Sessions.Dtos;

public record SessionResponseDto(
    Guid SessionId,
    Guid SeriesId,
    string Title,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    string? TeamsWebinarId,
    string? JoinWebUrl,
    string ReconcileStatus,
    string DriftStatus,
    DateTime? LastSyncAt,
    string? LastError,
    List<SessionPresenterDto> Presenters,
    List<SessionCoordinatorDto> Coordinators);
