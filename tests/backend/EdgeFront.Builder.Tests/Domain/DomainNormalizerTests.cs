using EdgeFront.Builder.Domain;
using FluentAssertions;

namespace EdgeFront.Builder.Tests.Domain;

public class DomainNormalizerTests
{
    [Theory]
    [InlineData("User@Example.COM", "user@example.com")]
    [InlineData("  Alice@Test.org  ", "alice@test.org")]
    [InlineData("BOB@Sub.Domain.Net", "bob@sub.domain.net")]
    public void NormalizeEmail_ShouldLowercaseAndTrim(string input, string expected)
    {
        var result = DomainNormalizer.NormalizeEmail(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("user@example.com", "example.com")]
    [InlineData("USER@EXAMPLE.COM", "example.com")]
    [InlineData("  user@Sub.Example.COM  ", "sub.example.com")]
    public void NormalizeEmailDomain_ShouldExtractAndLowercaseDomain(string email, string expected)
    {
        var result = DomainNormalizer.NormalizeEmailDomain(email);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("sub.example.com", "example.com")]
    [InlineData("deep.sub.example.com", "example.com")]
    [InlineData("another.test.org", "test.org")]
    public void NormalizeRegistrableDomain_ShouldStripSubdomainsToTwoLabels(string domain, string expected)
    {
        var result = DomainNormalizer.NormalizeRegistrableDomain(domain);
        result.Should().Be(expected);
    }

    [Fact]
    public void NormalizeRegistrableDomain_SingleLabel_ShouldReturnAsIs()
    {
        var result = DomainNormalizer.NormalizeRegistrableDomain("localhost");
        result.Should().Be("localhost");
    }

    [Theory]
    [InlineData("  EXAMPLE.COM  ", "example.com")]
    [InlineData("Example.Com", "example.com")]
    public void NormalizeRegistrableDomain_ShouldTrimAndLowercase(string domain, string expected)
    {
        DomainNormalizer.NormalizeRegistrableDomain(domain).Should().Be(expected);
    }

    [Fact]
    public void NormalizeEmailDomain_NoAtSymbol_ReturnsFullStringLowered()
    {
        DomainNormalizer.NormalizeEmailDomain("natsymbol").Should().Be("natsymbol");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeEmail_EmptyOrWhitespace_ReturnsTrimmedLower(string input)
    {
        DomainNormalizer.NormalizeEmail(input).Should().Be(input.Trim().ToLowerInvariant());
    }

    [Fact]
    public void NormalizeRegistrableDomain_TwoLabels_ReturnsUnchanged()
    {
        DomainNormalizer.NormalizeRegistrableDomain("example.com").Should().Be("example.com");
    }
}
