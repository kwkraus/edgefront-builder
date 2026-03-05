using EdgeFront.Builder.Features.People;

namespace EdgeFront.Builder.Features.Sessions.Dtos;

public record SetPresentersRequest(List<PersonInput> People);
