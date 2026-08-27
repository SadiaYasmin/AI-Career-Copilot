using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Domain.Entities;

public sealed class UserProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public CareerLevel CareerLevel { get; set; } = CareerLevel.FreshGraduate;
    public double YearsOfExperience { get; set; }
    public WorkType PreferredWorkType { get; set; } = WorkType.OnSite;
    public string PreferredLocation { get; set; } = string.Empty;
    public string TargetRole { get; set; } = string.Empty;
    public string TargetIndustries { get; set; } = string.Empty;
    public string ProfessionalSummary { get; set; } = string.Empty;
    public string CareerGoals { get; set; } = string.Empty;
    public string GitHubUrl { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;

    public User? User { get; set; }
    public ICollection<Education> Education { get; set; } = new List<Education>();
    public ICollection<Experience> Experiences { get; set; } = new List<Experience>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public ICollection<Certification> Certifications { get; set; } = new List<Certification>();
    public ICollection<CareerGoal> Goals { get; set; } = new List<CareerGoal>();
    public LinkedInProfile? LinkedInProfile { get; set; }
}