namespace CareerCopilot.Application.Features.JobMatching.Dtos;

public sealed record MatchEvidenceDto(string Name, string Status, string Source, string Detail);

public sealed record JobMatchDto(
    Guid Id,
    Guid JobId,
    Guid? ResumeId,
    int OverallScore,
    int SkillsScore,
    int ExperienceScore,
    int EducationScore,
    int ProjectScore,
    int KeywordScore,
    int AlignmentScore,
    IReadOnlyList<string> StrongMatches,
    IReadOnlyList<string> PartialMatches,
    IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<MatchEvidenceDto> Evidence,
    IReadOnlyList<string> Recommendations,
    string Explanation,
    DateTime CreatedAt);