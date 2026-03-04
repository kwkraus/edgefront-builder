using EdgeFront.Builder.Domain;
using FluentAssertions;

namespace EdgeFront.Builder.Tests.Domain;

public class InternalDomainFilterTests
{
    private readonly InternalDomainFilter _filter = new(["contoso.com", "fabrikam.org"]);

    [Theory]
    [InlineData("contoso.com", true)]
    [InlineData("CONTOSO.COM", true)]
    [InlineData("fabrikam.org", true)]
    [InlineData("external.com", false)]
    [InlineData("notcontoso.com", false)]
    public void IsInternal_ShouldMatchCaseInsensitively(string domain, bool expected)
    {
        _filter.IsInternal(domain).Should().Be(expected);
    }

    [Fact]
    public void IsInternal_EmptyList_ShouldAlwaysReturnFalse()
    {
        var filter = new InternalDomainFilter([]);
        filter.IsInternal("anything.com").Should().BeFalse();
    }

    [Fact]
    public void Constructor_NormalizesDomains_ToLowercase()
    {
        var filter = new InternalDomainFilter(["UPPER.COM", "Mixed.Org"]);
        filter.IsInternal("upper.com").Should().BeTrue();
        filter.IsInternal("mixed.org").Should().BeTrue();
    }

    [Fact]
    public void IsInternal_EmptyString_ReturnsFalse()
    {
        _filter.IsInternal("").Should().BeFalse();
    }
}
