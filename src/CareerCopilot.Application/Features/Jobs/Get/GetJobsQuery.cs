using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Models;
using CareerCopilot.Application.Features.Jobs.Dtos;
using CareerCopilot.Application.Features.Jobs.Shared;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Jobs.Get;

public sealed record GetJobsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<JobDto>>;

public sealed class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, PagedResult<JobDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetJobsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<JobDto>> Handle(GetJobsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, PageRequest.MaxPageSize);

        var query = _db.Set<Job>().Where(j => j.UserId == userId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var jobIds = items.Select(j => j.Id).ToList();

        var latestMatches = await _db.Set<JobMatch>()
            .Where(m => m.UserId == userId && jobIds.Contains(m.JobId))
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new { m.JobId, m.OverallScore })
            .ToListAsync(cancellationToken);

        var matchByJob = latestMatches
            .GroupBy(m => m.JobId)
            .ToDictionary(g => g.Key, g => (int?)g.First().OverallScore);

        var result = items.Select(j => new JobDto(
            j.Id, j.Title, j.CompanyName, j.Location, j.EmploymentType,
            j.SourceUrl, j.IsAnalyzed, j.AnalyzedAt, j.CreatedAt,
            matchByJob.TryGetValue(j.Id, out var score) ? score : null)).ToList();

        return new PagedResult<JobDto>(result, total, page, pageSize);
    }
}

public sealed record GetJobDetailsQuery(Guid Id) : IRequest<JobDetailDto>;

public sealed class GetJobDetailsQueryHandler : IRequestHandler<GetJobDetailsQuery, JobDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetJobDetailsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<JobDetailDto> Handle(GetJobDetailsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var job = await _db.Set<Job>()
            .Where(j => j.Id == request.Id && j.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Job not found.");

        return await JobMapper.MapDetailAsync(_db, job, userId, cancellationToken);
    }
}