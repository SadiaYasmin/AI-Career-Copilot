namespace CareerCopilot.Application.Scoring;

/// <summary>
/// Configurable weights for the transparent job match scoring model.
/// Defaults match the PRD reference model.
/// </summary>
public sealed class MatchScoringOptions
{
    public const string SectionName = "MatchScoring";

    public int Skills { get; set; } = 30;
    public int Experience { get; set; } = 20;
    public int Projects { get; set; } = 15;
    public int Education { get; set; } = 10;
    public int Keywords { get; set; } = 10;
    public int Alignment { get; set; } = 15;
}

public sealed record MatchScoreBreakdown(
    int Overall,
    int Skills,
    int Experience,
    int Education,
    int Projects,
    int Keywords,
    int Alignment);

public sealed record MatchItemFinding(string Name, string Status, string Source, string Detail);

public sealed record MatchResult(
    MatchScoreBreakdown Scores,
    IReadOnlyList<string> StrongMatches,
    IReadOnlyList<string> PartialMatches,
    IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<MatchItemFinding> Evidence,
    IReadOnlyList<string> Recommendations,
    string Explanation,
    DateTime GeneratedAt);