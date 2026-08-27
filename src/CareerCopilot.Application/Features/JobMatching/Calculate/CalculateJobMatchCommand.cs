using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Services;
using CareerCopilot.Application.Features.JobMatching.Dtos;
using CareerCopilot.Application.Scoring;
using CareerCopilot.Domain.Entities;
using CareerCopilot.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.JobMatching.Calculate;

public sealed record CalculateJobMatchCommand(Guid JobId, Guid? ResumeId = null) : IRequest<JobMatchDto>;

public sealed class CalculateJobMatchCommandHandler : IRequestHandler<CalculateJobMatchCommand, JobMatchDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly JobSnapshotBuilder _jobSnapshot;
    private readonly ProfileSnapshotBuilder _personSnapshot;
    private readonly MatchScoringService _matchScoring;
    private readonly SkillGapService _skillGap;
    private readonly ICareerAiService _ai;

    public CalculateJobMatchCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        JobSnapshotBuilder jobSnapshot,
        ProfileSnapshotBuilder personSnapshot,
        MatchScoringService matchScoring,
        SkillGapService skillGap,
        ICareerAiService ai)
    {
        _db = db;
        _currentUser = currentUser;
        _jobSnapshot = jobSnapshot;
        _personSnapshot = personSnapshot;
        _matchScoring = matchScoring;
        _skillGap = skillGap;
        _ai = ai;
    }

    public async Task<JobMatchDto> Handle(CalculateJobMatchCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        if (request.ResumeId is not null)
        {
            var ownsResume = await _db.Set<Resume>()
                .AnyAsync(r => r.Id == request.ResumeId && r.UserId == userId, cancellationToken);
            if (!ownsResume)
            {
                throw new NotFoundException("Resume not found.");
            }
        }

        var job = await _jobSnapshot.BuildAsync(request.JobId, userId, cancellationToken)
            ?? throw new NotFoundException("Job not found.");

        if (job.Requirements.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["jobId"] = new[] { "Analyze the job description first so Copilot knows what the role requires." }
            });
        }

        var person = await _personSnapshot.BuildPersonAsync(userId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                ["profile"] = new[] { "Complete your career profile before calculating a match." }
            });

        AiResumeSnapshot? resume = null;
        if (request.ResumeId is not null)
        {
            resume = await _personSnapshot.BuildResumeAsync(request.ResumeId.Value, userId, cancellationToken);
        }

        var input = new MatchEngineInput(
            CandidateSkills: person.Skills.Select(s => s.Name).ToList(),
            Experience: person.Experience,
            Projects: person.Projects,
            Education: person.Education,
            YearsOfExperience: person.YearsOfExperience,
            TargetRole: person.TargetRole,
            TargetIndustries: person.CareerGoals,
            Certifications: person.Certifications,
            ProfileSummary: person.ProfessionalSummary,
            Requirements: job.Requirements,
            JobTitle: job.Title,
            JobDescription: job.Description,
            ResumeText: resume?.ParsedText);

        var match = _matchScoring.Calculate(input);

        string explanation = match.Explanation;
        var evidence = match.Evidence;
        var recommendations = new List<string>(match.Recommendations);

        try
        {
            var aiResult = await _ai.ExplainMatchAsync(
                new MatchAiContext(
                    person, resume, job,
                    match.Scores.Overall,
                    match.Scores.Skills,
                    match.Scores.Experience,
                    match.Scores.Education,
                    match.Scores.Projects,
                    match.Scores.Keywords,
                    match.Scores.Alignment,
                    match.StrongMatches,
                    match.PartialMatches,
                    match.MissingRequirements),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(aiResult.Explanation))
            {
                explanation = aiResult.Explanation;
            }

            if (aiResult.Recommendations.Count > 0)
            {
                foreach (var rec in aiResult.Recommendations)
                {
                    if (!recommendations.Contains(rec, StringComparer.OrdinalIgnoreCase))
                    {
                        recommendations.Add(rec);
                    }
                }
            }
        }
        catch
        {
            // Deterministic output stands on its own when AI is unavailable.
        }

        var existingMatches = _db.Set<JobMatch>().Where(m => m.JobId == job.JobId && m.UserId == userId);
        _db.RemoveRange(existingMatches);

        var matchEntity = new JobMatch
        {
            UserId = userId,
            JobId = job.JobId,
            ResumeId = resume?.ResumeId,
            OverallScore = match.Scores.Overall,
            SkillsScore = match.Scores.Skills,
            ExperienceScore = match.Scores.Experience,
            EducationScore = match.Scores.Education,
            ProjectScore = match.Scores.Projects,
            KeywordScore = match.Scores.Keywords,
            AlignmentScore = match.Scores.Alignment,
            StrongMatchesJson = System.Text.Json.JsonSerializer.Serialize(match.StrongMatches),
            PartialMatchesJson = System.Text.Json.JsonSerializer.Serialize(match.PartialMatches),
            MissingRequirementsJson = System.Text.Json.JsonSerializer.Serialize(match.MissingRequirements),
            EvidenceJson = System.Text.Json.JsonSerializer.Serialize(
                evidence.Select(e => new { e.Name, e.Status, e.Source, e.Detail })),
            RecommendationsJson = System.Text.Json.JsonSerializer.Serialize(recommendations),
            Explanation = explanation
        };
        _db.Add(matchEntity);

        var oldGaps = _db.Set<SkillGap>().Where(s => s.JobId == job.JobId && s.UserId == userId);
        _db.RemoveRange(oldGaps);

        foreach (var gap in _skillGap.Calculate(job.Requirements, person.Skills.Select(s => s.Name).ToList()))
        {
            _db.Add(new SkillGap
            {
                UserId = userId,
                JobId = job.JobId,
                SkillName = gap.SkillName,
                GapType = ParseGapType(gap.GapType),
                Priority = ParseSkillPriority(gap.Priority),
                CurrentLevel = gap.CurrentLevel,
                RequiredLevel = gap.RequiredLevel,
                Recommendation = gap.Recommendation,
                LearningPath = gap.LearningPath
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new JobMatchDto(
            matchEntity.Id,
            job.JobId,
            resume?.ResumeId,
            match.Scores.Overall,
            match.Scores.Skills,
            match.Scores.Experience,
            match.Scores.Education,
            match.Scores.Projects,
            match.Scores.Keywords,
            match.Scores.Alignment,
            match.StrongMatches,
            match.PartialMatches,
            match.MissingRequirements,
            evidence.Select(e => new MatchEvidenceDto(e.Name, e.Status, e.Source, e.Detail)).ToList(),
            recommendations,
            explanation,
            match.GeneratedAt);
    }

    private static GapType ParseGapType(string value)
        => value == "Missing" ? GapType.Missing : GapType.NeedsImprovement;

    private static SkillPriority ParseSkillPriority(string value)
        => value switch
        {
            "Critical" => SkillPriority.Critical,
            "High" => SkillPriority.High,
            "Low" => SkillPriority.Low,
            _ => SkillPriority.Medium
        };
}