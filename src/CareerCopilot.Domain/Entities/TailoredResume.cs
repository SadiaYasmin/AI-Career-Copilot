using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Domain.Entities;

public sealed class TailoredResume : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid ResumeId { get; set; }
    public Guid JobId { get; set; }
    public TailoringMode Mode { get; set; } = TailoringMode.Balanced;
    public string Content { get; set; } = string.Empty;
    public string OriginalContent { get; set; } = string.Empty;
    public string Separator { get; set; } = string.Empty;
    public string ChangesSummary { get; set; } = string.Empty;
    public string? StorageReference { get; set; }
    public string? ExportFileName { get; set; }

    public User? User { get; set; }
    public Resume? Resume { get; set; }
    public Job? Job { get; set; }
}

public sealed class CoverLetter : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid JobId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Length { get; set; } = "Standard";
    public string Tone { get; set; } = "Professional";

    public User? User { get; set; }
    public Job? Job { get; set; }
}