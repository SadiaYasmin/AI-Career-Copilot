namespace CareerCopilot.Application.Common.Ai;

public sealed record JobAnalysisContext(AiJobSnapshot Job);

public sealed record JobAnalysisResult(
    string Title,
    string Company,
    string Location,
    string EmploymentType,
    string ExperienceRequirement,
    string EducationRequirement,
    IReadOnlyList<AiJobRequirement> Requirements,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> Responsibilities);