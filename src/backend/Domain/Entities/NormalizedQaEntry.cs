namespace EdgeFront.Builder.Domain.Entities;

public class NormalizedQaEntry
{
    public Guid QaEntryId { get; set; }
    public Guid SessionId { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public DateTime? AskedAt { get; set; }
    public string? AskedByDisplayName { get; set; }
    public string? AskedByEmail { get; set; }
    public bool IsAnswered { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public string? AnswerText { get; set; }
}
