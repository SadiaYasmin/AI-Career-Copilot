using CareerCopilot.Application.Common.Ai;

namespace CareerCopilot.Application.Scoring;

public sealed record SkillGapEntry(
    string SkillName,
    string GapType,
    string Priority,
    string CurrentLevel,
    string RequiredLevel,
    string Recommendation,
    string LearningPath);

public sealed class SkillGapService
{
    public IReadOnlyList<SkillGapEntry> Calculate(
        IReadOnlyList<AiJobRequirement> requirements,
        IReadOnlyList<string> candidateSkills)
    {
        if (requirements.Count == 0)
        {
            return new List<SkillGapEntry>();
        }

        var candidate = candidateSkills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().TrimEnd('.', ',', ';'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var results = new List<SkillGapEntry>();

        foreach (var r in requirements)
        {
            var name = r.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                continue;
            }

            var status = Classify(name, candidate);
            var isRequired = r.RequirementType == "Required";

            var priority = (status, isRequired) switch
            {
                ("Matched", _) => "Low",
                ("NeedsImprovement", true) => "High",
                ("NeedsImprovement", false) => "Medium",
                ("Missing", true) => "Critical",
                ("Missing", false) => isPreferredOrInferred(r) ? "Medium" : "High",
                _ => "Medium"
            };

            var recommendation = status switch
            {
                "Matched" => $"Keep {name} up to date and show measurable impact in your applications.",
                "NeedsImprovement" => $"Deepen hands-on {name} experience through practice and a small project before listing it as a core strength.",
                _ => $"Gain genuine {name} experience - learn the fundamentals, build a project, and only then add it to your resume.",
            };

            results.Add(new SkillGapEntry(
                name,
                status,
                priority,
                CurrentLevel(name, candidate),
                r.Importance.Length > 0 ? r.Importance : (isRequired ? "Required" : "Preferred"),
                recommendation,
                LearningPath(name, status)));
        }

        return results
            .OrderByDescending(e => PriorityRank(e.Priority))
            .ToList();
    }

    private static bool isPreferredOrInferred(AiJobRequirement r)
        => r.RequirementType == "Preferred";

    private static string Classify(string skill, IReadOnlySet<string> candidate)
    {
        var s = skill.Trim();

        if (candidate.Contains(s))
        {
            return "Matched";
        }

        var isContained = candidate.Any(c =>
            (c.Length >= 4 && c.Contains(s, StringComparison.OrdinalIgnoreCase))
            || (s.Length >= 4 && s.Contains(c, StringComparison.OrdinalIgnoreCase)));

        return isContained ? "NeedsImprovement" : "Missing";
    }

    private static string CurrentLevel(string skill, IReadOnlySet<string> candidate)
        => candidate.Contains(skill) ? "Listed in profile" : "Not in profile";

    private static int PriorityRank(string priority) => priority switch
    {
        "Critical" => 0,
        "High" => 1,
        "Medium" => 2,
        _ => 3
    };

    private static string LearningPath(string skill, string status)
    {
        if (status == "Matched")
        {
            return "Continue deepening and applying this skill in real projects.";
        }

        var basePath = $"1. Learn {skill} fundamentals via structured courses or official documentation. " +
                       $"2. Build a small project that uses {skill} end to end. " +
                       "3. Capture concrete outcomes to mention in interviews and on your resume.";

        return basePath;
    }
}