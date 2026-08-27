using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Domain.Entities;

public sealed class CopilotConversation : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? JobId { get; set; }
    public string? Title { get; set; }
    public Guid? ResumeId { get; set; }

    public User? User { get; set; }
    public Job? Job { get; set; }
    public ICollection<CopilotMessage> Messages { get; set; } = new List<CopilotMessage>();
}

public sealed class CopilotMessage : AuditableEntity
{
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ContextType { get; set; }

    public CopilotConversation? Conversation { get; set; }
}