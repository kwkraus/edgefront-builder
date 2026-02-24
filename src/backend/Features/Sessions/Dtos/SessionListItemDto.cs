namespace EdgeFront.Builder.Features.Sessions.Dtos;

public record SessionListItemDto(
    Guid SessionId,
    string Title,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    string ReconcileStatus,
    string DriftStatus,
    int TotalRegistrations,
    int TotalAttendees);
