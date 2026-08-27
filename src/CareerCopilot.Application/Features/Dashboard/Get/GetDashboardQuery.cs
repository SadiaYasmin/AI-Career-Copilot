using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Domain.Entities;
using CareerCopilot.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = CareerCopilot.Domain.Entities.Application;

namespace CareerCopilot.Application.Features.Dashboard.Get;

public sealed record DashboardStatusCountDto(ApplicationStatus Status, int Count);

public sealed record DashboardDto(
    int JobCount,
    int ActiveApplicationCount,
    int InterviewCount,
    int ResumeCount,
    int SkillGapCount,
    int? LatestMatchScore,
    Guid? LastJobMatchId,
    int? RecruiterReadinessScore,
    IReadOnlyList<DashboardStatusCountDto> ApplicationStatuses,
    IReadOnlyList<string> TopSkillGaps,
    IReadOnlyList<DashboardTaskDto> UpcomingTasks,
    IReadOnlyList<DashboardApplicationDto> RecentApplications);

public sealed record DashboardTaskDto(string Title, string Skill, string Priority, string Status, DateTime? DueDate);

public sealed record DashboardApplicationDto(
    Guid Id,
    string JobTitle,
    string CompanyName,
    string Status,
    int? MatchScore);

public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var jobCount = await _db.Set<Job>()
            .CountAsync(j => j.UserId == userId, cancellationToken);

        var activeStatuses = new[]
        {
            ApplicationStatus.Saved, ApplicationStatus.Applied, ApplicationStatus.Screening,
            ApplicationStatus.Interview, ApplicationStatus.TechnicalRound, ApplicationStatus.FinalRound
        };

        var activeApplications = await _db.Set<ApplicationEntity>()
            .CountAsync(a => a.UserId == userId && activeStatuses.Contains(a.Status), cancellationToken);

        var interviewCount = await _db.Set<InterviewSession>()
            .CountAsync(i => i.UserId == userId && i.CompletedAt != null, cancellationToken);

        var resumeCount = await _db.Set<Resume>()
            .CountAsync(r => r.UserId == userId, cancellationToken);

        var skillGaps = await _db.Set<SkillGap>()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Priority)
            .ToListAsync(cancellationToken);

        var latestMatch = await _db.Set<JobMatch>()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var readiness = await _db.Set<RecruiterReadinessScore>()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CalculatedAt)
            .Select(r => (int?)r.OverallScore)
            .FirstOrDefaultAsync(cancellationToken);

        var statusCounts = await _db.Set<ApplicationEntity>()
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var taskRows = await _db.Set<RoadmapTask>()
            .Where(t => t.CareerRoadmap!.UserId == userId && t.Status != RoadmapTaskStatus.Completed)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Title)
            .Take(5)
            .ToListAsync(cancellationToken);

        var tasks = taskRows.Select(t => new DashboardTaskDto(
            t.Title, t.Skill, t.Priority.ToString(), t.Status.ToString(), t.DueDate)).ToList();

        var recentApplications = await _db.Set<ApplicationEntity>()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.UpdatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            jobCount,
            activeApplications,
            interviewCount,
            resumeCount,
            skillGaps.Count,
            latestMatch?.OverallScore,
            latestMatch?.JobId,
            readiness,
            statusCounts.OrderBy(s => s.Status).Select(s => new DashboardStatusCountDto(s.Status, s.Count)).ToList(),
            skillGaps.Where(s => s.Priority is SkillPriority.Critical or SkillPriority.High).Take(5).Select(s => s.SkillName).ToList(),
            tasks,
            recentApplications.Select(a => new DashboardApplicationDto(
                a.Id, a.JobTitle, a.CompanyName, a.Status.ToString(), a.MatchScore)).ToList());
    }
}