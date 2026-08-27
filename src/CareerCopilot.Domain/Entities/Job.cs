using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Domain.Entities;

public sealed class Job : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public bool IsAnalyzed { get; set; }
    public DateTime? AnalyzedAt { get; set; }

    public User? User { get; set; }
    public ICollection<JobRequirement> Requirements { get; set; } = new List<JobRequirement>();
    public ICollection<JobMatch> Matches { get; set; } = new List<JobMatch>();
    public ICollection<SkillGap> SkillGaps { get; set; } = new List<SkillGap>();
    public ICollection<CoverLetter> CoverLetters { get; set; } = new List<CoverLetter>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
}

public sealed class JobRequirement : AuditableEntity
{
    public Guid JobId { get; set; }
    public RequirementType RequirementType { get; set; } = RequirementType.Required;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Importance { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;

    public Job? Job { get; set; }
}