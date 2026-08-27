using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Common.Services;

public sealed class JobSnapshotBuilder
{
    private readonly IApplicationDbContext _db;

    public JobSnapshotBuilder(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AiJobSnapshot?> BuildAsync(Guid jobId, Guid userId, CancellationToken ct)
    {
        var job = await _db.Set<Job>()
            .Where(j => j.Id == jobId && j.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (job is null)
        {
            return null;
        }

        var requirements = await _db.Set<JobRequirement>()
            .Where(r => r.JobId == jobId)
            .OrderByDescending(r => r.RequirementType)
            .ToListAsync(ct);

        return new AiJobSnapshot(
            job.Id,
            job.Title,
            job.CompanyName,
            job.Location,
            job.Description,
            requirements.Select(r => new AiJobRequirement(
                r.Name,
                r.RequirementType.ToString(),
                r.Importance,
                r.SourceText)).ToList());
    }
}