using EdgeFront.Builder.Api.Domain;
using FluentAssertions;
using Nager.PublicSuffix;
using Nager.PublicSuffix.Models;
using Nager.PublicSuffix.RuleProviders;

namespace EdgeFront.Builder.Tests.Domain;

/// <summary>
/// Unit tests for DomainNormalizer per SPEC-010.
/// Uses StaticRuleProvider so tests run offline without network or file I/O.
/// </summary>
public class DomainNormalizerTests
{
    private static DomainNormalizer CreateNormalizer(IEnumerable<string>? internalDomains = null)
    {
        var rules = new List<TldRule>
        {
            new TldRule("com",    TldRuleDivision.ICANN),
            new TldRule("org",    TldRuleDivision.ICANN),
            new TldRule("net",    TldRuleDivision.ICANN),
            new TldRule("io",     TldRuleDivision.ICANN),
            new TldRule("uk",     TldRuleDivision.ICANN),
            new TldRule("co.uk",  TldRuleDivision.ICANN),
            new TldRule("au",     TldRuleDivision.ICANN),
            new TldRule("com.au", TldRuleDivision.ICANN),
            new TldRule("de",     TldRuleDivision.ICANN),
            new TldRule("fr",     TldRuleDivision.ICANN),
        };

        var provider = new StaticRuleProvider(rules);
        provider.BuildAsync().GetAwaiter().GetResult();
        var parser = new DomainParser(provider);

        return new DomainNormalizer(parser, internalDomains ?? []);
    }

    // ---- email inputs -------------------------------------------------------

    [Fact]
    public void NormalizeRegistrableDomain_EmailWithSimpleDomain_ReturnsRegistrableDomain()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("alice@example.com").Should().Be("example.com");
    }

    [Fact]
    public void NormalizeRegistrableDomain_EmailWithSubdomain_StripsSubdomain()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("alice@sub.example.com").Should().Be("example.com");
    }

    [Fact]
    public void NormalizeRegistrableDomain_EmailWithDeepSubdomain_StripsAllSubdomains()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("alice@deep.sub.example.com").Should().Be("example.com");
    }

    // ---- two-level TLDs (core fix) ------------------------------------------

    [Fact]
    public void NormalizeRegistrableDomain_CoUkDomain_ReturnsEtldPlusOne()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("alice@example.co.uk").Should().Be("example.co.uk");
    }

    [Fact]
    public void NormalizeRegistrableDomain_SubdomainOfCoUk_StripsSubdomain()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("alice@sub.example.co.uk").Should().Be("example.co.uk");
    }

    [Fact]
    public void NormalizeRegistrableDomain_DeepSubdomainOfCoUk_StripsAllSubdomains()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("alice@deep.sub.example.co.uk").Should().Be("example.co.uk");
    }

    [Fact]
    public void NormalizeRegistrableDomain_ComAuDomain_ReturnsEtldPlusOne()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("alice@example.com.au").Should().Be("example.com.au");
    }

    [Fact]
    public void NormalizeRegistrableDomain_DeepSubdomainOfComAu_StripsAllSubdomains()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("alice@deep.sub.example.com.au").Should().Be("example.com.au");
    }

    // ---- bare domain inputs (no email) --------------------------------------

    [Fact]
    public void NormalizeRegistrableDomain_BareDomain_ReturnsRegistrableDomain()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("sub.example.com").Should().Be("example.com");
    }

    [Fact]
    public void NormalizeRegistrableDomain_BareCoUkDomain_ReturnsEtldPlusOne()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("sub.example.co.uk").Should().Be("example.co.uk");
    }

    // ---- case normalisation -------------------------------------------------

    [Fact]
    public void NormalizeRegistrableDomain_MixedCaseInput_ReturnsLowercase()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("Alice@Example.COM").Should().Be("example.com");
    }

    [Fact]
    public void NormalizeRegistrableDomain_MixedCaseCoUk_ReturnsLowercase()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("Alice@Sub.Example.CO.UK").Should().Be("example.co.uk");
    }

    // ---- internal domain exclusion (SPEC-010) --------------------------------

    [Fact]
    public void NormalizeRegistrableDomain_InternalDomain_ReturnsNull()
    {
        var sut = CreateNormalizer(internalDomains: ["company.com"]);
        sut.NormalizeRegistrableDomain("alice@company.com").Should().BeNull();
    }

    [Fact]
    public void NormalizeRegistrableDomain_InternalDomainSubdomain_ReturnsNull()
    {
        var sut = CreateNormalizer(internalDomains: ["company.com"]);
        sut.NormalizeRegistrableDomain("alice@sub.company.com").Should().BeNull();
    }

    [Fact]
    public void NormalizeRegistrableDomain_InternalDomainCaseInsensitive_ReturnsNull()
    {
        var sut = CreateNormalizer(internalDomains: ["Company.COM"]);
        sut.NormalizeRegistrableDomain("alice@company.com").Should().BeNull();
    }

    [Fact]
    public void NormalizeRegistrableDomain_NonInternalDomain_IsNotExcluded()
    {
        var sut = CreateNormalizer(internalDomains: ["internal.com"]);
        sut.NormalizeRegistrableDomain("alice@external.com").Should().Be("external.com");
    }

    // ---- null / empty / invalid inputs --------------------------------------

    [Fact]
    public void NormalizeRegistrableDomain_NullInput_ReturnsNull()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain(null).Should().BeNull();
    }

    [Fact]
    public void NormalizeRegistrableDomain_EmptyString_ReturnsNull()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain(string.Empty).Should().BeNull();
    }

    [Fact]
    public void NormalizeRegistrableDomain_WhitespaceOnly_ReturnsNull()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("   ").Should().BeNull();
    }

    [Fact]
    public void NormalizeRegistrableDomain_BarePublicSuffix_ReturnsNull()
    {
        var sut = CreateNormalizer();
        // "co.uk" is itself a public suffix, no registrable domain
        sut.NormalizeRegistrableDomain("co.uk").Should().BeNull();
    }

    [Fact]
    public void NormalizeRegistrableDomain_EmailWithNoLocalPart_ReturnsRegistrableDomain()
    {
        var sut = CreateNormalizer();
        sut.NormalizeRegistrableDomain("@example.com").Should().Be("example.com");
    }
}
