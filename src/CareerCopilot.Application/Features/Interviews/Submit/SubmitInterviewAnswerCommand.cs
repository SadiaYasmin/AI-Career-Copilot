using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Services;
using CareerCopilot.Application.Features.Interviews.Dtos;
using CareerCopilot.Application.Features.Interviews.Shared;
using CareerCopilot.Application.Scoring;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Interviews.Submit
{

public sealed class SubmitInterviewAnswerCommandHandler : IRequestHandler<SubmitInterviewAnswerCommand, SubmitInterviewAnswerDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly JobSnapshotBuilder _jobSnapshot;
    private readonly ProfileSnapshotBuilder _personSnapshot;
    private readonly ICareerAiService _ai;
    private readonly InterviewScoringService _interviewScoring;

    public SubmitInterviewAnswerCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        JobSnapshotBuilder jobSnapshot,
        ProfileSnapshotBuilder personSnapshot,
        ICareerAiService ai,
        InterviewScoringService interviewScoring)
    {
        _db = db;
        _currentUser = currentUser;
        _jobSnapshot = jobSnapshot;
        _personSnapshot = personSnapshot;
        _ai = ai;
        _interviewScoring = interviewScoring;
    }

    public async Task<SubmitInterviewAnswerDto> Handle(SubmitInterviewAnswerCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var question = await _db.Set<InterviewQuestion>()
            .Where(q => q.Id == request.QuestionId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Interview question not found.");

        var session = await _db.Set<InterviewSession>()
            .Where(i => i.Id == question.InterviewSessionId && i.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Interview session not found.");

        if (session.IsCompleted)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["session"] = new[] { "This interview session is already completed." }
            });
        }

        var questionType = question.QuestionType.ToString();

        var job = await _jobSnapshot.BuildAsync(session.JobId, userId, cancellationToken)
            ?? new AiJobSnapshot(session.JobId, "Role", string.Empty, string.Empty, string.Empty, Array.Empty<AiJobRequirement>());

        var person = await _personSnapshot.BuildPersonAsync(userId, cancellationToken)
            ?? new AiPersonSnapshot(string.Empty, string.Empty, string.Empty,
                Array.Empty<AiSkill>(), Array.Empty<AiExperience>(), Array.Empty<AiProject>(),
                Array.Empty<AiEducation>(), Array.Empty<string>(), 0, string.Empty);

        var evaluationContext = new AnswerEvaluationContext(
            question.Question,
            questionType,
            request.Answer,
            job,
            person,
            person.Projects.FirstOrDefault());

        var result = await _ai.EvaluateAnswerAsync(evaluationContext, cancellationToken);

        var existingAnswer = await _db.Set<InterviewAnswer>()
            .Where(a => a.InterviewQuestionId == question.Id)
            .FirstOrDefaultAsync(cancellationToken);

        InterviewAnswer answer;
        if (existingAnswer is null)
        {
            answer = new InterviewAnswer
            {
                InterviewQuestionId = question.Id,
                Answer = request.Answer
            };
            _db.Add(answer);
        }
        else
        {
            answer = existingAnswer;
            answer.Answer = request.Answer;
            _db.Update(answer);
        }

        answer.Score = result.Score;
        answer.Feedback = result.Feedback;
        answer.ImprovementSuggestion = result.ImprovementSuggestion;

        var evaluation = new InterviewEvaluation
        {
            InterviewAnswerId = answer.Id,
            Score = result.Score,
            RelevanceScore = result.RelevanceScore,
            ClarityScore = result.ClarityScore,
            TechnicalScore = result.TechnicalScore,
            StructureScore = result.StructureScore,
            SpecificityScore = result.SpecificityScore,
            ConcisenessScore = result.ConcisenessScore,
            Feedback = result.Feedback,
            ImprovementSuggestion = result.ImprovementSuggestion
        };
        _db.Add(evaluation);

        var allQuestions = await _db.Set<InterviewQuestion>()
            .Where(q => q.InterviewSessionId == session.Id)
            .ToListAsync(cancellationToken);

        var answeredCount = await _db.Set<InterviewAnswer>()
            .Where(a => allQuestions.Select(q => q.Id).Contains(a.InterviewQuestionId))
            .Select(a => a.InterviewQuestionId)
            .Distinct()
            .CountAsync(cancellationToken);

        var sessionCompleted = answeredCount >= allQuestions.Count;
        int? sessionScore = null;
        string? sessionSummary = null;

        if (sessionCompleted)
        {
            var evaluations = await _db.Set<InterviewEvaluation>()
                .Where(e => allQuestions.Select(q => q.Id).Contains(e.InterviewAnswer!.InterviewQuestionId))
                .ToListAsync(cancellationToken);

            var scores = evaluations.Select(e => new AnswerScore(
                e.RelevanceScore, e.ClarityScore, e.TechnicalScore,
                e.StructureScore, e.SpecificityScore, e.ConcisenessScore)).ToList();

            var report = _interviewScoring.BuildReport(scores);
            sessionScore = report.OverallScore;
            sessionSummary = await BuildSummaryAsync(report, job.Title, cancellationToken);

            session.Complete(sessionScore.Value, sessionSummary, DateTime.UtcNow);
            _db.Update(session);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new SubmitInterviewAnswerDto(
            question.Id,
            result.Score,
            result.RelevanceScore,
            result.ClarityScore,
            result.TechnicalScore,
            result.StructureScore,
            result.SpecificityScore,
            result.ConcisenessScore,
            result.Feedback,
            result.ImprovementSuggestion,
            result.FollowUpQuestion,
            sessionCompleted,
            sessionScore,
            sessionSummary);
    }

    private async Task<string> BuildSummaryAsync(InterviewReport report, string role, CancellationToken ct)
    {
        try
        {
            var aiSummary = await _ai.GenerateInterviewCompletionSummaryAsync(
                string.Join("\n", report.CombinedFeedback), role, ct);

            if (!string.IsNullOrWhiteSpace(aiSummary))
            {
                return aiSummary;
            }
        }
        catch
        {
            // Fall back to deterministic summary.
        }

        var parts = new List<string> { $"Interview completed. Overall score: {report.OverallScore}/100." };
        if (report.StrongAreas.Count > 0)
        {
            parts.Add("Strong areas: " + string.Join(", ", report.StrongAreas) + ".");
        }
        if (report.Improvements.Count > 0)
        {
            parts.Add("Areas to improve: " + string.Join(", ", report.Improvements) + ".");
        }

        return string.Join(" ", parts);
    }
}

}

