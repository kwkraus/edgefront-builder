using System.Text.RegularExpressions;

namespace EdgeFront.Builder.Features.Export;

/// <summary>
/// Provides static helpers for converting arbitrary series titles into
/// filesystem-safe, markdown-ready filenames.
/// </summary>
/// <remarks>
/// Sanitization pipeline (applied in order):
/// <list type="number">
///   <item>Replace every character that is not alphanumeric, a hyphen, an underscore,
///         or a space with a hyphen.</item>
///   <item>Replace spaces with hyphens.</item>
///   <item>Collapse consecutive hyphens into a single hyphen.</item>
///   <item>Trim leading and trailing hyphens and whitespace.</item>
///   <item>Truncate the slug to a maximum of 100 characters.</item>
///   <item>Fall back to <c>series-export</c> when the slug is empty.</item>
///   <item>Append the <c>.md</c> extension.</item>
/// </list>
/// </remarks>
public static class FileNameSanitizer
{
    private const int MaxSlugLength = 100;
    private const string FallbackSlug = "series-export";
    private const string Extension = ".md";

    // Pre-compiled patterns for performance — static readonly fields are
    // initialized once per AppDomain, avoiding repeated regex JIT compilation.

    /// <summary>Matches any character that is not alphanumeric, a hyphen, an underscore, or a space.</summary>
    private static readonly Regex InvalidCharsPattern =
        new(@"[^a-zA-Z0-9\-_ ]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>Matches two or more consecutive hyphens (after spaces have been replaced).</summary>
    private static readonly Regex ConsecutiveHyphensPattern =
        new(@"-{2,}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Converts <paramref name="title"/> into a filesystem-safe markdown filename.
    /// </summary>
    /// <param name="title">The raw series title. May be <see langword="null"/>, empty, or whitespace.</param>
    /// <returns>
    /// A sanitized filename that includes the <c>.md</c> extension,
    /// e.g. <c>"My-Q1-Webinar-Series.md"</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// FileNameSanitizer.Sanitize("My Q1 Webinar Series")  // => "My-Q1-Webinar-Series.md"
    /// FileNameSanitizer.Sanitize("Series: 2024/Q2")       // => "Series-2024-Q2.md"
    /// FileNameSanitizer.Sanitize("")                       // => "series-export.md"
    /// FileNameSanitizer.Sanitize("   ")                   // => "series-export.md"
    /// FileNameSanitizer.Sanitize("A/B\C*D?E""F<G>H|I")   // => "A-B-C-D-E-F-G-H-I.md"
    /// </code>
    /// </example>
    public static string Sanitize(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return FallbackSlug + Extension;

        // Step 1 -- replace disallowed characters with hyphens.
        var slug = InvalidCharsPattern.Replace(title, "-");

        // Step 2 -- replace spaces with hyphens.
        slug = slug.Replace(' ', '-');

        // Step 3 -- collapse consecutive hyphens into a single hyphen.
        slug = ConsecutiveHyphensPattern.Replace(slug, "-");

        // Step 4 -- trim leading and trailing hyphens and whitespace.
        slug = slug.Trim('-', ' ');

        // Step 5 -- truncate to the maximum allowed slug length.
        if (slug.Length > MaxSlugLength)
            slug = slug[..MaxSlugLength];

        // Re-trim in case truncation landed precisely on a hyphen.
        slug = slug.Trim('-');

        // Step 6 -- fall back to a safe default when nothing usable remains.
        if (string.IsNullOrEmpty(slug))
            slug = FallbackSlug;

        // Step 7 -- append the markdown extension.
        return slug + Extension;
    }
}
