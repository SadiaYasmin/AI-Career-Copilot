namespace CareerCopilot.Domain.Entities;

public sealed class RecruiterReadinessScore : AuditableEntity
{
    public Guid UserId { get; set; }
    public int OverallScore { get; set; }
    public int? ResumeScore { get; set; }
    public int? SkillsScore { get; set; }
    public int? ProjectsScore { get; set; }
    public int? ProfileScore { get; set; }
    public int? InterviewScore { get; set; }
    public string ReportJson { get; set; } = "{}";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}