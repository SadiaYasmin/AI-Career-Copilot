using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Domain.Entities;

public sealed class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;

    public UserProfile? Profile { get; set; }
    public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    public ICollection<SkillGap> SkillGaps { get; set; } = new List<SkillGap>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
    public ICollection<CareerRoadmap> CareerRoadmaps { get; set; } = new List<CareerRoadmap>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public ICollection<CopilotConversation> CopilotConversations { get; set; } = new List<CopilotConversation>();
    public ICollection<RecruiterReadinessScore> RecruiterReadinessScores { get; set; } = new List<RecruiterReadinessScore>();

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}