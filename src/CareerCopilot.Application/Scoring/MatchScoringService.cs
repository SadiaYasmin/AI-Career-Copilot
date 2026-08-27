using System.Text.RegularExpressions;
using CareerCopilot.Application.Common.Ai;
using Microsoft.Extensions.Options;

namespace CareerCopilot.Application.Scoring;

public sealed record MatchEngineInput(
    IReadOnlyList<string> CandidateSkills,
    IReadOnlyList<AiExperience> Experience,
    IReadOnlyList<AiProject> Projects,
    IReadOnlyList<AiEducation> Education,
    double YearsOfExperience,
    string TargetRole,
    string TargetIndustries,
    IReadOnlyList<string> Certifications,
    string ProfileSummary,
    IReadOnlyList<AiJobRequirement> Requirements,
    string JobTitle,
    string JobDescription,
    string? ResumeText);

/// <summary>
/// Transparent, deterministic job match engine. Produces explainable scores and
/// evidence without relying on AI. Output is not a hiring probability.
/// </summary>
public sealed class MatchScoringService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "that", "this", "you", "your", "will", "have", "are", "our", "their",
        "from", "into", "over", "jobs", "work", "role", "team", "ability", "experience", "years", "plus",
        "using", "required", "preferred", "skills", "strong", "good", "etc", "per", "within", "across",
        "also", "such", "including", "related", "knowledge", "understanding", "responsibilities", "about"
    };

    private readonly MatchScoringOptions _options;

    public MatchScoringService(IOptions<MatchScoringOptions> options)
    {
        _options = options.Value;
    }

    public MatchResult Calculate(MatchEngineInput input)
    {
        var normalizedSkills = Normalize(input.CandidateSkills);
        var required = input.Requirements
            .Where(r => r.RequirementType == "Required")
            .ToList();
        var preferred = input.Requirements
            .Where(r => r.RequirementType == "Preferred")
            .ToList();

        var skillCoverage = EvaluateSkills(normalizedSkills, required, preferred, input);

        var experienceScore = ScoreExperience(input);
        var educationScore = ScoreEducation(input);
        var projectScore = ScoreProjects(input, required, normalizedSkills);
        var keywordScore = ScoreKeywords(input, normalizedSkills);
        var alignmentScore = ScoreAlignment(input);

        var totalWeight = _options.Skills + _options.Experience + _options.Projects
            + _options.Education + _options.Keywords + _options.Alignment;

        totalWeight = totalWeight <= 0 ? 100 : totalWeight;

        var overall = (int)Math.Round(
            (skillCoverage.SkillsScore * _options.Skills
             + experienceScore * _options.Experience
             + projectScore * _options.Projects
             + educationScore * _options.Education
             + keywordScore * _options.Keywords
             + alignmentScore * _options.Alignment) / (double)totalWeight);

        (var strong, var partial, var missing, var evidence) = BuildMatchItems(
            input, required, preferred, skillCoverage);

        var recommendations = BuildRecommendations(input, required, preferred, missing, experienceScore, projectScore);
        var explanation = BuildExplanation(overall, strong, missing, input.JobTitle);

        return new MatchResult(
            new MatchScoreBreakdown(overall, skillCoverage.SkillsScore, experienceScore,
                educationScore, projectScore, keywordScore, alignmentScore),
            strong, partial, missing, evidence, recommendations, explanation, DateTime.UtcNow);
    }

    private static IReadOnlyList<string> Normalize(IReadOnlyList<string> skills)
        => skills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(SafeTrim)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string SafeTrim(string s)
        => s.Trim().Replace("  ", " ").TrimEnd('.', ',', ';');

    private (int SkillsScore, Dictionary<string, string> StatusBySkill) EvaluateSkills(
        IReadOnlyList<string> candidateSkills,
        IReadOnlyList<AiJobRequirement> required,
        IReadOnlyList<AiJobRequirement> preferred,
        MatchEngineInput input)
    {
        var statusBySkill = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var allNameTokens = new List<string>();

        foreach (var req in required.Concat(preferred))
        {
            var name = SafeTrim(req.Name);
            if (name.Length == 0)
            {
                continue;
            }

            var (status, matchedName) = MatchSkill(name, candidateSkills, AllText(input));
            statusBySkill[name] = status;
            allNameTokens.Add(name);
            _ = matchedName;
        }

        var matchedRequired = required.Count(r => statusBySkill.TryGetValue(SafeTrim(r.Name), out var s) && s == "Matched");
        var partiallyRequired = required.Count(r => statusBySkill.TryGetValue(SafeTrim(r.Name), out var s) && s == "NeedsImprovement");

        var preferredRequired = preferred.Count(r => statusBySkill.TryGetValue(SafeTrim(r.Name), out var s) && s == "Matched");

        var skillsScore = required.Count == 0
            ? (preferred.Count == 0 ? 50 : RoundPct(preferredRequired, preferred.Count))
            : RoundPct(matchedRequired * 2 + partiallyRequired, required.Count * 2);

        if (required.Count > 0 && matchedRequired == 0 && partiallyRequired == 0)
        {
            skillsScore = 0;
        }

        return (skillsScore, statusBySkill);
    }

    private static string AllText(MatchEngineInput input)
        => string.Join(" ",
            input.ProfileSummary,
            string.Join(" ", input.Experience.Select(e => e.Summary)),
            string.Join(" ", input.Projects.Select(p => p.Description)),
            string.Join(" ", input.Projects.Select(p => p.Technologies)),
            input.ResumeText ?? string.Empty);

    private static (string Status, string MatchedName) MatchSkill(string requirement, IReadOnlyList<string> skills, string allText)
    {
        var r = SafeTrim(requirement);
        if (r.Length == 0)
        {
            return ("Missing", r);
        }

        var broadText = allText.ToLowerInvariant();

        foreach (var skill in skills)
        {
            if (string.Equals(skill, r, StringComparison.OrdinalIgnoreCase))
            {
                return ("Matched", skill);
            }

            if ((r.Length >= 4 && skill.Contains(r, StringComparison.OrdinalIgnoreCase))
                || (skill.Length >= 4 && r.Contains(skill, StringComparison.OrdinalIgnoreCase)))
            {
                return ("Matched", skill);
            }
        }

        if (r.Length >= 4 && broadText.Contains(r.ToLowerInvariant()))
        {
            return ("Matched", r);
        }

        return ("Missing", r);
    }

    private static int ScoreExperience(MatchEngineInput input)
    {
        var requiredYears = ExtractRequiredYears(input.JobDescription, input.Requirements);
        var candidateYears = Math.Max(0, input.YearsOfExperience);

        var hasRelevantRole = input.Experience.Any(e =>
            TokenOverlap(e.Title + " " + e.Company, input.JobTitle) > 0.5);

        if (requiredYears <= 0)
        {
            if (candidateYears >= 2)
            {
                return 90;
            }

            if (input.Experience.Count > 0)
            {
                return 70;
            }

            return 40;
        }

        var ratio = Math.Min(candidateYears / (double)requiredYears, 1.0);
        var score = (int)Math.Round(ratio * 100);
        if (hasRelevantRole && score < 100)
        {
            score = Math.Min(100, score + 10);
        }

        return score;
    }

    private static int ExtractRequiredYears(string jobDescription, IReadOnlyList<AiJobRequirement> requirements)
    {
        var text = jobDescription + " " + string.Join(" ", requirements.Select(r => r.Name + " " + r.SourceText));
        var matches = Regex.Matches(text, @"(\d{1,2})\s*(?:\+|plus\s*)?\s*years?", RegexOptions.IgnoreCase);
        var years = matches
            .Select(m => int.TryParse(m.Groups[1].Value, out var v) ? v : 0)
            .Where(v => v > 0 && v <= 30)
            .DefaultIfEmpty(0)
            .Max();
        return years;
    }

    private static int ScoreEducation(MatchEngineInput input)
    {
        var requiredLevel = RequiredEducationLevel(input.Requirements, input.JobDescription);
        if (requiredLevel <= 0)
        {
            return 100;
        }

        var candidateLevel = input.Education.Count == 0
            ? 0
            : input.Education.Max(e => EducationLevel(e.Degree ?? string.Empty));

        if (candidateLevel >= requiredLevel)
        {
            return 100;
        }

        return candidateLevel > 0 ? 70 : 30;
    }

    private static int RequiredEducationLevel(IReadOnlyList<AiJobRequirement> requirements, string jobDescription)
    {
        var text = string.Join(" ", requirements.Select(r => r.Name + " " + r.SourceText));
        text += " " + jobDescription;

        if (Regex.IsMatch(text, @"doctorat|ph\.?d|phd", RegexOptions.IgnoreCase))
        {
            return 3;
        }

        if (Regex.IsMatch(text, @"master'?s|postgraduate|graduate degree", RegexOptions.IgnoreCase))
        {
            return 2;
        }

        if (Regex.IsMatch(text, @"bachelor'?s|b\.?s\.?|b\.?a\.?|undergraduate|4-year|four.year degree", RegexOptions.IgnoreCase))
        {
            return 1;
        }

        if (Regex.IsMatch(text, @"associate|diploma|high school", RegexOptions.IgnoreCase))
        {
            return 1;
        }

        return 0;
    }

    private static int EducationLevel(string degree) => degree.ToLowerInvariant() switch
    {
        var d when d.Contains("phd") || d.Contains("ph.d") || d.Contains("doctor") => 3,
        var d when d.Contains("master") => 2,
        var d when d.Contains("bachelor") || d.Contains("b.s.") || d.Contains("b.a.") || d.StartsWith("b") => 1,
        var d when d.Contains("associate") || d.Contains("diploma") => 1,
        _ => 1
    };

    private static int ScoreProjects(MatchEngineInput input, IReadOnlyList<AiJobRequirement> required, IReadOnlyList<string> candidateSkills)
    {
        if (input.Projects.Count == 0)
        {
            return 40;
        }

        var projectText = string.Join(" ",
            input.Projects.SelectMany(p => new[] { p.Technologies, p.Description }));

        var requiredNames = required.Select(r => SafeTrim(r.Name)).Where(n => n.Length > 0).ToList();

        var covered = requiredNames.Count == 0
            ? 0
            : requiredNames.Count(r => projectText.Contains(r, StringComparison.OrdinalIgnoreCase)
                || candidateSkills.Any(s => s.Contains(r, StringComparison.OrdinalIgnoreCase)));

        var skillPart = requiredNames.Count == 0
            ? 100
            : RoundPct(covered, requiredNames.Count);

        var breadthBonus = Math.Min(100, 60 + input.Projects.Count * 10);

        return (int)Math.Round((skillPart * 0.7) + (breadthBonus * 0.3));
    }

    private static int ScoreKeywords(MatchEngineInput input, IReadOnlyList<string> candidateSkills)
    {
        var keywords = ExtractKeywords(input.JobDescription);
        if (keywords.Count == 0)
        {
            return 60;
        }

        var haystack = string.Join(" ",
            candidateSkills,
            string.Join(" ", input.Experience.Select(e => e.Summary)),
            string.Join(" ", input.Projects.Select(p => p.Name + " " + p.Technologies + " " + p.Description)),
            input.ProfileSummary)
            .ToLowerInvariant();

        var hits = keywords.Count(k => haystack.Contains(k));
        var keywordPart = RoundPct(hits, keywords.Count);

        var requiredCoverage = input.Requirements
            .Where(r => r.RequirementType == "Required" && SafeTrim(r.Name).Length > 0)
            .ToList();

        var skillPart = requiredCoverage.Count == 0
            ? 100
            : RoundPct(requiredCoverage.Count(r => candidateSkills.Any(s => s == SafeTrim(r.Name) || s.Contains(SafeTrim(r.Name)))), requiredCoverage.Count);

        return (int)Math.Round((keywordPart * 0.5) + (skillPart * 0.5));
    }

    private static int ScoreAlignment(MatchEngineInput input)
    {
        var score = 55;

        var roleSimilarity = TokenOverlap(input.TargetRole, input.JobTitle);
        if (roleSimilarity >= 0.8)
        {
            score += 30;
        }
        else if (roleSimilarity >= 0.4)
        {
            score += 15;
        }

        if (!string.IsNullOrWhiteSpace(input.TargetRole)
            && (input.JobTitle.Contains("Frontend", StringComparison.OrdinalIgnoreCase)
                && input.TargetRole.Contains("Frontend", StringComparison.OrdinalIgnoreCase)))
        {
            score += 5;
        }

        if (!string.IsNullOrWhiteSpace(input.TargetIndustries))
        {
            var industries = input.TargetIndustries.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(SafeTrim).ToList();
            if (industries.Count > 0 && input.JobDescription.Length > 0)
            {
                var hit = industries.Any(i => i.Length >= 3 && input.JobDescription.Contains(i, StringComparison.OrdinalIgnoreCase));
                if (hit)
                {
                    score += 10;
                }
            }
        }

        if (input.Experience.Count > 0 || input.YearsOfExperience > 0)
        {
            score += 5;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static double TokenOverlap(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return 0;
        }

        var tokensA = a.Split(new[] { ' ', ',', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(SafeTrim).Where(t => t.Length >= 2).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tokensB = b.Split(new[] { ' ', ',', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(SafeTrim).Where(t => t.Length >= 2).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (tokensA.Count == 0 || tokensB.Count == 0)
        {
            return 0;
        }

        var intersect = tokensA.Count(tokensB.Contains);
        return intersect / (double)Math.Max(tokensA.Count, tokensB.Count);
    }

    private static List<string> ExtractKeywords(string jobDescription)
    {
        var tokens = Regex.Matches(jobDescription.ToLowerInvariant(), @"[a-z0-9#.+\-]{4,}")
            .Select(m => m.Value)
            .Where(t => !StopWords.Contains(t))
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(24)
            .Select(g => g.Key)
            .ToList();
        return tokens;
    }

    private static int RoundPct(int numerator, int denominator)
        => denominator <= 0 ? 100 : (int)Math.Round((numerator / (double)denominator) * 100);

    private static (List<string> Strong, List<string> Partial, List<string> Missing, List<MatchItemFinding> Evidence)
        BuildMatchItems(
            MatchEngineInput input,
            IReadOnlyList<AiJobRequirement> required,
            IReadOnlyList<AiJobRequirement> preferred,
            (int SkillsScore, Dictionary<string, string> StatusBySkill) coverage)
    {
        var strong = new List<string>();
        var partial = new List<string>();
        var missing = new List<string>();
        var evidence = new List<MatchItemFinding>();

        string statusOf(AiJobRequirement r)
            => coverage.StatusBySkill.TryGetValue(SafeTrim(r.Name), out var s) ? s : "Missing";

        foreach (var r in required)
        {
            var name = SafeTrim(r.Name);
            if (name.Length == 0)
            {
                continue;
            }

            var status = statusOf(r);
            var (source, detail) = FindEvidence(name, input);

            switch (status)
            {
                case "Matched":
                    strong.Add(name);
                    evidence.Add(new MatchItemFinding(name, "Strong match", source, detail));
                    break;
                case "NeedsImprovement":
                    partial.Add(name);
                    evidence.Add(new MatchItemFinding(name, "Partial match", source, detail));
                    break;
                default:
                    missing.Add(name);
                    break;
            }
        }

        foreach (var r in preferred)
        {
            var name = SafeTrim(r.Name);
            if (name.Length == 0)
            {
                continue;
            }

            var status = statusOf(r);
            if (status == "Matched")
            {
                partial.Add(name);
                var (source, detail) = FindEvidence(name, input);
                evidence.Add(new MatchItemFinding(name, "Strong match", source, detail));
            }
            else
            {
                var (source, detail) = FindEvidence(name, input);
                if (source.Length > 0)
                {
                    partial.Add(name);
                    evidence.Add(new MatchItemFinding(name, "Partial match", source, detail));
                }
            }
        }

        return (strong, partial, missing, evidence);
    }

    private static (string Source, string Detail) FindEvidence(string skill, MatchEngineInput input)
    {
        var s = SafeTrim(skill);

        var project = input.Projects.FirstOrDefault(p =>
            p.Technologies.Contains(s, StringComparison.OrdinalIgnoreCase));
        if (project is not null)
        {
            return ("Project", $"{project.Name} uses {s}");
        }

        var skillInProfile = input.CandidateSkills.FirstOrDefault(c =>
            string.Equals(c, s, StringComparison.OrdinalIgnoreCase)
            || c.Contains(s, StringComparison.OrdinalIgnoreCase));
        if (skillInProfile is not null)
        {
            return ("Profile skills", skillInProfile);
        }

        var exp = input.Experience.FirstOrDefault(e =>
            e.Summary.Contains(s, StringComparison.OrdinalIgnoreCase));
        if (exp is not null)
        {
            return ("Experience", $"{exp.Company} - {exp.Title}");
        }

        if (!string.IsNullOrWhiteSpace(input.ResumeText))
        {
            var line = input.ResumeText.Split('\n')
                .FirstOrDefault(l => l.Contains(s, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(line))
            {
                return ("Resume", line.Trim().Length > 80 ? line.Trim()[..80] : line.Trim());
            }
        }

        return (string.Empty, string.Empty);
    }

    private static List<string> BuildRecommendations(
        MatchEngineInput input,
        IReadOnlyList<AiJobRequirement> required,
        IReadOnlyList<AiJobRequirement> preferred,
        IReadOnlyList<string> missing,
        int experienceScore,
        int projectScore)
    {
        var recommendations = new List<string>();

        foreach (var m in missing.Take(5))
        {
            recommendations.Add($"Build demonstrable {m} experience so it can be added to your profile.");
        }

        foreach (var r in required)
        {
            var name = SafeTrim(r.Name);
            if (name.Length == 0)
            {
                continue;
            }

            if (preferred.Any(p => SafeTrim(p.Name) == name))
            {
                recommendations.Add($"Your preferred {name} is a plus - highlight relevant work if you have it.");
            }
        }

        if (experienceScore < 70)
        {
            recommendations.Add("Gain more hands-on experience in the target role's core responsibilities.");
        }

        if (projectScore < 70)
        {
            recommendations.Add("Strengthen your project portfolio with work that exercises the job's required skills.");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("You are a strong match. Focus on interview preparation and application quality.");
        }

        return recommendations.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string BuildExplanation(int overall, IReadOnlyList<string> strong, IReadOnlyList<string> missing, string jobTitle)
    {
        var parts = new List<string>
        {
            $"Your profile matches {overall}% of what this {jobTitle} role asks for, based on skills, experience, projects, education, keywords and career alignment."
        };

        if (strong.Count > 0)
        {
            parts.Add($"You strongly match: {string.Join(", ", strong.Take(5))}.");
        }

        if (missing.Count > 0)
        {
            parts.Add($"Missing: {string.Join(", ", missing.Take(5))}.");
        }

        parts.Add("This AI match score indicates profile alignment with the job description. It is not a prediction of hiring outcome.");

        return string.Join(" ", parts);
    }
}