namespace CareerCopilot.Application.Features.Interviews.Complete
{
    public sealed record CompleteInterviewCommand(Guid Id) : IRequest<Dtos.InterviewSessionDetailDto>;

    public sealed class CompleteInterviewCommandHandler : IRequestHandler<CompleteInterviewCommand, Dtos.InterviewSessionDetailDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly ICareerAiService _ai;
        private readonly InterviewScoringService _interviewScoring;

        public CompleteInterviewCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            ICareerAiService ai,
            InterviewScoringService interviewScoring)
        {
            _db = db;
            _currentUser = currentUser;
            _ai = ai;
            _interviewScoring = interviewScoring;
        }

        public async Task<Dtos.InterviewSessionDetailDto> Handle(CompleteInterviewCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var session = await _db.Set<InterviewSession>()
                .Where(i => i.Id == request.Id && i.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Interview session not found.");

            if (session.IsCompleted)
            {
                return await InterviewMapper.ToDetailDtoAsync(_db, session, userId, cancellationToken);
            }

            var questionIds = await _db.Set<InterviewQuestion>()
                .Where(q => q.InterviewSessionId == session.Id)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            var evaluations = await _db.Set<InterviewEvaluation>()
                .Where(e => questionIds.Contains(e.InterviewAnswer!.InterviewQuestionId))
                .ToListAsync(cancellationToken);

            string summary;
            if (evaluations.Count == 0)
            {
                summary = "Interview ended before any answers were provided.";
                session.Complete(0, summary, DateTime.UtcNow);
            }
            else
            {
                var scores = evaluations.Select(e => new AnswerScore(
                    e.RelevanceScore, e.ClarityScore, e.TechnicalScore,
                    e.StructureScore, e.SpecificityScore, e.ConcisenessScore)).ToList();

                var report = _interviewScoring.BuildReport(scores);
                summary = $"Interview completed early. Overall score: {report.OverallScore}/100. "
                    + string.Join(" ", report.CombinedFeedback);
                session.Complete(report.OverallScore, summary, DateTime.UtcNow);
            }

            _db.Update(session);
            await _db.SaveChangesAsync(cancellationToken);

            return await InterviewMapper.ToDetailDtoAsync(_db, session, userId, cancellationToken);
        }
    }
}