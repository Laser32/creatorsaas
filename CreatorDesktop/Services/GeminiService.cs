using System.Net.Http.Json;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

public class GeminiService : IAIService
{
    private readonly HttpClient _http;
    private readonly AppSettings _settings;

    public GeminiService(AppSettings settings)
    {
        _settings = settings;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    }

    public async Task<VideoJob> GenerateScriptAsync(string topic, string style, string language,
        int targetSeconds, IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.GeminiApiKey))
            throw new InvalidOperationException("Gemini API Key fehlt — bitte unter Settings eintragen.");

        log?.Report($"Generiere Skript mit Gemini ({_settings.GeminiModel})...");
        var text = await CallAsync(AIPrompts.ScriptPrompt(topic, style, language, targetSeconds), 4096, ct);
        var json = AIPrompts.ExtractJson(text);
        return AIPrompts.ParseScriptJob(json, topic, style, language, targetSeconds);
    }

    public async Task<List<string>> GenerateTopicSuggestionsAsync(string channelTheme, int count,
        string language, IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.GeminiApiKey))
            throw new InvalidOperationException("Gemini API Key fehlt — bitte unter Settings eintragen.");

        log?.Report($"Generiere {count} Themen mit Gemini...");
        var text = await CallAsync(AIPrompts.TopicsPrompt(channelTheme, count, language), 2048, ct);
        var json = AIPrompts.ExtractJson(text);
        return AIPrompts.ParseTopics(json);
    }

    public Task<string> GenerateTextAsync(string prompt, int maxTokens, CancellationToken ct)
        => CallAsync(prompt, maxTokens, ct, jsonMode: false);

    private async Task<string> CallAsync(string prompt, int maxTokens, CancellationToken ct, bool jsonMode = true)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.GeminiModel}:generateContent?key={_settings.GeminiApiKey}";

        HttpResponseMessage resp;
        if (jsonMode)
        {
            var request = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = maxTokens,
                    responseMimeType = "application/json"
                }
            };
            resp = await _http.PostAsJsonAsync(url, request, ct);
        }
        else
        {
            var request = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = maxTokens
                }
            };
            resp = await _http.PostAsJsonAsync(url, request, ct);
        }

        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini API Fehler {(int)resp.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0)
            throw new InvalidOperationException("Gemini lieferte keine Antwort.");
        return candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
    }
}
