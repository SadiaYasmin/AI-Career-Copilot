using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.JobMatching.Dtos;
using CareerCopilot.Application.Scoring;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CareerCopilot.Application.Features.JobMatching.Get;

public sealed record GetJobMatchQuery(Guid JobId) : IRequest<JobMatchDto>;

public sealed class GetJobMatchQueryHandler : IRequestHandler<GetJobMatchQuery, JobMatchDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetJobMatchQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<JobMatchDto> Handle(GetJobMatchQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var match = await _db.Set<JobMatch>()
            .Where(m => m.JobId == request.JobId && m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No match calculated for this job yet.");

        var strong = JobMatchPayloadParser.ParseStringList(match.StrongMatchesJson);
        var partial = JobMatchPayloadParser.ParseStringList(match.PartialMatchesJson);
        var missing = JobMatchPayloadParser.ParseStringList(match.MissingRequirementsJson);
        var evidenceList = JobMatchPayloadParser.ParseEvidence(match.EvidenceJson);
        var recommendations = JobMatchPayloadParser.ParseStringList(match.RecommendationsJson);

        if (evidenceList.Count > 0)
        {
            strong = evidenceList.Where(e => e.Status == "Strong").Select(e => e.Name).ToList();
            partial = evidenceList.Where(e => e.Status == "Partial").Select(e => e.Name).ToList();
            missing = evidenceList.Where(e => e.Status == "Missing").Select(e => e.Name).ToList();
        }

        return new JobMatchDto(
            match.Id,
            match.JobId,
            match.ResumeId,
            match.OverallScore,
            match.SkillsScore,
            match.ExperienceScore,
            match.EducationScore,
            match.ProjectScore,
            match.KeywordScore,
            match.AlignmentScore,
            strong,
            partial,
            missing,
            evidenceList.Select(e => new MatchEvidenceDto(e.Name, e.Status, e.Source, e.Detail)).ToList(),
            recommendations,
            match.Explanation ?? string.Empty,
            match.CreatedAt);
    }
}

internal static class JobMatchPayloadParser
{
    public static IReadOnlyList<MatchItemFinding> ParseEvidence(string json)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<EvidencePayload>>(json)
                ?? new List<EvidencePayload>();
            return items
                .Select(i => new MatchItemFinding(i.Name ?? "", i.Status ?? "", i.Source ?? "", i.Detail ?? ""))
                .ToList();
        }
        catch
        {
            return new List<MatchItemFinding>();
        }
    }

    public static IReadOnlyList<string> ParseStringList(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private sealed class EvidencePayload
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Source { get; set; }
        public string? Detail { get; set; }
    }
}