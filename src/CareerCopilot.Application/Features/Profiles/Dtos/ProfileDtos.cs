using CareerCopilot.Domain.Enums;
using MediatR;

namespace CareerCopilot.Application.Features.Profiles.Dtos;

public sealed record EducationDto(
    string Institution, string Degree, string FieldOfStudy,
    string StartDate, string EndDate, string Description);

public sealed record ExperienceDto(
    string Company, string Title, string Location,
    string StartDate, string EndDate, bool IsCurrent,
    string Description, string Responsibilities, string Achievements);

public sealed record ProjectDto(
    string Name, string Description, string Technologies, string Role,
    string Url, string StartDate, string EndDate, string Highlights);

public sealed record SkillDto(string Name, string Category, string Proficiency);

public sealed record CertificationDto(string Name, string Issuer, string DateObtained, string Url);

public sealed record CareerGoalDto(string Description, string Timeframe);

public sealed record LinkedInDto(
    string Url, string Headline, string About,
    string ExperienceText, string SkillsText);

public sealed record ProfileDto(
    Guid UserId,
    string FullName,
    string Email,
    string Headline,
    string Phone,
    string Location,
    CareerLevel CareerLevel,
    double YearsOfExperience,
    WorkType PreferredWorkType,
    string PreferredLocation,
    string TargetRole,
    string TargetIndustries,
    string ProfessionalSummary,
    string CareerGoals,
    string GitHubUrl,
    string LinkedInUrl,
    string PortfolioUrl,
    IReadOnlyList<EducationDto> Education,
    IReadOnlyList<ExperienceDto> Experiences,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<SkillDto> Skills,
    IReadOnlyList<CertificationDto> Certifications,
    IReadOnlyList<CareerGoalDto> Goals,
    LinkedInDto? LinkedInProfile);

public sealed record UpdateProfileCommand(
    string FullName,
    string Headline,
    string Phone,
    string Location,
    CareerLevel CareerLevel,
    double YearsOfExperience,
    WorkType PreferredWorkType,
    string PreferredLocation,
    string TargetRole,
    string TargetIndustries,
    string ProfessionalSummary,
    string CareerGoals,
    string GitHubUrl,
    string LinkedInUrl,
    string PortfolioUrl,
    IReadOnlyList<EducationDto> Education,
    IReadOnlyList<ExperienceDto> Experiences,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<SkillDto> Skills,
    IReadOnlyList<CertificationDto> Certifications,
    IReadOnlyList<CareerGoalDto> Goals,
    LinkedInDto? LinkedInProfile) : IRequest<ProfileDto>;