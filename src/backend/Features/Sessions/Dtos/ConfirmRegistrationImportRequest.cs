namespace EdgeFront.Builder.Features.Sessions.Dtos;

/// <summary>
/// Request DTO for confirming and persisting a registration import.
/// Contains the list of registrants that passed validation in the preview step.
/// </summary>
public class ConfirmRegistrationImportRequest
{
    /// <summary>
    /// List of parsed registrants to import. These should have been returned from the preview endpoint.
    /// All registrants in this list are expected to have Status = "success".
    /// </summary>
    public List<ParsedRegistrant> Registrants { get; set; } = new();
}
