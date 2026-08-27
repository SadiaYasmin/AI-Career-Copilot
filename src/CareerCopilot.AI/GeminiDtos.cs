using System.Text.Json.Serialization;

namespace CareerCopilot.AI;

internal sealed class GeminiRequestDto
{
    [JsonPropertyName("systemInstruction")]
    public GeminiPartDto? systemInstruction { get; set; }

    [JsonPropertyName("contents")]
    public GeminiContentDto[]? contents { get; set; }

    [JsonPropertyName("generationConfig")]
    public GeminiConfigDto? generationConfig { get; set; }
}

internal sealed class GeminiContentDto
{
    [JsonPropertyName("role")]
    public string role { get; set; } = string.Empty;

    [JsonPropertyName("parts")]
    public GeminiPartDto[]? parts { get; set; }
}

internal sealed class GeminiPartDto
{
    [JsonPropertyName("text")]
    public string? text { get; set; }
}

internal sealed class GeminiConfigDto
{
    [JsonPropertyName("temperature")]
    public double temperature { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int maxOutputTokens { get; set; }
}

internal sealed class GeminiResponseDto
{
    [JsonPropertyName("candidates")]
    public GeminiCandidateDto[]? candidates { get; set; }
}

internal sealed class GeminiCandidateDto
{
    [JsonPropertyName("content")]
    public GeminiContentDto? content { get; set; }
}