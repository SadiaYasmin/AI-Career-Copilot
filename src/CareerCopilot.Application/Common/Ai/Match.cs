namespace CareerCopilot.Application.Common.Ai;

public sealed record MatchAiContext(
    AiPersonSnapshot Person,
    AiResumeSnapshot? Resume,
    AiJobSnapshot Job,
    int OverallScore,
    int SkillsScore,
    int ExperienceScore,
    int EducationScore,
    int ProjectScore,
    int KeywordScore,
    int AlignmentScore,
    IReadOnlyList<string> StrongMatches,
    IReadOnlyList<string> PartialMatches,
    IReadOnlyList<string> Missing);

public sealed record MatchAiResult(
    IReadOnlyList<MatchItem> Matches,
    IReadOnlyList<string> Recommendations,
    string Explanation);

public sealed record MatchItem(string Name, string Status, string Evidence);