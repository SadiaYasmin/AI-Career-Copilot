using System.Text.Json.Serialization;

namespace CareerCopilot.AI;

internal sealed class JsonJobAnalysisDto
{
    public string? title { get; set; }
    public string? company { get; set; }
    public string? location { get; set; }
    public string? employmentType { get; set; }
    public string? experienceRequirement { get; set; }
    public string? educationRequirement { get; set; }
    public List<JsonRequirementDto>? requirements { get; set; }
    public List<string>? keywords { get; set; }
    public List<string>? responsibilities { get; set; }
}

internal sealed class JsonRequirementDto
{
    public string? name { get; set; }
    public string? requirementType { get; set; }
    public string? importance { get; set; }
    public string? sourceText { get; set; }
}

internal sealed class JsonResumeAnalysisDto
{
    public int score { get; set; }
    public List<string>? strengths { get; set; }
    public List<string>? improvements { get; set; }
    public List<string>? atRiskFindings { get; set; }
    public string? summary { get; set; }
}

internal sealed class JsonMatchDto
{
    public List<JsonMatchItemDto>? matches { get; set; }
    public List<string>? recommendations { get; set; }
    public string? explanation { get; set; }
}

internal sealed class JsonMatchItemDto
{
    public string? name { get; set; }
    public string? status { get; set; }
    public string? evidence { get; set; }
}

internal sealed class JsonTailorDto
{
    public string? content { get; set; }
    public string? changesSummary { get; set; }
}

internal sealed class JsonCoverLetterDto
{
    public string? content { get; set; }
}

internal sealed class JsonInterviewQuestionsDto
{
    public List<JsonInterviewQuestionDto>? questions { get; set; }
}

internal sealed class JsonInterviewQuestionDto
{
    public string? question { get; set; }
    public string? questionType { get; set; }
}

internal sealed class JsonEvaluationDto
{
    public int score { get; set; }
    public int relevanceScore { get; set; }
    public int clarityScore { get; set; }
    public int technicalScore { get; set; }
    public int structureScore { get; set; }
    public int specificityScore { get; set; }
    public int concisenessScore { get; set; }
    public string? feedback { get; set; }
    public string? improvementSuggestion { get; set; }
    public string? followUpQuestion { get; set; }
}

internal sealed class JsonRoadmapDto
{
    public string? targetRole { get; set; }
    public string? description { get; set; }
    public List<JsonRoadmapTaskDto>? tasks { get; set; }
}

internal sealed class JsonRoadmapTaskDto
{
    public string? title { get; set; }
    public string? description { get; set; }
    public string? month { get; set; }
    public string? skill { get; set; }
    public string? priority { get; set; }
}

internal sealed class JsonCopilotDto
{
    public string? reply { get; set; }
}

internal sealed class JsonLinkedInDto
{
    public List<JsonLinkedInSuggestionDto>? suggestions { get; set; }
    public List<string>? strengths { get; set; }
    public int? score { get; set; }
}

internal sealed class JsonLinkedInSuggestionDto
{
    public string? section { get; set; }
    public string? original { get; set; }
    public string? improved { get; set; }
    public string? reasoning { get; set; }
}