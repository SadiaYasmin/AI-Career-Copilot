namespace CareerCopilot.Application.Common.Ai;

public sealed record LinkedInContext(
    string Headline,
    string About,
    string ExperienceText,
    string SkillsText,
    string TargetRole);

public sealed record LinkedInAnalysisResult(
    IReadOnlyList<LinkedInSuggestion> Suggestions,
    IReadOnlyList<string> Strengths,
    int Score);

public sealed record LinkedInSuggestion(string Section, string Original, string Improved, string Reasoning);