namespace EdgeFront.Builder.Domain.Entities;

public class SessionImportSummary
{
    public Guid SessionImportSummaryId { get; set; }
    public Guid SessionId { get; set; }
    public SessionImportType ImportType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public DateTime ImportedAt { get; set; }
}
