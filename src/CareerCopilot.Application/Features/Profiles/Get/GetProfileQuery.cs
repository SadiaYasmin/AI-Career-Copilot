using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Profiles.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Profiles.Get;

public sealed record GetProfileQuery : IRequest<ProfileDto>;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetProfileQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var profile = await _db.Set<UserProfile>()
            .Where(p => p.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Career profile not found. Create your profile to continue.");

        var education = await _db.Set<Education>()
            .Where(e => e.UserProfileId == profile.Id)
            .OrderBy(e => e.StartDate)
            .Select(e => new EducationDto(e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate, e.Description))
            .ToListAsync(cancellationToken);

        var experiences = await _db.Set<Experience>()
            .Where(e => e.UserProfileId == profile.Id)
            .OrderBy(e => e.StartDate)
            .Select(e => new ExperienceDto(e.Company, e.Title, e.Location, e.StartDate, e.EndDate, e.IsCurrent,
                e.Description, e.Responsibilities, e.Achievements))
            .ToListAsync(cancellationToken);

        var projects = await _db.Set<Project>()
            .Where(p => p.UserProfileId == profile.Id)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new ProjectDto(p.Name, p.Description, p.Technologies, p.Role, p.Url, p.StartDate, p.EndDate, p.Highlights))
            .ToListAsync(cancellationToken);

        var skills = await _db.Set<Skill>()
            .Where(s => s.UserProfileId == profile.Id)
            .Select(s => new SkillDto(s.Name, s.Category, s.Proficiency))
            .ToListAsync(cancellationToken);

        var certifications = await _db.Set<Certification>()
            .Where(c => c.UserProfileId == profile.Id)
            .Select(c => new CertificationDto(c.Name, c.Issuer, c.DateObtained, c.Url))
            .ToListAsync(cancellationToken);

        var goals = await _db.Set<CareerGoal>()
            .Where(g => g.UserProfileId == profile.Id)
            .Select(g => new CareerGoalDto(g.Description, g.Timeframe))
            .ToListAsync(cancellationToken);

        var linkedIn = await _db.Set<LinkedInProfile>()
            .Where(l => l.UserProfileId == profile.Id)
            .Select(l => new LinkedInDto(l.Url, l.Headline, l.About, l.ExperienceText, l.SkillsText))
            .FirstOrDefaultAsync(cancellationToken);

        return new ProfileDto(
            profile.UserId, profile.FullName, profile.Email, profile.Headline, profile.Phone, profile.Location,
            profile.CareerLevel, profile.YearsOfExperience, profile.PreferredWorkType, profile.PreferredLocation,
            profile.TargetRole, profile.TargetIndustries, profile.ProfessionalSummary, profile.CareerGoals,
            profile.GitHubUrl, profile.LinkedInUrl, profile.PortfolioUrl,
            education, experiences, projects, skills, certifications, goals, linkedIn);
    }
}