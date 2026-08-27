namespace CareerCopilot.Domain.Entities;

public sealed class Education : AuditableEntity
{
    public Guid UserProfileId { get; set; }
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string FieldOfStudy { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public UserProfile? UserProfile { get; set; }
}