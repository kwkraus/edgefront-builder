using EdgeFront.Builder.Features.Sessions.Dtos;

namespace EdgeFront.Builder.Features.Sessions;

/// <summary>
/// Abstraction for parsing registration files.
/// Implementations handle specific file formats (e.g., CSV, Excel) and delegate parsing to a parsing service.
/// </summary>
public interface IRegistrationFileParser
{
    /// <summary>
    /// Parses a registration file and returns a list of parsed registrants.
    /// </summary>
    /// <param name="file">The file to parse (form file from HTTP request).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of parsed registrants with success/failure status.</returns>
    Task<List<ParsedRegistrant>> ParseAsync(IFormFile file, CancellationToken ct);
}
