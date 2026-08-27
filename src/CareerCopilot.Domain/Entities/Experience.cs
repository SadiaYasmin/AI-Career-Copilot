namespace CareerCopilot.Domain.Entities;

public sealed class Experience : AuditableEntity
{
    public Guid UserProfileId { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Responsibilities { get; set; } = string.Empty;
    public string Achievements { get; set; } = string.Empty;

    public UserProfile? UserProfile { get; set; }
}