using Nager.PublicSuffix;

namespace EdgeFront.Builder.Api.Domain;

/// <summary>
/// Normalizes raw input (email address or bare domain) to a registrable domain
/// using public-suffix-aware eTLD+1 parsing per SPEC-010.
/// </summary>
public class DomainNormalizer
{
    private readonly IDomainParser _domainParser;
    private readonly IReadOnlySet<string> _internalDomains;

    public DomainNormalizer(IDomainParser domainParser, IEnumerable<string> internalDomains)
    {
        _domainParser = domainParser;
        _internalDomains = new HashSet<string>(
            internalDomains.Select(d => d.Trim().ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts the registrable domain (eTLD+1) from an email address or bare domain.
    /// Strips all subdomains. Returns null for internal domains, invalid input, or
    /// domains that cannot be resolved to a registrable domain.
    /// </summary>
    public string? NormalizeRegistrableDomain(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var domain = ExtractDomain(input);
        if (string.IsNullOrEmpty(domain))
            return null;

        DomainInfo? domainInfo;
        try
        {
            domainInfo = _domainParser.Parse(domain);
        }
        catch
        {
            return null;
        }

        var registrableDomain = domainInfo?.RegistrableDomain;
        if (string.IsNullOrEmpty(registrableDomain))
            return null;

        if (_internalDomains.Contains(registrableDomain))
            return null;

        return registrableDomain;
    }

    private static string? ExtractDomain(string input)
    {
        var atIndex = input.IndexOf('@');
        var raw = atIndex >= 0 ? input[(atIndex + 1)..] : input;
        var domain = raw.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(domain) ? null : domain;
    }
}
