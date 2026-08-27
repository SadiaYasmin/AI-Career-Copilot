using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Application.Features.Applications.Dtos;

public sealed record ApplicationDto(
    Guid Id,
    Guid? JobId,
    string JobTitle,
    string CompanyName,
    ApplicationStatus Status,
    string Source,
    DateTime? AppliedAt,
    string ResumeName,
    int? MatchScore,
    DateTime? UpdatedAt);

public sealed record ApplicationDetailDto(
    Guid Id,
    Guid? JobId,
    string JobTitle,
    string CompanyName,
    string JobUrl,
    string Location,
    ApplicationStatus Status,
    string Source,
    DateTime? AppliedAt,
    DateTime? FollowUpDate,
    string Notes,
    Guid? ResumeId,
    string ResumeName,
    Guid? CoverLetterId,
    int? MatchScore,
    int InterviewCount,
    DateTime? LastInterviewAt);

public sealed record CreateApplicationCommand(
    Guid? JobId,
    string CompanyName,
    string JobTitle,
    string JobUrl,
    string Location,
    string? JobDescription,
    ApplicationStatus Status,
    string Source,
    DateTime? AppliedAt,
    Guid? ResumeId,
    Guid? CoverLetterId) : MediatR.IRequest<ApplicationDto>;

public sealed record UpdateApplicationDetailsCommand(
    Guid Id,
    string? Notes,
    DateTime? FollowUpDate) : MediatR.IRequest<ApplicationDetailDto>;

public sealed record UpdateApplicationStatusCommand(Guid Id, ApplicationStatus NewStatus) : MediatR.IRequest<ApplicationDetailDto>;