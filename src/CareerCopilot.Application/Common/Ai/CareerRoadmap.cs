namespace CareerCopilot.Application.Common.Ai;

public sealed record CareerRoadmapContext(
    AiPersonSnapshot Person,
    string TargetRole,
    IReadOnlyList<string> SkillGaps,
    IReadOnlyList<string> Recommendations);

public sealed record AiRoadmapTask(
    string Title,
    string Description,
    string Month,
    string Skill,
    string Priority);

public sealed record CareerRoadmapResult(
    string TargetRole,
    string Description,
    IReadOnlyList<AiRoadmapTask> Tasks);