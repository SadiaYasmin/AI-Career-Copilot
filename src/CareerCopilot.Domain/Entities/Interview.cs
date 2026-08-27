using CareerCopilot.Domain.Enums;
using CareerCopilot.Domain.Exceptions;

namespace CareerCopilot.Domain.Entities;

public sealed class InterviewSession : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid JobId { get; set; }
    public InterviewMode Mode { get; set; } = InterviewMode.Mixed;
    public int? OverallScore { get; set; }
    public string? Summary { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public User? User { get; set; }
    public Job? Job { get; set; }
    public ICollection<InterviewQuestion> Questions { get; set; } = new List<InterviewQuestion>();

    public bool IsCompleted => CompletedAt is not null;

    public void Complete(int score, string summary, DateTime utcNow)
    {
        if (IsCompleted)
        {
            throw new DomainRuleException("This interview session is already completed.");
        }

        OverallScore = score;
        Summary = summary;
        CompletedAt = utcNow;
        UpdatedAt = utcNow;
    }
}

public sealed class InterviewQuestion : AuditableEntity
{
    public Guid InterviewSessionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; } = QuestionType.Behavioral;
    public int Order { get; set; }

    public InterviewSession? InterviewSession { get; set; }
    public ICollection<InterviewAnswer> Answers { get; set; } = new List<InterviewAnswer>();
}

public sealed class InterviewAnswer : AuditableEntity
{
    public Guid InterviewQuestionId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public string? ImprovementSuggestion { get; set; }

    public InterviewQuestion? InterviewQuestion { get; set; }
    public InterviewEvaluation? Evaluation { get; set; }
}

public sealed class InterviewEvaluation : AuditableEntity
{
    public Guid InterviewAnswerId { get; set; }
    public int Score { get; set; }
    public int RelevanceScore { get; set; }
    public int ClarityScore { get; set; }
    public int TechnicalScore { get; set; }
    public int StructureScore { get; set; }
    public int SpecificityScore { get; set; }
    public int ConcisenessScore { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public string ImprovementSuggestion { get; set; } = string.Empty;

    public InterviewAnswer? InterviewAnswer { get; set; }
}