namespace CareerCopilot.Domain.Entities;

public sealed class Reminder : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? ApplicationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime FollowUpDate { get; set; }
    public string ContactInformation { get; set; } = string.Empty;
    public bool IsDone { get; set; }

    public User? User { get; set; }
    public Application? Application { get; set; }
}