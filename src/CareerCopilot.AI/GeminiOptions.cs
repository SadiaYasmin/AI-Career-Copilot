namespace CareerCopilot.AI;

public sealed class GeminiOptions
{
    public const string SectionName = "Ai";
    public const string ApiKeyEnv = "AI_API_KEY";
    public const string ModelEnv = "AI_MODEL";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
}