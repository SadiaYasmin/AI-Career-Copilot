using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Models;
using CareerCopilot.Application.Features.Resumes.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Resumes.Get;

public sealed record GetResumesQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<ResumeDto>>;

public sealed class GetResumesQueryHandler : IRequestHandler<GetResumesQuery, PagedResult<ResumeDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetResumesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ResumeDto>> Handle(GetResumesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, PageRequest.MaxPageSize);

        var query = _db.Set<Resume>().Where(r => r.UserId == userId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.IsDefault)
            .ThenByDescending(r => r.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ResumeDto(r.Id, r.Name, r.OriginalFileName, r.FileType,
                r.IsDefault, r.UploadedAt, r.ParseFailed, r.ResumeScore, r.AnalyzedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ResumeDto>(items, total, page, pageSize);
    }
}

public sealed record GetResumeQuery(Guid Id) : IRequest<ResumeDto>;

public sealed class GetResumeQueryHandler : IRequestHandler<GetResumeQuery, ResumeDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetResumeQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ResumeDto> Handle(GetResumeQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var resume = await _db.Set<Resume>()
            .Where(r => r.Id == request.Id && r.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Resume not found.");

        return new ResumeDto(resume.Id, resume.Name, resume.OriginalFileName, resume.FileType,
            resume.IsDefault, resume.UploadedAt, resume.ParseFailed, resume.ResumeScore, resume.AnalyzedAt);
    }
}