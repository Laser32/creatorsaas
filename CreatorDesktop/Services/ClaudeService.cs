using System.Net.Http.Json;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

public class ClaudeService : IAIService
{
    private readonly HttpClient _http;
    private readonly AppSettings _settings;

    public ClaudeService(AppSettings settings)
    {
        _settings = settings;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<VideoJob> GenerateScriptAsync(string topic, string style, string language,
        int targetSeconds, IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.AnthropicApiKey))
            throw new InvalidOperationException("Anthropic API Key fehlt — bitte unter Settings eintragen.");

        log?.Report($"Generiere Skript mit Claude ({_settings.AnthropicModel})...");
        var text = await CallAsync(AIPrompts.ScriptPrompt(topic, style, language, targetSeconds), 4096, ct);
        var json = AIPrompts.ExtractJson(text);
        return AIPrompts.ParseScriptJob(json, topic, style, language, targetSeconds);
    }

    public async Task<List<string>> GenerateTopicSuggestionsAsync(string channelTheme, int count,
        string language, IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.AnthropicApiKey))
            throw new InvalidOperationException("Anthropic API Key fehlt — bitte unter Settings eintragen.");

        log?.Report($"Generiere {count} Themen mit Claude...");
        var text = await CallAsync(AIPrompts.TopicsPrompt(channelTheme, count, language), 2048, ct);
        var json = AIPrompts.ExtractJson(text);
        return AIPrompts.ParseTopics(json);
    }

    public Task<string> GenerateTextAsync(string prompt, int maxTokens, CancellationToken ct)
        => CallAsync(prompt, maxTokens, ct);

    private async Task<string> CallAsync(string prompt, int maxTokens, CancellationToken ct)
    {
        var request = new
        {
            model = _settings.AnthropicModel,
            max_tokens = maxTokens,
            messages = new[] { new { role = "user", content = prompt } }
        };

        _http.DefaultRequestHeaders.Remove("x-api-key");
        _http.DefaultRequestHeaders.Add("x-api-key", _settings.AnthropicApiKey);

        var resp = await _http.PostAsJsonAsync("https://api.anthropic.com/v1/messages", request, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Anthropic API Fehler {(int)resp.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }
}
