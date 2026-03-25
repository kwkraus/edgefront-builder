using EdgeFront.Builder.Features.Sessions.Dtos;

namespace EdgeFront.Builder.Features.Sessions;

/// <summary>
/// CSV-specific implementation of IRegistrationFileParser.
/// Reads CSV file content and delegates parsing to RegistrationParsingService.
/// </summary>
public class CsvRegistrationFileParser : IRegistrationFileParser
{
    private readonly RegistrationParsingService _parsingService;

    public CsvRegistrationFileParser(RegistrationParsingService parsingService)
    {
        _parsingService = parsingService ?? throw new ArgumentNullException(nameof(parsingService));
    }

    /// <summary>
    /// Parses a CSV file by reading its content and passing it to RegistrationParsingService.
    /// </summary>
    /// <param name="file">The CSV file to parse.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of parsed registrants with success/failure status.</returns>
    /// <exception cref="ArgumentException">Thrown if file is null or empty.</exception>
    public async Task<List<ParsedRegistrant>> ParseAsync(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File must not be null or empty.", nameof(file));

        using var reader = new StreamReader(file.OpenReadStream());
        var csvText = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(csvText))
            throw new ArgumentException("CSV file is empty.", nameof(file));

        return await _parsingService.ParseRegistrationsAsync(csvText, ct);
    }
}
