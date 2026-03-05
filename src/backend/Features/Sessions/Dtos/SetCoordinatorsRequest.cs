using EdgeFront.Builder.Features.People;

namespace EdgeFront.Builder.Features.Sessions.Dtos;

public record SetCoordinatorsRequest(List<PersonInput> People);
