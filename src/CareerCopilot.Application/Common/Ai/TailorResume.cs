namespace CareerCopilot.Application.Common.Ai;

public sealed record TailorResumeContext(
    AiResumeSnapshot Resume,
    AiJobSnapshot Job,
    string Mode,
    IReadOnlyList<string> StrongMatches,
    IReadOnlyList<string> Missing);

public sealed record TailorResumeResult(string Content, string ChangesSummary);