using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

public class OpenAIService : IAIService
{
    private readonly HttpClient _http;
    private readonly AppSettings _settings;

    public OpenAIService(AppSettings settings)
    {
        _settings = settings;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    }

    public async Task<VideoJob> GenerateScriptAsync(string topic, string style, string language,
        int targetSeconds, IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.OpenAiApiKey))
            throw new InvalidOperationException("OpenAI API Key fehlt — bitte unter Settings eintragen.");

        log?.Report($"Generiere Skript mit ChatGPT ({_settings.OpenAiModel})...");
        var text = await CallAsync(AIPrompts.ScriptPrompt(topic, style, language, targetSeconds), 4096, ct);
        var json = AIPrompts.ExtractJson(text);
        return AIPrompts.ParseScriptJob(json, topic, style, language, targetSeconds);
    }

    public async Task<List<string>> GenerateTopicSuggestionsAsync(string channelTheme, int count,
        string language, IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.OpenAiApiKey))
            throw new InvalidOperationException("OpenAI API Key fehlt — bitte unter Settings eintragen.");

        log?.Report($"Generiere {count} Themen mit ChatGPT...");
        var text = await CallAsync(AIPrompts.TopicsPrompt(channelTheme, count, language), 2048, ct);
        var json = AIPrompts.ExtractJson(text);
        return AIPrompts.ParseTopics(json);
    }

    public Task<string> GenerateTextAsync(string prompt, int maxTokens, CancellationToken ct)
        => CallAsync(prompt, maxTokens, ct, jsonMode: false);

    private async Task<string> CallAsync(string prompt, int maxTokens, CancellationToken ct, bool jsonMode = true)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.OpenAiApiKey);

        HttpResponseMessage resp;
        if (jsonMode)
        {
            var request = new
            {
                model = _settings.OpenAiModel,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.7,
                max_tokens = maxTokens,
                response_format = new { type = "json_object" }
            };
            resp = await _http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, ct);
        }
        else
        {
            var request = new
            {
                model = _settings.OpenAiModel,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.7,
                max_tokens = maxTokens
            };
            resp = await _http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, ct);
        }

        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI API Fehler {(int)resp.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}
