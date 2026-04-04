using EdgeFront.Builder.Features.Export;
using FluentAssertions;

namespace EdgeFront.Builder.Tests.Features.Export;

public class FileNameSanitizerTests
{
    // ── 1. Normal title ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("My Webinar Series", "My-Webinar-Series.md")]
    [InlineData("Q1 2025 Kickoff", "Q1-2025-Kickoff.md")]
    [InlineData("Single", "Single.md")]
    [InlineData("Already-Hyphenated", "Already-Hyphenated.md")]
    [InlineData("With_Underscore", "With_Underscore.md")]
    public void Sanitize_ReturnsExpectedFileName_ForNormalTitles(string input, string expected)
    {
        FileNameSanitizer.Sanitize(input).Should().Be(expected);
    }

    // ── 2. Title with forward slash ───────────────────────────────────────────

    [Fact]
    public void Sanitize_ReplacesForwardSlash_WithHyphen()
    {
        // "Series/2024" → forward slash replaced, collapse not needed here
        FileNameSanitizer.Sanitize("Series/2024")
            .Should().Be("Series-2024.md");
    }

    // ── 3. All special filesystem chars replaced, consecutive hyphens collapsed

    [Theory]
    [InlineData(@"A/B",       "A-B.md")]       // forward slash
    [InlineData(@"A\B",       "A-B.md")]       // back slash
    [InlineData("A:B",        "A-B.md")]       // colon
    [InlineData("A*B",        "A-B.md")]       // asterisk
    [InlineData("A?B",        "A-B.md")]       // question mark
    [InlineData("A\"B",       "A-B.md")]       // double quote
    [InlineData("A<B",        "A-B.md")]       // less-than
    [InlineData("A>B",        "A-B.md")]       // greater-than
    [InlineData("A|B",        "A-B.md")]       // pipe
    [InlineData(@"A/B\C:D",   "A-B-C-D.md")]  // multiple different specials
    public void Sanitize_ReplacesSpecialChars_WithHyphens(string input, string expected)
    {
        FileNameSanitizer.Sanitize(input).Should().Be(expected);
    }

    // ── 3b. Consecutive hyphens collapsed to one ──────────────────────────────

    [Fact]
    public void Sanitize_CollapsesConsecutiveHyphens()
    {
        // "A//B" → "A" + "--" + "B" → collapse → "A-B"
        FileNameSanitizer.Sanitize("A//B").Should().Be("A-B.md");
    }

    [Fact]
    public void Sanitize_CollapsesConsecutiveHyphens_FromMixedSpecials()
    {
        // "A: /B" → "A" + ":-space" → each becomes hyphen → "A---B" → "A-B"
        FileNameSanitizer.Sanitize("A: /B").Should().Be("A-B.md");
    }

    // ── 4. Empty string ───────────────────────────────────────────────────────

    [Fact]
    public void Sanitize_ReturnsFallback_ForEmptyString()
    {
        FileNameSanitizer.Sanitize(string.Empty).Should().Be("series-export.md");
    }

    // ── 5. Whitespace only ────────────────────────────────────────────────────

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Sanitize_ReturnsFallback_ForWhitespaceOnly(string input)
    {
        FileNameSanitizer.Sanitize(input).Should().Be("series-export.md");
    }

    // ── 6. Title > 100 chars truncated ────────────────────────────────────────

    [Fact]
    public void Sanitize_TruncatesSlug_To100Characters()
    {
        // 120 'a' characters — slug should be truncated to 100 chars before ".md"
        var longTitle = new string('a', 120);
        var result = FileNameSanitizer.Sanitize(longTitle);

        // The slug portion (before ".md") must be at most 100 characters
        var slug = result[..^3]; // strip ".md"
        slug.Length.Should().BeLessThanOrEqualTo(100,
            because: "the sanitizer must truncate slugs longer than 100 characters");
        result.Should().EndWith(".md");
    }

    [Fact]
    public void Sanitize_TruncatesSlug_ExactlyAt100Chars_WhenInputIsExactly100()
    {
        var title100 = new string('b', 100);
        var result = FileNameSanitizer.Sanitize(title100);

        result.Should().Be(new string('b', 100) + ".md");
    }

    [Fact]
    public void Sanitize_TruncatesSlug_ExactlyAt100Chars_WhenInputIsSpaceSeparatedWords()
    {
        // 50 two-letter words separated by spaces → 50 * 2 + 49 spaces = 149 chars raw
        // After space→hyphen replacement and truncation the slug ≤ 100 chars.
        var title = string.Join(" ", Enumerable.Repeat("ab", 50));
        var result = FileNameSanitizer.Sanitize(title);

        var slug = result[..^3]; // strip ".md"
        slug.Length.Should().BeLessThanOrEqualTo(100);
        result.Should().EndWith(".md");
    }

    // ── 7. Title that becomes empty after sanitization ────────────────────────

    [Theory]
    [InlineData("/")]
    [InlineData("///")]
    [InlineData(@"/\:*?""<>|")]
    [InlineData("---")]
    public void Sanitize_ReturnsFallback_WhenSlugBecomesEmptyAfterSanitization(string input)
    {
        FileNameSanitizer.Sanitize(input).Should().Be("series-export.md");
    }
}
