namespace CareerCopilot.Domain.Entities;

using CareerCopilot.Domain.Enums;

public sealed class JobMatch : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid JobId { get; set; }
    public Guid? ResumeId { get; set; }
    public int OverallScore { get; set; }
    public int SkillsScore { get; set; }
    public int ExperienceScore { get; set; }
    public int EducationScore { get; set; }
    public int ProjectScore { get; set; }
    public int KeywordScore { get; set; }
    public int AlignmentScore { get; set; }
    public string StrongMatchesJson { get; set; } = "[]";
    public string PartialMatchesJson { get; set; } = "[]";
    public string MissingRequirementsJson { get; set; } = "[]";
    public string EvidenceJson { get; set; } = "[]";
    public string RecommendationsJson { get; set; } = "[]";
    public string Explanation { get; set; } = string.Empty;

    public User? User { get; set; }
    public Job? Job { get; set; }
    public Resume? Resume { get; set; }
}

public sealed class SkillGap : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid JobId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public GapType GapType { get; set; } = GapType.Missing;
    public SkillPriority Priority { get; set; } = SkillPriority.Medium;
    public string CurrentLevel { get; set; } = string.Empty;
    public string RequiredLevel { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string LearningPath { get; set; } = string.Empty;

    public User? User { get; set; }
    public Job? Job { get; set; }
}