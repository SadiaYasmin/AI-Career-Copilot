namespace CareerCopilot.Application.Common.Ai;

public sealed record InterviewContext(
    AiJobSnapshot Job,
    AiPersonSnapshot Person,
    AiResumeSnapshot? Resume,
    string Mode,
    IReadOnlyList<string> JobResponsibilities,
    IReadOnlyList<string> RequiredSkills);

public sealed record AiInterviewQuestion(string Question, string QuestionType);

public sealed record InterviewQuestionsResult(IReadOnlyList<AiInterviewQuestion> Questions);

public sealed record AnswerEvaluationContext(
    string Question,
    string QuestionType,
    string Answer,
    AiJobSnapshot Job,
    AiPersonSnapshot Person,
    AiProject? CurrentProject);

public sealed record AnswerEvaluationResult(
    int Score,
    int RelevanceScore,
    int ClarityScore,
    int TechnicalScore,
    int StructureScore,
    int SpecificityScore,
    int ConcisenessScore,
    string Feedback,
    string ImprovementSuggestion,
    string? FollowUpQuestion);