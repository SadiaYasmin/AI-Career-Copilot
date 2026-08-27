using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Profiles.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Profiles.Update;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ProfileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateProfileCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var profile = await _db.Set<UserProfile>()
            .Where(p => p.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Career profile not found.");

        profile.FullName = request.FullName?.Trim() ?? string.Empty;
        profile.Headline = request.Headline ?? string.Empty;
        profile.Phone = request.Phone ?? string.Empty;
        profile.Location = request.Location ?? string.Empty;
        profile.CareerLevel = request.CareerLevel;
        profile.YearsOfExperience = request.YearsOfExperience;
        profile.PreferredWorkType = request.PreferredWorkType;
        profile.PreferredLocation = request.PreferredLocation ?? string.Empty;
        profile.TargetRole = request.TargetRole ?? string.Empty;
        profile.TargetIndustries = request.TargetIndustries ?? string.Empty;
        profile.ProfessionalSummary = request.ProfessionalSummary ?? string.Empty;
        profile.CareerGoals = request.CareerGoals ?? string.Empty;
        profile.GitHubUrl = request.GitHubUrl ?? string.Empty;
        profile.LinkedInUrl = request.LinkedInUrl ?? string.Empty;
        profile.PortfolioUrl = request.PortfolioUrl ?? string.Empty;
        profile.UpdatedAt = DateTime.UtcNow;

        await ReplaceChildrenAsync(profile.Id, request, cancellationToken);

        _db.Update(profile);
        await _db.SaveChangesAsync(cancellationToken);

        return await BuildDto(profile);
    }

    private async Task ReplaceChildrenAsync(
        Guid profileId,
        UpdateProfileCommand request,
        CancellationToken ct)
    {
        var existingEducation = _db.Set<Education>().Where(e => e.UserProfileId == profileId);
        _db.RemoveRange(existingEducation);

        var existingExperience = _db.Set<Experience>().Where(e => e.UserProfileId == profileId);
        _db.RemoveRange(existingExperience);

        var existingProjects = _db.Set<Project>().Where(p => p.UserProfileId == profileId);
        _db.RemoveRange(existingProjects);

        var existingSkills = _db.Set<Skill>().Where(s => s.UserProfileId == profileId);
        _db.RemoveRange(existingSkills);

        var existingCerts = _db.Set<Certification>().Where(c => c.UserProfileId == profileId);
        _db.RemoveRange(existingCerts);

        var existingGoals = _db.Set<CareerGoal>().Where(g => g.UserProfileId == profileId);
        _db.RemoveRange(existingGoals);

        var existingLinkedIn = _db.Set<LinkedInProfile>().Where(l => l.UserProfileId == profileId);
        _db.RemoveRange(existingLinkedIn);

        foreach (var item in request.Education ?? Array.Empty<EducationDto>())
        {
            _db.Add(new Education
            {
                UserProfileId = profileId,
                Institution = item.Institution ?? string.Empty,
                Degree = item.Degree ?? string.Empty,
                FieldOfStudy = item.FieldOfStudy ?? string.Empty,
                StartDate = item.StartDate ?? string.Empty,
                EndDate = item.EndDate ?? string.Empty,
                Description = item.Description ?? string.Empty
            });
        }

        foreach (var item in request.Experiences ?? Array.Empty<ExperienceDto>())
        {
            _db.Add(new Experience
            {
                UserProfileId = profileId,
                Company = item.Company ?? string.Empty,
                Title = item.Title ?? string.Empty,
                Location = item.Location ?? string.Empty,
                StartDate = item.StartDate ?? string.Empty,
                EndDate = item.EndDate ?? string.Empty,
                IsCurrent = item.IsCurrent,
                Description = item.Description ?? string.Empty,
                Responsibilities = item.Responsibilities ?? string.Empty,
                Achievements = item.Achievements ?? string.Empty
            });
        }

        foreach (var item in request.Projects ?? Array.Empty<ProjectDto>())
        {
            _db.Add(new Project
            {
                UserProfileId = profileId,
                Name = item.Name ?? string.Empty,
                Description = item.Description ?? string.Empty,
                Technologies = item.Technologies ?? string.Empty,
                Role = item.Role ?? string.Empty,
                Url = item.Url ?? string.Empty,
                StartDate = item.StartDate ?? string.Empty,
                EndDate = item.EndDate ?? string.Empty,
                Highlights = item.Highlights ?? string.Empty
            });
        }

        foreach (var item in request.Skills ?? Array.Empty<SkillDto>())
        {
            _db.Add(new Skill
            {
                UserProfileId = profileId,
                Name = item.Name?.Trim() ?? string.Empty,
                Category = item.Category ?? "Technical",
                Proficiency = item.Proficiency ?? string.Empty
            });
        }

        foreach (var item in request.Certifications ?? Array.Empty<CertificationDto>())
        {
            _db.Add(new Certification
            {
                UserProfileId = profileId,
                Name = item.Name ?? string.Empty,
                Issuer = item.Issuer ?? string.Empty,
                DateObtained = item.DateObtained ?? string.Empty,
                Url = item.Url ?? string.Empty
            });
        }

        foreach (var item in request.Goals ?? Array.Empty<CareerGoalDto>())
        {
            _db.Add(new CareerGoal
            {
                UserProfileId = profileId,
                Description = item.Description ?? string.Empty,
                Timeframe = item.Timeframe ?? string.Empty
            });
        }

        if (request.LinkedInProfile is not null)
        {
            _db.Add(new LinkedInProfile
            {
                UserProfileId = profileId,
                Url = request.LinkedInProfile.Url ?? string.Empty,
                Headline = request.LinkedInProfile.Headline ?? string.Empty,
                About = request.LinkedInProfile.About ?? string.Empty,
                ExperienceText = request.LinkedInProfile.ExperienceText ?? string.Empty,
                SkillsText = request.LinkedInProfile.SkillsText ?? string.Empty
            });
        }
    }

    private async Task<ProfileDto> BuildDto(UserProfile profile)
    {
        var education = await _db.Set<Education>()
            .Where(e => e.UserProfileId == profile.Id)
            .OrderBy(e => e.StartDate)
            .Select(e => new EducationDto(e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate, e.Description))
            .ToListAsync();

        var experiences = await _db.Set<Experience>()
            .Where(e => e.UserProfileId == profile.Id)
            .OrderBy(e => e.StartDate)
            .Select(e => new ExperienceDto(e.Company, e.Title, e.Location, e.StartDate, e.EndDate, e.IsCurrent,
                e.Description, e.Responsibilities, e.Achievements))
            .ToListAsync();

        var projects = await _db.Set<Project>()
            .Where(p => p.UserProfileId == profile.Id)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new ProjectDto(p.Name, p.Description, p.Technologies, p.Role, p.Url, p.StartDate, p.EndDate, p.Highlights))
            .ToListAsync();

        var skills = await _db.Set<Skill>()
            .Where(s => s.UserProfileId == profile.Id)
            .Select(s => new SkillDto(s.Name, s.Category, s.Proficiency))
            .ToListAsync();

        var certifications = await _db.Set<Certification>()
            .Where(c => c.UserProfileId == profile.Id)
            .Select(c => new CertificationDto(c.Name, c.Issuer, c.DateObtained, c.Url))
            .ToListAsync();

        var goals = await _db.Set<CareerGoal>()
            .Where(g => g.UserProfileId == profile.Id)
            .Select(g => new CareerGoalDto(g.Description, g.Timeframe))
            .ToListAsync();

        var linkedIn = await _db.Set<LinkedInProfile>()
            .Where(l => l.UserProfileId == profile.Id)
            .Select(l => new LinkedInDto(l.Url, l.Headline, l.About, l.ExperienceText, l.SkillsText))
            .FirstOrDefaultAsync();

        return new ProfileDto(
            profile.UserId, profile.FullName, profile.Email, profile.Headline, profile.Phone, profile.Location,
            profile.CareerLevel, profile.YearsOfExperience, profile.PreferredWorkType, profile.PreferredLocation,
            profile.TargetRole, profile.TargetIndustries, profile.ProfessionalSummary, profile.CareerGoals,
            profile.GitHubUrl, profile.LinkedInUrl, profile.PortfolioUrl,
            education, experiences, projects, skills, certifications, goals, linkedIn);
    }
}