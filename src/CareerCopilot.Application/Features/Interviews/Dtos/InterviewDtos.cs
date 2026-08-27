using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Application.Features.Interviews.Dtos;

public sealed record InterviewQuestionDto(
    Guid Id,
    string Question,
    string QuestionType,
    int Order,
    int? AnswerScore,
    bool IsAnswered);

public sealed record InterviewSessionDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string Mode,
    int? OverallScore,
    string? Summary,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int QuestionCount,
    int AnsweredCount,
    bool IsCompleted);

public sealed record InterviewSessionDetailDto(
    InterviewSessionDto Session,
    IReadOnlyList<InterviewQuestionDto> Questions);

public sealed record SubmitInterviewAnswerDto(
    Guid QuestionId,
    int Score,
    int RelevanceScore,
    int ClarityScore,
    int TechnicalScore,
    int StructureScore,
    int SpecificityScore,
    int ConcisenessScore,
    string Feedback,
    string ImprovementSuggestion,
    string? FollowUpQuestion,
    bool SessionCompleted,
    int? SessionOverallScore,
    string? SessionSummary);

public sealed record CreateInterviewSessionCommand(
    Guid JobId,
    InterviewMode Mode) : MediatR.IRequest<InterviewSessionDetailDto>;

public sealed record SubmitInterviewAnswerCommand(Guid QuestionId, string Answer) : MediatR.IRequest<SubmitInterviewAnswerDto>;