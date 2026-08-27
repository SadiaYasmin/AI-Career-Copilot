using CareerCopilot.Application.Scoring;
using FluentAssertions;

namespace CareerCopilot.UnitTests;

public sealed class ResumeAnalyzerServiceTests
{
    private static readonly ResumeAnalyzerService Service = new();

    private const string StrongResume =
        "Summary\nBackend developer with 5 years of experience building web services.\n\n" +
        "Email: john@example.com\nPhone: +49 151 12345678\nLinkedIn: https://linkedin.com/in/john\n\n" +
        "Skills\n- C#\n- ASP.NET Core\n- PostgreSQL\n\n" +
        "Experience\n" +
        "- Built a payments platform used by 10000 users, reducing latency by 40%.\n" +
        "- Led a team of 3 engineers at ACME.\n" +
        "- Designed and shipped an order service used by 5000 customers.\n\n" +
        "Education\nBSc Computer Science\n\n" +
        "Projects\n- Developed an inventory API with C# and PostgreSQL, cutting errors by 25%.\n" +
        "Certifications\n- Microsoft AZ-204";

    [Fact]
    public void EmptyText_ScoresZero_WithAtRiskFinding()
    {
        var result = Service.Analyze(string.Empty);

        result.Score.Should().Be(0);
        result.AtRiskFindings.Should().ContainMatch("*No text could be extracted*");
    }

    [Fact]
    public void WellStructuredResume_ScoresAboveSixty()
    {
        var result = Service.Analyze(StrongResume);

        result.Score.Should().BeGreaterThanOrEqualTo(60);
        result.Sections.HasSummary.Should().BeTrue();
        result.Sections.HasExperience.Should().BeTrue();
        result.Sections.HasEducation.Should().BeTrue();
        result.Sections.HasProjects.Should().BeTrue();
    }

    [Fact]
    public void Analyzer_IsDeterministic()
    {
        var first = Service.Analyze(StrongResume);
        var second = Service.Analyze(StrongResume);

        first.Score.Should().Be(second.Score);
    }

    [Fact]
    public void MissingSections_ProduceImprovementSuggestions()
    {
        var result = Service.Analyze("No sections here, just a short line.");

        result.Score.Should().BeLessThan(60);
        result.Improvements.Should().NotBeEmpty();
    }
}