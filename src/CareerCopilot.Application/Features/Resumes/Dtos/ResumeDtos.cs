namespace CareerCopilot.Application.Features.Resumes.Dtos;

public sealed record ResumeDto(
    Guid Id,
    string Name,
    string OriginalFileName,
    string FileType,
    bool IsDefault,
    DateTime UploadedAt,
    bool ParseFailed,
    int? ResumeScore,
    DateTime? AnalyzedAt);

public sealed record ResumeFileResponse(System.IO.MemoryStream Stream, string FileName, string ContentType);

public sealed record ResumeAnalysisDto(
    ResumeDto Resume,
    int Score,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Improvements,
    IReadOnlyList<string> AtRiskFindings,
    string Summary,
    bool UsedAi);