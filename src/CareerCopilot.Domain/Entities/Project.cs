namespace CareerCopilot.Domain.Entities;

public sealed class Project : AuditableEntity
{
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Technologies { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Highlights { get; set; } = string.Empty;

    public UserProfile? UserProfile { get; set; }
}