namespace CareerCopilot.Application.Scoring;

public sealed record ReadinessInput(
    int? ResumeScore,
    int? LatestMatchScore,
    double ProfileCompleteness,
    int SkillCount,
    int ProjectCount,
    int InterviewCount,
    double? AverageInterviewScore);

public sealed record ReadinessReport(
    int Overall,
    int ResumeScore,
    int SkillsScore,
    int ProjectsScore,
    int ProfileScore,
    int InterviewScore,
    IReadOnlyList<string> ImprovementActions);

/// <summary>
/// Deterministic recruiter readiness score based on stored career metrics.
/// </summary>
public sealed class RecruiterReadinessService
{
    public ReadinessReport Calculate(ReadinessInput input)
    {
        var resumeScore = input.ResumeScore ?? (input.ResumeScore is null && HasResumeSignal(input) ? 60 : 30);
        var skillsScore = ScoreSkills(input.SkillCount, input.LatestMatchScore);
        var projectsScore = ScoreProjects(input.ProjectCount);
        var profileScore = (int)Math.Round(input.ProfileCompleteness * 100);
        var interviewScore = input.InterviewCount > 0
            ? (int)Math.Round(input.AverageInterviewScore ?? 60)
            : 55;

        var overall = (int)Math.Round(
            (resumeScore * 0.20)
            + (skillsScore * 0.25)
            + (projectsScore * 0.15)
            + (profileScore * 0.20)
            + (interviewScore * 0.20));

        var actions = new List<string>();
        if (resumeScore < 70)
        {
            actions.Add("Upload and analyze your resume to get concrete improvement suggestions.");
        }
        if (skillsScore < 70)
        {
            actions.Add("Run job matches to identify which required skills you still need.");
        }
        if (projectsScore < 70)
        {
            actions.Add("Add projects that demonstrate your target role's core technologies.");
        }
        if (profileScore < 75)
        {
            actions.Add("Complete your career profile so AI analysis has enough evidence.");
        }
        if (interviewScore < 70)
        {
            actions.Add("Practice with the AI interview simulator to improve answer structure.");
        }
        if (actions.Count == 0)
        {
            actions.Add("Keep monitoring job matches and interview scores to sustain readiness.");
        }

        return new ReadinessReport(
            overall, resumeScore, skillsScore, projectsScore, profileScore, interviewScore, actions);
    }

    private static bool HasResumeSignal(ReadinessInput input)
        => input.ResumeScore is not null;

    private static int ScoreSkills(int skillCount, int? latestMatchScore)
    {
        if (latestMatchScore is > 0)
        {
            return latestMatchScore.Value;
        }

        return Math.Clamp(30 + (skillCount * 5), 0, 95);
    }

    private static int ScoreProjects(int projectCount)
        => Math.Clamp(30 + (projectCount * 15), 0, 95);
}