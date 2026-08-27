using System.Text.Json;
using CareerCopilot.Application.Scoring;

namespace CareerCopilot.Application.Features.RecruiterReadiness.Get;

internal static class ReadinessReportParser
{
    public static string Serialize(ReadinessReport report)
        => JsonSerializer.Serialize(new
        {
            overall = report.Overall,
            resume = report.ResumeScore,
            skills = report.SkillsScore,
            projects = report.ProjectsScore,
            profile = report.ProfileScore,
            interview = report.InterviewScore,
            actions = report.ImprovementActions
        });

    public static IReadOnlyList<string> ParseActions(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("actions", out var actions)
                && actions.ValueKind == JsonValueKind.Array)
            {
                return actions.EnumerateArray()
                    .Select(a => a.GetString() ?? string.Empty)
                    .Where(s => s.Length > 0)
                    .ToList();
            }
        }
        catch
        {
            // Fall through to empty list.
        }

        return new List<string>();
    }
}