using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Interviews.Dtos;
using CareerCopilot.Domain.Entities;
using CareerCopilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Interviews.Shared;

public static class InterviewMapper
{
    public static async Task<InterviewSessionDetailDto> ToDetailDtoAsync(
        IApplicationDbContext db,
        InterviewSession session,
        Guid userId,
        CancellationToken ct)
    {
        var questions = await db.Set<InterviewQuestion>()
            .Where(q => q.InterviewSessionId == session.Id)
            .OrderBy(q => q.Order)
            .ToListAsync(ct);

        var questionIds = questions.Select(q => q.Id).ToList();

        var answers = await db.Set<InterviewAnswer>()
            .Where(a => questionIds.Contains(a.InterviewQuestionId))
            .ToListAsync(ct);

        var scores = answers
            .GroupBy(a => a.InterviewQuestionId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.Score ?? 0));

        var questionDtos = questions.Select(q => new InterviewQuestionDto(
            q.Id, q.Question, q.QuestionType.ToString(), q.Order,
            scores.TryGetValue(q.Id, out var s) ? s : null,
            scores.ContainsKey(q.Id))).ToList();

        var job = await db.Set<Job>()
            .Where(j => j.Id == session.JobId)
            .FirstOrDefaultAsync(ct);

        var sessionDto = new InterviewSessionDto(
            session.Id,
            session.JobId,
            job?.Title ?? "Unknown",
            job?.CompanyName ?? string.Empty,
            session.Mode.ToString(),
            session.OverallScore,
            session.Summary,
            session.StartedAt,
            session.CompletedAt,
            questions.Count,
            questionDtos.Count(q => q.IsAnswered),
            session.IsCompleted);

        return new InterviewSessionDetailDto(sessionDto, questionDtos);
    }

    public static QuestionType MapQuestionType(string type)
        => type?.ToLowerInvariant() switch
        {
            "technical" => QuestionType.Technical,
            "behavioral" => QuestionType.Behavioral,
            "scenario" or "situational" or "case" => QuestionType.Scenario,
            "resumebased" or "resume" => QuestionType.ResumeBased,
            "companyrole" or "company" or "role" => QuestionType.CompanyRole,
            _ => QuestionType.Behavioral
        };
}