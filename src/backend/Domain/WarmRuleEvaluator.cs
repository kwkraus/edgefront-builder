using EdgeFront.Builder.Domain.Entities;

namespace EdgeFront.Builder.Domain;

public class WarmRuleEvaluator
{
    private readonly InternalDomainFilter _internalDomainFilter;

    public WarmRuleEvaluator(InternalDomainFilter internalDomainFilter)
    {
        _internalDomainFilter = internalDomainFilter;
    }

    /// <summary>
    /// W1: ≥2 distinct email addresses from the same domain in a single session.
    /// Returns external domains that meet the W1 threshold.
    /// </summary>
    public IEnumerable<string> EvaluateW1(IEnumerable<NormalizedAttendance> sessionAttendances)
    {
        return sessionAttendances
            .GroupBy(a => a.EmailDomain, StringComparer.OrdinalIgnoreCase)
            .Where(g => !_internalDomainFilter.IsInternal(g.Key))
            .Where(g => g.Select(a => a.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2)
            .Select(g => g.Key.ToLowerInvariant());
    }

    /// <summary>
    /// W2: Same email address attends ≥2 distinct sessions in the same series.
    /// Returns external email domains of qualifying emails.
    /// </summary>
    public IEnumerable<string> EvaluateW2(IEnumerable<NormalizedAttendance> allSeriesAttendances)
    {
        return allSeriesAttendances
            .GroupBy(a => a.Email, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(a => a.SessionId).Distinct().Count() >= 2)
            .Select(g => g.First().EmailDomain.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(domain => !_internalDomainFilter.IsInternal(domain));
    }
}
