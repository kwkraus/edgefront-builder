namespace EdgeFront.Builder.Domain;

public static class DomainNormalizer
{
    public static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    public static string NormalizeEmailDomain(string email)
    {
        var normalized = NormalizeEmail(email);
        var atIndex = normalized.IndexOf('@');
        return atIndex >= 0 ? normalized[(atIndex + 1)..] : normalized;
    }

    /// <summary>
    /// Returns the registrable domain (eTLD+1) by stripping leading subdomains to the last two labels.
    /// For domains with only one or two labels, returns as-is.
    /// </summary>
    /// <remarks>
    /// TODO-SPEC: Full public suffix list integration needed for accurate eTLD+1 parsing.
    /// Current implementation strips all subdomains to last 2 labels only.
    /// Known two-level TLDs (e.g., co.uk, com.au) are not handled correctly.
    /// </remarks>
    public static string NormalizeRegistrableDomain(string domain)
    {
        var lower = domain.Trim().ToLowerInvariant();
        var parts = lower.Split('.');
        if (parts.Length <= 2)
            return lower;

        // TODO-SPEC: Full public suffix list integration needed for accurate eTLD+1 parsing.
        return string.Join('.', parts[^2..]);
    }
}
