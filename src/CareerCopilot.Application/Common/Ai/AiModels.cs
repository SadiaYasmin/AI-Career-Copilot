namespace CareerCopilot.Application.Common.Ai;

public sealed record AiJobRequirement(string Name, string RequirementType, string Importance, string SourceText);

public sealed record AiSkill(string Name, string Level);

public sealed record AiExperience(string Company, string Title, string Summary);

public sealed record AiProject(string Name, string Description, string Technologies);

public sealed record AiEducation(string Degree, string Institution);

public sealed record AiPersonSnapshot(
    string TargetRole,
    string Headline,
    string ProfessionalSummary,
    IReadOnlyList<AiSkill> Skills,
    IReadOnlyList<AiExperience> Experience,
    IReadOnlyList<AiProject> Projects,
    IReadOnlyList<AiEducation> Education,
    IReadOnlyList<string> Certifications,
    double YearsOfExperience,
    string CareerGoals);

public sealed record AiJobSnapshot(
    Guid JobId,
    string Title,
    string Company,
    string Location,
    string Description,
    IReadOnlyList<AiJobRequirement> Requirements);

public sealed record AiResumeSnapshot(
    Guid ResumeId,
    string Name,
    string FileName,
    string ParsedText,
    IReadOnlyList<string> Lines);