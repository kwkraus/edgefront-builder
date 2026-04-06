using EdgeFront.Builder.Common.Extensions;
using System.Text;

namespace EdgeFront.Builder.Features.Export;

public static class ExportEndpoints
{
    public static WebApplication MapExportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/series").RequireAuthorization();

        group.MapGet("/{id:guid}/export/markdown", async (
            Guid id,
            MarkdownExportService exportService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserOid();
            if (userId is null)
                return Results.Unauthorized();

            var result = await exportService.ExportSeriesAsync(id, userId, ct);
            if (result is null)
                return Results.NotFound();

            var bytes = Encoding.UTF8.GetBytes(result.Content);
            return Results.Bytes(bytes, "text/markdown; charset=utf-8", result.FileName);
        })
        .WithName("ExportSeriesMarkdown")
        .WithTags("Export")
        .WithSummary("Download series as markdown");

        return app;
    }
}
