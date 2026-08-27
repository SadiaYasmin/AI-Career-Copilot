namespace CareerCopilot.Application.Common.Ai;

public sealed record ResumeAnalysisContext(
    AiResumeSnapshot Resume,
    AiPersonSnapshot Person);

public sealed record ResumeAnalysisResult(
    int Score,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Improvements,
    IReadOnlyList<string> AtRiskFindings,
    string Summary,
    bool UsedAi);