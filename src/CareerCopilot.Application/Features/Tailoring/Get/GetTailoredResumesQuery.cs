using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Tailoring.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Tailoring.Get;

public sealed record GetTailoredResumesQuery(Guid? ResumeId = null) : IRequest<IReadOnlyList<TailoredResumeDto>>;

public sealed class GetTailoredResumesQueryHandler : IRequestHandler<GetTailoredResumesQuery, IReadOnlyList<TailoredResumeDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTailoredResumesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TailoredResumeDto>> Handle(GetTailoredResumesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var query = _db.Set<TailoredResume>().Where(t => t.UserId == userId);

        if (request.ResumeId is not null)
        {
            query = query.Where(t => t.ResumeId == request.ResumeId);
        }

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var jobIds = items.Select(t => t.JobId).Distinct().ToList();
        var jobs = await _db.Set<Job>()
            .Where(j => jobIds.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, j => new { j.Title, j.CompanyName }, cancellationToken);

        return items.Select(t =>
        {
            jobs.TryGetValue(t.JobId, out var job);
            return new TailoredResumeDto(
                t.Id, t.ResumeId, t.JobId,
                job?.Title ?? "Unknown", job?.CompanyName ?? string.Empty,
                t.Mode.ToString(), t.ChangesSummary, t.CreatedAt);
        }).ToList();
    }
}

public sealed record GetTailoredResumeQuery(Guid Id) : IRequest<TailoredResumeDetailDto>;

public sealed class GetTailoredResumeQueryHandler : IRequestHandler<GetTailoredResumeQuery, TailoredResumeDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTailoredResumeQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TailoredResumeDetailDto> Handle(GetTailoredResumeQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var tailored = await _db.Set<TailoredResume>()
            .Where(t => t.Id == request.Id && t.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Tailored resume not found.");

        var job = await _db.Set<Job>()
            .Where(j => j.Id == tailored.JobId)
            .FirstOrDefaultAsync(cancellationToken);

        return new TailoredResumeDetailDto(
            tailored.Id, tailored.ResumeId, tailored.JobId,
            job?.Title ?? "Unknown", job?.CompanyName ?? string.Empty,
            tailored.Mode.ToString(),
            tailored.Content, tailored.OriginalContent, tailored.Separator,
            tailored.ChangesSummary, tailored.CreatedAt);
    }
}