namespace EdgeFront.Builder.Infrastructure.Graph;

public record TeamsWebinarInfo(
    string WebinarId,
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);
