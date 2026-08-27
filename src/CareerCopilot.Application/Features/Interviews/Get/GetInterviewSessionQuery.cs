using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Interviews.Dtos;
using CareerCopilot.Application.Features.Interviews.Shared;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Interviews.Get;

public sealed record GetInterviewSessionQuery(Guid Id) : IRequest<InterviewSessionDetailDto>;

public sealed class GetInterviewSessionQueryHandler : IRequestHandler<GetInterviewSessionQuery, InterviewSessionDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetInterviewSessionQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<InterviewSessionDetailDto> Handle(GetInterviewSessionQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var session = await _db.Set<InterviewSession>()
            .Where(i => i.Id == request.Id && i.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Interview session not found.");

        return await InterviewMapper.ToDetailDtoAsync(_db, session, userId, cancellationToken);
    }
}

public sealed record GetInterviewSessionsQuery(Guid JobId) : IRequest<IReadOnlyList<InterviewSessionDto>>;

public sealed class GetInterviewSessionsQueryHandler : IRequestHandler<GetInterviewSessionsQuery, IReadOnlyList<InterviewSessionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetInterviewSessionsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<InterviewSessionDto>> Handle(GetInterviewSessionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var ownsJob = await _db.Set<Job>()
            .AnyAsync(j => j.Id == request.JobId && j.UserId == userId, cancellationToken);
        if (!ownsJob)
        {
            throw new NotFoundException("Job not found.");
        }

        var sessions = await _db.Set<InterviewSession>()
            .Where(i => i.JobId == request.JobId && i.UserId == userId)
            .OrderByDescending(i => i.StartedAt)
            .ToListAsync(cancellationToken);

        var dtos = new List<InterviewSessionDto>();
        foreach (var session in sessions)
        {
            var detail = await InterviewMapper.ToDetailDtoAsync(_db, session, userId, cancellationToken);
            dtos.Add(detail.Session);
        }

        return dtos;
    }
}