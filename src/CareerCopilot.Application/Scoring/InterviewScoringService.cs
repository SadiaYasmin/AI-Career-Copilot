namespace CareerCopilot.Application.Scoring;

public sealed record AnswerScore(double Relevance, double Clarity, double Technical, double Structure, double Specificity, double Conciseness);

public sealed record InterviewReport(
    int OverallScore,
    int TechnicalKnowledge,
    int Communication,
    int ProblemSolving,
    int AnswerStructure,
    int RoleAlignment,
    IReadOnlyList<string> StrongAreas,
    IReadOnlyList<string> Improvements,
    IReadOnlyList<string> CombinedFeedback);

/// <summary>
/// Deterministic aggregation of per-answer interview evaluations into a session report.
/// The report dimensions mirror the PRD interview evaluation criteria.
/// </summary>
public sealed class InterviewScoringService
{
    private const double TechnicalWeight = 0.25;
    private const double CommunicationWeight = 0.20;
    private const double ProblemSolvingWeight = 0.20;
    private const double StructureWeight = 0.15;
    private const double RoleAlignmentWeight = 0.20;

    public InterviewReport BuildReport(IReadOnlyList<AnswerScore> answers)
    {
        if (answers.Count == 0)
        {
            return new InterviewReport(0, 0, 0, 0, 0, 0, new List<string>(), new List<string>(), new List<string>());
        }

        var technical = (int)Math.Round(answers.Average(a => a.Technical));
        var communication = (int)Math.Round(answers.Average(a => a.Clarity));
        var problemSolving = (int)Math.Round(answers.Average(a => (a.Specificity * 0.55) + (a.Relevance * 0.45)));
        var structure = (int)Math.Round(answers.Average(a => a.Structure));
        var roleAlignment = (int)Math.Round(answers.Average(a => (a.Technical * 0.40) + (a.Relevance * 0.60)));

        var overall = (int)Math.Round(
            technical * TechnicalWeight
            + communication * CommunicationWeight
            + problemSolving * ProblemSolvingWeight
            + structure * StructureWeight
            + roleAlignment * RoleAlignmentWeight);

        var categories = new List<(string Name, int Score)>
        {
            ("Technical Knowledge", technical),
            ("Communication", communication),
            ("Problem Solving", problemSolving),
            ("Answer Structure", structure),
            ("Role Alignment", roleAlignment)
        };

        var strong = categories.Where(c => c.Score >= 80).Select(c => $"{c.Name}: {c.Score}").ToList();
        var improvements = categories.Where(c => c.Score < 70).Select(c => $"{c.Name}: {c.Score}").ToList();

        var combinedFeedback = new List<string>();
        if (strong.Count > 0)
        {
            combinedFeedback.Add("Strong areas: " + string.Join(", ", strong.Select(s => s.Split(':')[0])));
        }
        if (improvements.Count > 0)
        {
            combinedFeedback.Add("Improve: " + string.Join(", ", improvements.Select(s => s.Split(':')[0])));
        }
        if (answers.Count < 5)
        {
            combinedFeedback.Add("Complete more questions for a more reliable assessment.");
        }

        return new InterviewReport(
            overall, technical, communication, problemSolving, structure, roleAlignment,
            strong, improvements, combinedFeedback);
    }
}