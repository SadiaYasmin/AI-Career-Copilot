using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Jobs.Dtos;
using CareerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = CareerCopilot.Domain.Entities.Application;

namespace CareerCopilot.Application.Features.Jobs.Shared;

public static class JobMapper
{
    public static async Task<JobDetailDto> MapDetailAsync(
        IApplicationDbContext db,
        Job job,
        Guid userId,
        CancellationToken ct)
    {
        var requirements = await db.Set<JobRequirement>()
            .Where(r => r.JobId == job.Id)
            .OrderByDescending(r => r.RequirementType)
            .Select(r => new JobRequirementDto(
                r.Id, r.RequirementType, r.Name, r.Description, r.Importance, r.SourceText))
            .ToListAsync(ct);

        var latestMatch = await db.Set<JobMatch>()
            .Where(m => m.JobId == job.Id && m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => (int?)m.OverallScore)
            .FirstOrDefaultAsync(ct);

        var applicationsCount = await db.Set<ApplicationEntity>()
            .Where(a => a.JobId == job.Id && a.UserId == userId)
            .CountAsync(ct);

        return new JobDetailDto(
            job.Id, job.Title, job.CompanyName, job.Location, job.EmploymentType,
            job.Description, job.SourceUrl, job.IsAnalyzed, job.AnalyzedAt, job.CreatedAt,
            latestMatch, applicationsCount, requirements);
    }
}