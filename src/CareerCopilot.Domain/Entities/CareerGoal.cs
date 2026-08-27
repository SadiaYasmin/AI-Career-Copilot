namespace CareerCopilot.Domain.Entities;

public sealed class CareerGoal : AuditableEntity
{
    public Guid UserProfileId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;

    public UserProfile? UserProfile { get; set; }
}