using Microsoft.AspNetCore.Mvc;

namespace EdgeFront.Builder.Features.Sessions.Dtos;

public sealed class SessionImportFileRequest
{
    [FromForm(Name = "file")]
    public IFormFile? File { get; init; }
}
