namespace CareerCopilot.Domain.Entities;

public sealed class LinkedInProfile : AuditableEntity
{
    public Guid UserProfileId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string About { get; set; } = string.Empty;
    public string ExperienceText { get; set; } = string.Empty;
    public string SkillsText { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;

    public UserProfile? UserProfile { get; set; }
}