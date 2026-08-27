using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Application.Features.Tailoring.Dtos;

public sealed record TailoredResumeDto(
    Guid Id,
    Guid ResumeId,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string Mode,
    string ChangesSummary,
    DateTime CreatedAt);

public sealed record TailoredResumeDetailDto(
    Guid Id,
    Guid ResumeId,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string Mode,
    string Content,
    string OriginalContent,
    string Separator,
    string ChangesSummary,
    DateTime CreatedAt);

public sealed record CoverLetterDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string Content,
    string Length,
    string Tone,
    DateTime CreatedAt);

public sealed record GenerateTailoredResumeCommand(
    Guid ResumeId,
    Guid JobId,
    TailoringMode Mode) : MediatR.IRequest<TailoredResumeDetailDto>;

public sealed record GenerateCoverLetterCommand(
    Guid JobId,
    string Length,
    string Tone) : MediatR.IRequest<CoverLetterDto>;