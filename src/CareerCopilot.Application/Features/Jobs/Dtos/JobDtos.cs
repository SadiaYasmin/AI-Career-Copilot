using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Application.Features.Jobs.Dtos;

public sealed record JobRequirementDto(
    Guid Id,
    RequirementType RequirementType,
    string Name,
    string Description,
    string Importance,
    string SourceText);

public sealed record JobDto(
    Guid Id,
    string Title,
    string CompanyName,
    string Location,
    string EmploymentType,
    string SourceUrl,
    bool IsAnalyzed,
    DateTime? AnalyzedAt,
    DateTime CreatedAt,
    int? LatestMatchScore);

public sealed record JobDetailDto(
    Guid Id,
    string Title,
    string CompanyName,
    string Location,
    string EmploymentType,
    string Description,
    string SourceUrl,
    bool IsAnalyzed,
    DateTime? AnalyzedAt,
    DateTime CreatedAt,
    int? LatestMatchScore,
    int ApplicationsCount,
    IReadOnlyList<JobRequirementDto> Requirements);

public sealed record CreateJobCommand(
    string Title,
    string CompanyName,
    string Location,
    string EmploymentType,
    string Description,
    string SourceUrl) : MediatR.IRequest<JobDto>;

public sealed record UpdateJobCommand(
    Guid Id,
    string Title,
    string CompanyName,
    string Location,
    string EmploymentType,
    string Description,
    string SourceUrl) : MediatR.IRequest<JobDto>;

public sealed record AnalyzeJobCommand(Guid Id) : MediatR.IRequest<JobDetailDto>;