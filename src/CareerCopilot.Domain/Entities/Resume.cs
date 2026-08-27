namespace CareerCopilot.Domain.Entities;

public sealed class Resume : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string StorageReference { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? ParsedText { get; set; }
    public bool ParseFailed { get; set; }
    public string? ResumeAnalysisJson { get; set; }
    public int? ResumeScore { get; set; }
    public DateTime? AnalyzedAt { get; set; }

    public User? User { get; set; }
    public ICollection<TailoredResume> TailoredResumes { get; set; } = new List<TailoredResume>();

    public void MarkAsDefault() => IsDefault = true;

    public void ClearDefault() => IsDefault = false;
}