using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CareerCopilot.IntegrationTests;

public sealed record ApiEnvelope<T>(bool Success, T Data);

public sealed record ApiErrorEnvelope(bool Success, string Message, string ErrorCode);

public sealed class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public string? Token
    {
        get => _http.DefaultRequestHeaders.Authorization?.Parameter;
        set => _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(value)
                ? null
                : new AuthenticationHeaderValue("Bearer", value);
    }

    public async Task<(HttpStatusCode Status, ApiEnvelope<T>? Envelope)> GetAsync<T>(string url, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(url, ct);
        return await ReadEnvelopeAsync<T>(response, ct);
    }

    public async Task<(HttpStatusCode Status, ApiEnvelope<T>? Envelope)> PostAsync<T>(
        string url, object? body = null, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(url, body, JsonOptions, ct);
        return await ReadEnvelopeAsync<T>(response, ct);
    }

    public async Task<(HttpStatusCode Status, ApiEnvelope<T>? Envelope)> PutAsync<T>(
        string url, object? body = null, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(url, body, JsonOptions, ct);
        return await ReadEnvelopeAsync<T>(response, ct);
    }

    public async Task<(HttpStatusCode Status, ApiEnvelope<T>? Envelope)> DeleteAsync<T>(string url, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync(url, ct);
        return await ReadEnvelopeAsync<T>(response, ct);
    }

    public async Task<(HttpStatusCode Status, string? Text)> GetRawAsync(string url, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(url, ct);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<(HttpStatusCode Status, string? Text)> PostFileAsync(
        string url,
        string fileName,
        string contentType,
        byte[] content,
        bool setDefault = false,
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent(setDefault ? "true" : "false"), "setDefault");

        using var response = await _http.PostAsync(url, form, ct);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<(HttpStatusCode Status, string? Text)> PostTextAsync(string url, string json, CancellationToken ct = default)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(url, content, ct);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    private async Task<(HttpStatusCode, ApiEnvelope<T>?)> ReadEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, null);
        }

        var envelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(json, JsonOptions);
        return (response.StatusCode, envelope);
    }
}