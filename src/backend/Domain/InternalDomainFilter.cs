namespace EdgeFront.Builder.Domain;

public class InternalDomainFilter
{
    private readonly HashSet<string> _internalDomains;

    public InternalDomainFilter(IEnumerable<string> internalDomains)
    {
        _internalDomains = new HashSet<string>(
            internalDomains.Select(d => d.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool IsInternal(string domain) =>
        _internalDomains.Contains(domain);
}
