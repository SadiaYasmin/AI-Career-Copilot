using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Models;
using CareerCopilot.Application.Features.Applications.Dtos;
using CareerCopilot.Application.Features.Applications.Shared;
using CareerCopilot.Domain.Entities;
using CareerCopilot.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = CareerCopilot.Domain.Entities.Application;

namespace CareerCopilot.Application.Features.Applications.Get;

public sealed record GetApplicationsQuery(
    int Page = 1,
    int PageSize = 20,
    ApplicationStatus? Status = null) : IRequest<PagedResult<ApplicationDto>>;

public sealed class GetApplicationsQueryHandler : IRequestHandler<GetApplicationsQuery, PagedResult<ApplicationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetApplicationsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ApplicationDto>> Handle(GetApplicationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, PageRequest.MaxPageSize);

        var query = _db.Set<ApplicationEntity>().Where(a => a.UserId == userId);

        if (request.Status is not null)
        {
            query = query.Where(a => a.Status == request.Status);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var results = new List<ApplicationDto>(items.Count);
        foreach (var item in items)
        {
            results.Add(await ApplicationMapper.ToDtoAsync(_db, item, userId, cancellationToken));
        }

        return new PagedResult<ApplicationDto>(results, total, page, pageSize);
    }
}

public sealed record GetApplicationQuery(Guid Id) : IRequest<ApplicationDetailDto>;

public sealed class GetApplicationQueryHandler : IRequestHandler<GetApplicationQuery, ApplicationDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetApplicationQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApplicationDetailDto> Handle(GetApplicationQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var application = await _db.Set<ApplicationEntity>()
            .Where(a => a.Id == request.Id && a.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Application not found.");

        return await ApplicationMapper.ToDetailDtoAsync(_db, application, userId, cancellationToken);
    }
}