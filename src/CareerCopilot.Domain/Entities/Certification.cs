namespace CareerCopilot.Domain.Entities;

public sealed class Certification : AuditableEntity
{
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string DateObtained { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public UserProfile? UserProfile { get; set; }
}