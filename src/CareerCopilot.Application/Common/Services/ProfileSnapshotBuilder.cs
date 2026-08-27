using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Common.Services;

/// <summary>
/// Builds tenant-scoped career snapshots for AI features from the authenticated user's data.
/// </summary>
public sealed class ProfileSnapshotBuilder
{
    private readonly IApplicationDbContext _db;

    public ProfileSnapshotBuilder(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken ct)
        => _db.Set<UserProfile>()
            .Where(p => p.UserId == userId)
            .FirstOrDefaultAsync(ct);

    public async Task<AiPersonSnapshot?> BuildPersonAsync(Guid userId, CancellationToken ct)
    {
        var profile = await GetProfileAsync(userId, ct);
        if (profile is null)
        {
            return null;
        }

        var skills = await _db.Set<Skill>()
            .Where(s => s.UserProfileId == profile.Id)
            .Select(s => new AiSkill(s.Name, s.Proficiency))
            .ToListAsync(ct);

        var experiences = await _db.Set<Experience>()
            .Where(e => e.UserProfileId == profile.Id)
            .OrderBy(e => e.StartDate)
            .ToListAsync(ct);

        var projects = await _db.Set<Project>()
            .Where(p => p.UserProfileId == profile.Id)
            .ToListAsync(ct);

        var education = await _db.Set<Education>()
            .Where(e => e.UserProfileId == profile.Id)
            .ToListAsync(ct);

        var certifications = await _db.Set<Certification>()
            .Where(c => c.UserProfileId == profile.Id)
            .Select(c => c.Name)
            .ToListAsync(ct);

        return new AiPersonSnapshot(
            profile.TargetRole,
            profile.Headline,
            profile.ProfessionalSummary,
            skills,
            experiences.Select(e => new AiExperience(e.Company, e.Title,
                BuildExperienceText(e))).ToList(),
            projects.Select(p => new AiProject(p.Name, p.Description, p.Technologies)).ToList(),
            education.Select(e => new AiEducation(e.Degree, e.Institution)).ToList(),
            certifications,
            profile.YearsOfExperience,
            profile.CareerGoals);
    }

    public async Task<AiResumeSnapshot?> BuildResumeAsync(Guid resumeId, Guid userId, CancellationToken ct)
    {
        var resume = await _db.Set<Resume>()
            .Where(r => r.Id == resumeId && r.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (resume is null)
        {
            return null;
        }

        var lines = (resume.ParsedText ?? string.Empty)
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        return new AiResumeSnapshot(
            resume.Id,
            resume.Name,
            resume.OriginalFileName,
            resume.ParsedText ?? string.Empty,
            lines);
    }

    private static string BuildExperienceText(Experience e)
    {
        var parts = new List<string> { e.Title, e.Company };
        if (!string.IsNullOrWhiteSpace(e.Description))
        {
            parts.Add(e.Description);
        }
        if (!string.IsNullOrWhiteSpace(e.Responsibilities))
        {
            parts.Add(e.Responsibilities);
        }
        if (!string.IsNullOrWhiteSpace(e.Achievements))
        {
            parts.Add(e.Achievements);
        }

        return string.Join(". ", parts);
    }
}