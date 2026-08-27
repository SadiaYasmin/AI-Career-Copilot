using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Jobs.Dtos;
using CareerCopilot.Application.Features.Jobs.Shared;
using CareerCopilot.Domain.Entities;
using CareerCopilot.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Jobs.Analyze;

public sealed class AnalyzeJobCommandHandler : IRequestHandler<AnalyzeJobCommand, JobDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICareerAiService _ai;

    public AnalyzeJobCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        ICareerAiService ai)
    {
        _db = db;
        _currentUser = currentUser;
        _ai = ai;
    }

    public async Task<JobDetailDto> Handle(AnalyzeJobCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var job = await _db.Set<Job>()
            .Where(j => j.Id == request.Id && j.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Job not found.");

        if (string.IsNullOrWhiteSpace(job.Description))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["description"] = new[] { "Paste a job description before analyzing this job." }
            });
        }

        var snapshot = new AiJobSnapshot(
            job.Id, job.Title, job.CompanyName, job.Location, job.Description,
            Array.Empty<AiJobRequirement>());

        var result = await _ai.AnalyzeJobAsync(new JobAnalysisContext(snapshot), cancellationToken);

        if (!string.IsNullOrWhiteSpace(result.Title))
        {
            job.Title = result.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result.Company))
        {
            job.CompanyName = result.Company.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result.Location))
        {
            job.Location = result.Location.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result.EmploymentType)
            && string.Equals(result.EmploymentType, "Unknown", StringComparison.OrdinalIgnoreCase) is false)
        {
            job.EmploymentType = result.EmploymentType.Trim();
        }

        var oldRequirements = _db.Set<JobRequirement>().Where(r => r.JobId == job.Id);
        _db.RemoveRange(oldRequirements);

        foreach (var req in result.Requirements)
        {
            _db.Add(new JobRequirement
            {
                JobId = job.Id,
                RequirementType = MapRequirementType(req.RequirementType),
                Name = req.Name,
                Description = req.SourceText ?? string.Empty,
                Importance = req.Importance ?? string.Empty,
                SourceText = req.SourceText ?? string.Empty
            });
        }

        job.IsAnalyzed = true;
        job.AnalyzedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        _db.Update(job);

        await _db.SaveChangesAsync(cancellationToken);

        return await JobMapper.MapDetailAsync(_db, job, userId, cancellationToken);
    }

    private static RequirementType MapRequirementType(string type)
        => type?.ToLowerInvariant() switch
        {
            "required" => RequirementType.Required,
            "preferred" => RequirementType.Preferred,
            _ => RequirementType.Inferred
        };
}