namespace EdgeFront.Builder.Features.Export;

/// <summary>
/// Carries the generated markdown content and the sanitized filename that the
/// HTTP layer should use as the <c>Content-Disposition</c> filename.
/// </summary>
/// <param name="FileName">
/// A filesystem-safe filename produced by <see cref="FileNameSanitizer"/>,
/// e.g. <c>My-Q1-Webinar-Series.md</c>.
/// </param>
/// <param name="Content">The complete markdown string ready for download.</param>
public record MarkdownExportResult(string FileName, string Content);
