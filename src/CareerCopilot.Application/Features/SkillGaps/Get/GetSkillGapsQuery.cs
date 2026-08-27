using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Domain.Entities;
using CareerCopilot.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.SkillGaps.Get;

public sealed record SkillGapDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string SkillName,
    string GapType,
    string Priority,
    string CurrentLevel,
    string RequiredLevel,
    string Recommendation,
    string LearningPath);

public sealed record GetSkillGapsQuery(Guid? JobId = null) : IRequest<IReadOnlyList<SkillGapDto>>;

public sealed class GetSkillGapsQueryHandler : IRequestHandler<GetSkillGapsQuery, IReadOnlyList<SkillGapDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetSkillGapsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SkillGapDto>> Handle(GetSkillGapsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        if (request.JobId is not null)
        {
            var ownsJob = await _db.Set<Job>()
                .AnyAsync(j => j.Id == request.JobId && j.UserId == userId, cancellationToken);
            if (!ownsJob)
            {
                throw new NotFoundException("Job not found.");
            }
        }

        var query = _db.Set<SkillGap>().Where(s => s.UserId == userId);

        if (request.JobId is not null)
        {
            query = query.Where(s => s.JobId == request.JobId);
        }

        var items = await query
            .OrderByDescending(s => s.Priority)
            .ThenBy(s => s.SkillName)
            .ToListAsync(cancellationToken);

        var jobIds = items.Select(s => s.JobId).Distinct().ToList();
        var jobs = await _db.Set<Job>()
            .Where(j => jobIds.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, j => j.Title, cancellationToken);

        return items.Select(s => new SkillGapDto(
            s.Id, s.JobId,
            jobs.TryGetValue(s.JobId, out var title) ? title : "Unknown",
            s.SkillName, s.GapType.ToString(), s.Priority.ToString(),
            s.CurrentLevel, s.RequiredLevel, s.Recommendation, s.LearningPath)).ToList();
    }
}