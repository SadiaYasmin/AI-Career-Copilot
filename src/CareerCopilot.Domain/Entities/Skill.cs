namespace CareerCopilot.Domain.Entities;

public sealed class Skill : AuditableEntity
{
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Technical";
    public string Proficiency { get; set; } = string.Empty;

    public UserProfile? UserProfile { get; set; }
}