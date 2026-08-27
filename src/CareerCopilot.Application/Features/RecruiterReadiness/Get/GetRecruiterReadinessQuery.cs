using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Scoring;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.RecruiterReadiness.Get;

public sealed record RecruiterReadinessDto(
    int OverallScore,
    int ResumeScore,
    int SkillsScore,
    int ProjectsScore,
    int ProfileScore,
    int InterviewScore,
    IReadOnlyList<string> ImprovementActions,
    DateTime CalculatedAt);

public sealed record GetRecruiterReadinessQuery(bool Recalculate = false) : IRequest<RecruiterReadinessDto>;

public sealed class GetRecruiterReadinessQueryHandler : IRequestHandler<GetRecruiterReadinessQuery, RecruiterReadinessDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly RecruiterReadinessService _readiness;

    public GetRecruiterReadinessQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        RecruiterReadinessService readiness)
    {
        _db = db;
        _currentUser = currentUser;
        _readiness = readiness;
    }

    public async Task<RecruiterReadinessDto> Handle(GetRecruiterReadinessQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        if (!request.Recalculate)
        {
            var stored = await _db.Set<RecruiterReadinessScore>()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CalculatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (stored is not null && stored.ReportJson != "{}")
            {
                return new RecruiterReadinessDto(
                    stored.OverallScore, stored.ResumeScore ?? 0, stored.SkillsScore ?? 0,
                    stored.ProjectsScore ?? 0, stored.ProfileScore ?? 0, stored.InterviewScore ?? 0,
                    ReadinessReportParser.ParseActions(stored.ReportJson), stored.CalculatedAt);
            }
        }

        var report = await CalculateAsync(userId, cancellationToken);

        var entity = new RecruiterReadinessScore
        {
            UserId = userId,
            OverallScore = report.Overall,
            ResumeScore = report.ResumeScore,
            SkillsScore = report.SkillsScore,
            ProjectsScore = report.ProjectsScore,
            ProfileScore = report.ProfileScore,
            InterviewScore = report.InterviewScore,
            ReportJson = ReadinessReportParser.Serialize(report),
            CalculatedAt = DateTime.UtcNow
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new RecruiterReadinessDto(
            report.Overall, report.ResumeScore, report.SkillsScore, report.ProjectsScore,
            report.ProfileScore, report.InterviewScore, report.ImprovementActions, entity.CalculatedAt);
    }

    private async Task<ReadinessReport> CalculateAsync(Guid userId, CancellationToken ct)
    {
        var profile = await _db.Set<UserProfile>()
            .Where(p => p.UserId == userId)
            .FirstOrDefaultAsync(ct);

        int? resumeScore = null;
        var defaultResume = await _db.Set<Resume>()
            .Where(r => r.UserId == userId && r.IsDefault)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (defaultResume?.ResumeScore is not null)
        {
            resumeScore = defaultResume.ResumeScore;
        }

        var latestMatch = await _db.Set<JobMatch>()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => (int?)m.OverallScore)
            .FirstOrDefaultAsync(ct);

        var skillCount = profile is null ? 0 : await _db.Set<Skill>()
            .Where(s => s.UserProfileId == profile.Id).CountAsync(ct);

        var projectCount = profile is null ? 0 : await _db.Set<Project>()
            .Where(p => p.UserProfileId == profile.Id).CountAsync(ct);

        var interviewStats = await _db.Set<InterviewSession>()
            .Where(i => i.UserId == userId && i.CompletedAt != null && i.OverallScore != null)
            .Select(i => i.OverallScore!.Value)
            .ToListAsync(ct);

        var educCount = profile is null ? 0 : await _db.Set<Education>()
            .Where(e => e.UserProfileId == profile.Id).CountAsync(ct);

        var expCount = profile is null ? 0 : await _db.Set<Experience>()
            .Where(e => e.UserProfileId == profile.Id).CountAsync(ct);

        var certCount = profile is null ? 0 : await _db.Set<Certification>()
            .Where(c => c.UserProfileId == profile.Id).CountAsync(ct);

        var completeness = ComputeCompleteness(profile, skillCount, expCount, educCount, certCount);

        return _readiness.Calculate(new ReadinessInput(
            resumeScore,
            latestMatch,
            completeness,
            skillCount,
            projectCount,
            interviewStats.Count,
            interviewStats.Count > 0 ? interviewStats.Average() : null));
    }

    private static double ComputeCompleteness(
        UserProfile? profile,
        int skillCount,
        int expCount,
        int educCount,
        int certCount)
    {
        var criteria = new[]
        {
            profile is not null && !string.IsNullOrWhiteSpace(profile.Headline),
            profile is not null && !string.IsNullOrWhiteSpace(profile.ProfessionalSummary),
            profile is not null && !string.IsNullOrWhiteSpace(profile.YearsOfExperience.ToString()) && (profile?.YearsOfExperience ?? 0) > 0,
            skillCount > 0,
            expCount > 0,
            educCount > 0,
            certCount > 0,
            profile is not null && !string.IsNullOrWhiteSpace(profile.CareerGoals)
        };

        var passed = criteria.Count(c => c);
        return passed / (double)criteria.Length;
    }
}