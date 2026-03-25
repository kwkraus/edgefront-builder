namespace EdgeFront.Builder.Features.Sessions.Dtos;

/// <summary>
/// Represents a single registrant parsed from a registration file.
/// Status indicates success or failure; ErrorReason is only populated for failed registrants.
/// </summary>
public class ParsedRegistrant
{
    /// <summary>
    /// Email address of the registrant (required).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name of the registrant (required, title-cased by AI parser).
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name of the registrant (required, title-cased by AI parser).
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// DateTime when the registrant registered (optional, defaults to current UTC if not provided).
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Status of the parsing result: "success" or "failed".
    /// </summary>
    public string Status { get; set; } = "success";

    /// <summary>
    /// If Status is "failed", describes the error reason (e.g., missing email, invalid format).
    /// Null for successful registrants.
    /// </summary>
    public string? ErrorReason { get; set; }
}
