namespace EdgeFront.Builder.Features.Sessions.Dtos;

/// <summary>
/// Response DTO for the registration preview endpoint.
/// Provides a summary of parsed registrants without persisting to the database.
/// </summary>
public class RegistrationPreviewDto
{
    /// <summary>
    /// Title of the session, extracted from the CSV file.
    /// </summary>
    public string SessionTitle { get; set; } = string.Empty;

    /// <summary>
    /// Total number of registrants parsed (success + failed).
    /// </summary>
    public int RegistrantCount { get; set; }

    /// <summary>
    /// Number of successfully parsed registrants.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of registrants that failed to parse.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// List of all parsed registrants, including both successful and failed rows.
    /// Each entry includes status and, for failed rows, an error reason.
    /// </summary>
    public List<ParsedRegistrant> Registrants { get; set; } = new();

    /// <summary>
    /// Non-blocking warnings (e.g., duplicate emails, mismatched fields).
    /// The preview can still proceed with confirmation.
    /// </summary>
    public List<string>? Warnings { get; set; }

    /// <summary>
    /// Blocking validation errors that prevent confirmation.
    /// These represent critical issues (e.g., AI API failure, malformed file).
    /// </summary>
    public List<string>? Errors { get; set; }
}
