namespace CareerCopilot.Domain.Entities;

using CareerCopilot.Domain.Enums;

public sealed class CareerRoadmap : AuditableEntity
{
    public Guid UserId { get; set; }
    public string TargetRole { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public User? User { get; set; }
    public ICollection<RoadmapTask> Tasks { get; set; } = new List<RoadmapTask>();
}

public sealed class RoadmapTask : AuditableEntity
{
    public Guid CareerRoadmapId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SkillPriority Priority { get; set; } = SkillPriority.Medium;
    public RoadmapTaskStatus Status { get; set; } = RoadmapTaskStatus.Pending;
    public DateTime? DueDate { get; set; }
    public string Skill { get; set; } = string.Empty;

    public CareerRoadmap? CareerRoadmap { get; set; }
}