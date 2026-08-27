using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Scoring;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CareerCopilot.UnitTests;

public sealed class MatchScoringServiceTests
{
    private static MatchScoringService Create() => new(Options.Create(new MatchScoringOptions()));

    private static readonly AiJobRequirement RequiredCsharp =
        new("C#", "Required", "High", "Strong C# skills required for backend services.");
    private static readonly AiJobRequirement RequiredSql =
        new("PostgreSQL", "Required", "High", "PostgreSQL experience required.");
    private static readonly AiJobRequirement PreferredRest =
        new("REST API", "Preferred", "Medium", "REST API design is a plus.");

    [Fact]
    public void StrongCandidate_Outscores_WeakCandidate()
    {
        var service = Create();

        var strongInput = Build(required: new[] { RequiredCsharp, RequiredSql }, skills: new[] { "C#", "PostgreSQL", "REST API" });
        var weakInput = Build(required: new[] { RequiredCsharp, RequiredSql }, skills: new[] { "Marketing", "Excel" });

        var strong = service.Calculate(strongInput);
        var weak = service.Calculate(weakInput);

        strong.Scores.Overall.Should().BeGreaterThan(weak.Scores.Overall);
        weak.MissingRequirements.Should().Contain("C#");
    }

    [Fact]
    public void Scores_StayWithinRange()
    {
        var service = Create();

        var result = service.Calculate(Build(
            required: new[] { RequiredCsharp, RequiredSql },
            skills: new[] { "C#", "PostgreSQL" }));

        result.Scores.Overall.Should().BeInRange(0, 100);
        result.Scores.Skills.Should().BeInRange(0, 100);
        result.Scores.Experience.Should().BeInRange(0, 100);
        result.Explanation.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void NoRequirements_ScoresWithoutThrowing()
    {
        var service = Create();

        var result = service.Calculate(Build(required: Array.Empty<AiJobRequirement>(), skills: new[] { "C#" }));

        result.Scores.Overall.Should().BeInRange(0, 100);
    }

    private static MatchEngineInput Build(
        IReadOnlyList<AiJobRequirement> required,
        IReadOnlyList<string> skills)
        => new(
            skills,
            new[]
            {
                new AiExperience("ACME", "Backend Engineer",
                    "Built payment APIs with C#, handled 1M requests a day.")
            },
            new[]
            {
                new AiProject("Order service", "Scalable order processing backend", "C#, PostgreSQL")
            },
            new[]
            {
                new AiEducation("BSc", "Computer Science")
            },
            5,
            "Backend Engineer",
            "Technology",
            new[] { "AZ-204" },
            "Backend engineer focused on reliable services.",
            required,
            "Backend Engineer",
            "We are looking for a Backend Engineer. Strong C# and PostgreSQL experience is required. " +
            "You will build and maintain REST APIs used by 100k customers.",
            null);
}