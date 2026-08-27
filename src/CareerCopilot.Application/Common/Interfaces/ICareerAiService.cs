using CareerCopilot.Application.Common.Ai;

namespace CareerCopilot.Application.Common.Interfaces;

/// <summary>
/// Career AI orchestration. The Application layer depends only on this abstraction.
/// Implementations are AI-provider specific and replaceable.
/// </summary>
public interface ICareerAiService
{
    Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisContext context, CancellationToken cancellationToken);
    Task<ResumeAnalysisResult> AnalyzeResumeAsync(ResumeAnalysisContext context, CancellationToken cancellationToken);
    Task<MatchAiResult> ExplainMatchAsync(MatchAiContext context, CancellationToken cancellationToken);
    Task<TailorResumeResult> TailorResumeAsync(TailorResumeContext context, CancellationToken cancellationToken);
    Task<CoverLetterResult> GenerateCoverLetterAsync(CoverLetterContext context, CancellationToken cancellationToken);
    Task<InterviewQuestionsResult> GenerateInterviewQuestionsAsync(InterviewContext context, CancellationToken cancellationToken);
    Task<AnswerEvaluationResult> EvaluateAnswerAsync(AnswerEvaluationContext context, CancellationToken cancellationToken);
    Task<CareerRoadmapResult> GenerateCareerRoadmapAsync(CareerRoadmapContext context, CancellationToken cancellationToken);
    Task<string> GenerateCopilotReplyAsync(CopilotContext context, CancellationToken cancellationToken);
    Task<LinkedInAnalysisResult> AnalyzeLinkedInAsync(LinkedInContext context, CancellationToken cancellationToken);
    Task<string> GenerateInterviewCompletionSummaryAsync(
        string questionSummary,
        string role,
        CancellationToken cancellationToken);
}