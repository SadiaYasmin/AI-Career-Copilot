using System.Text.RegularExpressions;

namespace CareerCopilot.Application.Scoring;

public sealed record ResumeSectionPresence(
    bool HasSummary,
    bool HasSkills,
    bool HasExperience,
    bool HasEducation,
    bool HasProjects,
    bool HasCertifications,
    bool HasContact);

public sealed record ResumeAnalysis(
    int Score,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Improvements,
    IReadOnlyList<string> AtRiskFindings,
    ResumeSectionPresence Sections,
    string Summary);

/// <summary>
/// Deterministic resume quality and ATS-readiness analyzer. Fully explainable.
/// Never fabricates candidate facts - it only analyzes the supplied text.
/// </summary>
public sealed class ResumeAnalyzerService
{
    private static readonly HashSet<string> ActionVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "developed", "built", "implemented", "designed", "led", "managed", "delivered", "created",
        "improved", "reduced", "increased", "optimized", "architected", "launched", "automated",
        "engineered", "integrated", "mentored", "coordinated", "analyzed", "streamlined", "deployed",
        "migrated", "refactored", "collaborated", "initiated", "resolved", "accelerated", "spearheaded"
    };

    public ResumeAnalysis Analyze(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ResumeAnalysis(0, new List<string>(), new List<string>(),
                new List<string> { "No text could be extracted from the resume." },
                new ResumeSectionPresence(false, false, false, false, false, false, false),
                "We could not extract text from this resume.");
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var body = string.Join("\n", lines);
        var lower = body.ToLowerInvariant();

        var sections = DetectSections(lines);

        var results = new List<(int Score, string Finding)>();
        var strengths = new List<string>();
        var improvements = new List<string>();
        var atRisk = new List<string>();

        // Contact information
        var hasEmail = Regex.IsMatch(lower, @"[a-z0-9._%+\-]+@[a-z0-9.\-]+\.[a-z]{2,}");
        var hasPhone = Regex.IsMatch(text, @"(\+?[\d\s()\-]{7,})");
        var hasLinks = Regex.IsMatch(lower, @"linkedin\.com|github\.com|http|www\.");

        var contactScore = hasEmail && (hasPhone || hasLinks) ? 10 : hasEmail ? 7 : 3;
        if (!hasEmail)
        {
            improvements.Add("Email is missing - a resume without reliable contact details is often filtered out.");
        }
        if (!hasLinks)
        {
            improvements.Add("Add LinkedIn/GitHub links to support verification and show work.");
        }

        // Sections
        var presentSections = new[]
        {
            ("Summary", sections.HasSummary),
            ("Skills", sections.HasSkills),
            ("Experience", sections.HasExperience),
            ("Education", sections.HasEducation),
            ("Projects", sections.HasProjects)
        }.Where(x => x.Item2).Select(x => x.Item1).ToList();

        var sectionScore = (int)Math.Round((presentSections.Count / 5.0) * 25);
        if (presentSections.Count >= 4)
        {
            strengths.Add("Clear standard sections: " + string.Join(", ", presentSections));
        }
        else
        {
            improvements.Add("Missing standard sections - include Summary, Skills, Experience, Education and Projects.");
        }
        if (!sections.HasProjects)
        {
            improvements.Add("Projects section is missing - relevant projects strengthen ATS keyword coverage.");
        }

        // Action verbs
        var bullets = lines
            .Where(l => l.StartsWith("•", StringComparison.Ordinal) || l.StartsWith("-", StringComparison.Ordinal)
                || l.StartsWith("*", StringComparison.Ordinal) || l.StartsWith("·", StringComparison.Ordinal)
                || (Regex.IsMatch(l, @"^\d+[\.\)]") && !Regex.IsMatch(l, @"^\d{2,4}")))
            .ToList();

        var actionVerbHits = bullets.Count(b => ActionVerbs.Any(v => b.Contains(v, StringComparison.OrdinalIgnoreCase)));
        var actionScore = bullets.Count == 0
            ? 5
            : (int)Math.Round(Math.Min(1.0, actionVerbHits / (double)bullets.Count) * 15);

        if (bullets.Count == 0)
        {
            improvements.Add("Use bullet points for experience so ATS can parse achievements cleanly.");
        }
        else if (actionVerbHits == 0)
        {
            improvements.Add("Start bullets with action verbs (developed, built, led, improved).");
        }
        else
        {
            strengths.Add("Frequent action verbs: " + actionVerbHits + " of " + bullets.Count + " bullets.");
        }

        // Quantification
        var quantifiedCount = bullets.Count(b =>
            Regex.IsMatch(b, @"\b\d+\s?(%|x|users|customers|requests|projects|clients|members|applications)\b")
            || Regex.IsMatch(b, @"\$[\d,.]+"));

        var percentOfBullets = bullets.Count == 0 ? 0 : quantifiedCount / (double)bullets.Count;
        if (percentOfBullets >= 0.3)
        {
            strengths.Add("Measurable impact present in " + quantifiedCount + " bullets.");
        }
        else if (bullets.Count > 0)
        {
            improvements.Add("Only " + quantifiedCount + " of " + bullets.Count + " bullets include measurable outcomes.");
        }

        // ATS & formatting risks
        var atRiskFindings = new List<string>();
        var tableChars = lines.Count(l => l.Count(c => c == '|') >= 2);
        if (tableChars > 2)
        {
            atRiskFindings.Add("Heavy use of table/divider characters (|) may confuse older ATS parsers.");
        }
        if (lines.Any(l => l.Length > 160))
        {
            atRiskFindings.Add("Some lines are very long - keep experience bullets concise (1-2 lines).");
        }

        var score = Math.Clamp(contactScore + sectionScore + actionScore + QuantScore(quantifiedCount, bullets.Count) + AtsScore(sections), 0, 100);

        var summary = Summarize(score, presentSections, sections);

        if (score >= 75)
        {
            strengths.Add("Overall resume quality looks strong - keep refining with job-specific keywords.");
        }
        if (score < 60)
        {
            improvements.Add("Overall resume score is low - prioritize adding standard sections and measurable bullets.");
        }

        atRisk.AddRange(atRiskFindings);

        return new ResumeAnalysis(
            score,
            strengths.Distinct().Take(6).ToList(),
            improvements.Distinct().Take(6).ToList(),
            atRisk.Take(4).ToList(),
            sections,
            summary);
    }

    private static int QuantScore(int quantifiedCount, int bulletCount)
        => bulletCount == 0 ? 5 : (int)Math.Round(Math.Min(1.0, quantifiedCount / (double)bulletCount) * 15);

    private static int AtsScore(ResumeSectionPresence sections)
    {
        var s = 0;
        if (sections.HasSummary) s += 5;
        if (sections.HasSkills) s += 10;
        if (sections.HasExperience) s += 5;
        if (sections.HasEducation) s += 5;
        if (sections.HasCertifications) s += 3;
        return s;
    }

    private static ResumeSectionPresence DetectSections(IReadOnlyList<string> lines)
    {
        bool Has(string token) => lines.Any(l => l.Trim().TrimEnd(':').Equals(token, StringComparison.OrdinalIgnoreCase));

        return new ResumeSectionPresence(
            Has("Summary") || Has("Professional Summary") || Has("Profile") || Has("Objective"),
            Has("Skills") || Has("Technical Skills") || Has("Core Skills") || Has("Technologies"),
            Has("Experience") || Has("Work Experience") || Has("Professional Experience") || Has("Employment"),
            Has("Education") || Has("Academic Background") || Has("Academic"),
            Has("Projects") || Has("Project") || Has("Personal Projects") || Has("Selected Projects"),
            Has("Certifications") || Has("Certificates") || Has("Licenses"),
            true);
    }

    private static string Summarize(int score, IReadOnlyList<string> sections, ResumeSectionPresence presence)
        => score >= 80
            ? "Strong resume with standard sections and measurable impact. Tailor keywords per job and it is in good shape."
            : score >= 60
                ? "Solid resume. Add measurable outcomes and job-specific keywords to improve ATS performance."
                : "Resume needs significant improvement - add standard sections, skills and quantified achievements.";
}