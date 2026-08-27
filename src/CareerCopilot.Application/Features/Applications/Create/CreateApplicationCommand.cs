using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Applications.Dtos;
using CareerCopilot.Application.Features.Applications.Shared;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = CareerCopilot.Domain.Entities.Application;

namespace CareerCopilot.Application.Features.Applications.Create;

public sealed class CreateApplicationCommandHandler : IRequestHandler<CreateApplicationCommand, ApplicationDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateApplicationCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApplicationDto> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
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

        if (request.ResumeId is not null)
        {
            var ownsResume = await _db.Set<Resume>()
                .AnyAsync(r => r.Id == request.ResumeId && r.UserId == userId, cancellationToken);
            if (!ownsResume)
            {
                throw new NotFoundException("Resume not found.");
            }
        }

        int? matchScore = null;
        if (request.JobId is not null)
        {
            matchScore = await _db.Set<JobMatch>()
                .Where(m => m.JobId == request.JobId && m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (int?)m.OverallScore)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var application = new ApplicationEntity
        {
            UserId = userId,
            JobId = request.JobId,
            CompanyName = request.CompanyName?.Trim() ?? string.Empty,
            JobTitle = request.JobTitle?.Trim() ?? string.Empty,
            JobUrl = request.JobUrl ?? string.Empty,
            Location = request.Location,
            JobDescription = request.JobDescription,
            ResumeId = request.ResumeId,
            CoverLetterId = request.CoverLetterId,
            MatchScore = matchScore,
            Status = request.Status,
            Source = request.Source ?? string.Empty,
            AppliedAt = request.AppliedAt ?? DateTime.UtcNow
        };

        _db.Add(application);
        await _db.SaveChangesAsync(cancellationToken);

        return await ApplicationMapper.ToDtoAsync(_db, application, userId, cancellationToken);
    }
}