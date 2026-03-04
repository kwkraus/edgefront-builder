namespace EdgeFront.Builder.Features.Sessions.Dtos;

public record UpdateSessionRequest(string Title, DateTime StartsAt, DateTime EndsAt);
