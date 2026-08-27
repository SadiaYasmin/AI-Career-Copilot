using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Applications.Dtos;
using CareerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = CareerCopilot.Domain.Entities.Application;

namespace CareerCopilot.Application.Features.Applications.Shared;

public static class ApplicationMapper
{
    public static async Task<ApplicationDto> ToDtoAsync(
        IApplicationDbContext db,
        ApplicationEntity application,
        Guid userId,
        CancellationToken ct)
    {
        var resumeName = await GetResumeNameAsync(db, application.ResumeId, ct);

        return new ApplicationDto(
            application.Id,
            application.JobId,
            application.JobTitle,
            application.CompanyName,
            application.Status,
            application.Source,
            application.AppliedAt,
            resumeName,
            application.MatchScore,
            application.UpdatedAt);
    }

    public static async Task<ApplicationDetailDto> ToDetailDtoAsync(
        IApplicationDbContext db,
        ApplicationEntity application,
        Guid userId,
        CancellationToken ct)
    {
        var resumeName = await GetResumeNameAsync(db, application.ResumeId, ct);

        var interviewCount = await db.Set<InterviewSession>()
            .Where(i => i.JobId == (application.JobId ?? Guid.Empty) && i.UserId == userId)
            .CountAsync(ct);

        var lastInterview = await db.Set<InterviewSession>()
            .Where(i => i.JobId == (application.JobId ?? Guid.Empty) && i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => (DateTime?)i.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new ApplicationDetailDto(
            application.Id,
            application.JobId,
            application.JobTitle,
            application.CompanyName,
            application.JobUrl,
            application.Location ?? string.Empty,
            application.Status,
            application.Source,
            application.AppliedAt,
            application.FollowUpDate,
            application.Notes ?? string.Empty,
            application.ResumeId,
            resumeName,
            application.CoverLetterId,
            application.MatchScore,
            interviewCount,
            lastInterview);
    }

    private static async Task<string> GetResumeNameAsync(IApplicationDbContext db, Guid? resumeId, CancellationToken ct)
    {
        if (resumeId is null)
        {
            return string.Empty;
        }

        return await db.Set<Resume>()
            .Where(r => r.Id == resumeId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;
    }
}