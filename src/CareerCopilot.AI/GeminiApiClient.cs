using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerCopilot.AI;

/// <summary>
/// Thin HTTP client for the Google Gemini generateContent REST API.
/// Returns null when the provider is unavailable, unconfigured or unparseable,
/// so callers always degrade to deterministic behavior.
/// </summary>
public sealed class GeminiApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GeminiOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiApiClient> _logger;

    public GeminiApiClient(IOptions<GeminiOptions> options, HttpClient httpClient, ILogger<GeminiApiClient> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<string?> GenerateTextAsync(
        string systemInstruction,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        try
        {
            var request = new GeminiRequestDto
            {
                systemInstruction = new GeminiPartDto { text = systemInstruction },
                contents = new[]
                {
                    new GeminiContentDto { role = "user", parts = new[] { new GeminiPartDto { text = userPrompt } } }
                },
                generationConfig = new GeminiConfigDto { temperature = 0.5, maxOutputTokens = 8192 }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent?key={_options.ApiKey}";

            using var response = await _httpClient.PostAsJsonAsync(url, request, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini API returned {Status} {Reason}: {Error}",
                    (int)response.StatusCode, response.ReasonPhrase, Truncate(error, 300));
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<GeminiResponseDto>(cancellationToken);
            var text = body?.candidates
                ?.SelectMany(c => c.content?.parts ?? Array.Empty<GeminiPartDto>())
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.text))
                ?.text;

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Gemini API returned an empty completion.");
                return null;
            }

            return StripCodeFence(text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini API call failed: {Message}", ex.Message);
            return null;
        }
    }

    private static string StripCodeFence(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
            {
                trimmed = trimmed[(firstNewline + 1)..];
            }
            else
            {
                trimmed = trimmed[3..];
            }

            trimmed = trimmed.TrimEnd('`').TrimEnd();
        }

        return trimmed;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "...";
}