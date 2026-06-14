using System.Text.Json;
using CreatorSaaS.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreatorSaaS.Infrastructure.Services.AI;

public class AIScriptService : IAIScriptService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<AIScriptService> _logger;
    private readonly string _provider;

    public AIScriptService(HttpClient httpClient, IConfiguration config, ILogger<AIScriptService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _provider = config["AI:Provider"] ?? "openai";
    }

    public async Task<ScriptResult> GenerateScriptAsync(string topic, string style, string language,
        string? keywords, int targetDurationSeconds, CancellationToken ct = default)
    {
        return _provider.ToLower() == "anthropic"
            ? await GenerateWithAnthropicAsync(topic, style, language, keywords, targetDurationSeconds, ct)
            : await GenerateWithOpenAIAsync(topic, style, language, keywords, targetDurationSeconds, ct);
    }

    public async Task<TitleResult> GenerateTitleAndDescriptionAsync(string scriptContent, string topic,
        CancellationToken ct = default)
    {
        var prompt = $"""
            Based on the following video script, generate:
            1. A catchy YouTube title (max 60 chars)
            2. An engaging description (150-250 chars)
            3. 5 relevant tags

            Topic: {topic}

            Script: {scriptContent}

            Return as JSON:
            {{"title": "...", "description": "...", "tags": ["tag1", "tag2", ...]}}
            """;

        return _provider.ToLower() == "anthropic"
            ? await CallAnthropicAsync(prompt, ct)
            : await CallOpenAIAsync(prompt, ct);
    }

    private async Task<ScriptResult> GenerateWithOpenAIAsync(string topic, string style, string language,
        string? keywords, int targetDurationSeconds, CancellationToken ct)
    {
        var prompt = GenerateScriptPrompt(topic, style, language, keywords, targetDurationSeconds);

        var request = new
        {
            model = _config["OpenAI:Model"] ?? "gpt-4o",
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.7,
            max_tokens = int.Parse(_config["OpenAI:MaxTokens"] ?? "4096")
        };

        var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", _config["OpenAI:ApiKey"]);

        try
        {
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, ct);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var scriptJson = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return ParseScriptJson(scriptJson ?? "{}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI script generation failed");
            throw new ExternalServiceException("OpenAI", ex.Message);
        }
    }

    private async Task<ScriptResult> GenerateWithAnthropicAsync(string topic, string style, string language,
        string? keywords, int targetDurationSeconds, CancellationToken ct)
    {
        var prompt = GenerateScriptPrompt(topic, style, language, keywords, targetDurationSeconds);

        var request = new
        {
            model = _config["Anthropic:Model"] ?? "claude-opus-4-6",
            max_tokens = int.Parse(_config["Anthropic:MaxTokens"] ?? "4096"),
            messages = new[] { new { role = "user", content = prompt } }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", _config["Anthropic:ApiKey"]);

        try
        {
            var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content, ct);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var scriptJson = jsonResponse.GetProperty("content")[0].GetProperty("text").GetString();

            return ParseScriptJson(scriptJson ?? "{}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic script generation failed");
            throw new ExternalServiceException("Anthropic", ex.Message);
        }
    }

    private async Task<TitleResult> CallOpenAIAsync(string prompt, CancellationToken ct)
    {
        var request = new
        {
            model = _config["OpenAI:Model"] ?? "gpt-4o",
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.7
        };

        var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", _config["OpenAI:ApiKey"]);

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var resultJson = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        return ParseTitleJson(resultJson ?? "{}");
    }

    private async Task<TitleResult> CallAnthropicAsync(string prompt, CancellationToken ct)
    {
        var request = new
        {
            model = _config["Anthropic:Model"] ?? "claude-opus-4-6",
            max_tokens = 1000,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", _config["Anthropic:ApiKey"]);

        var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var resultJson = jsonResponse.GetProperty("content")[0].GetProperty("text").GetString();

        return ParseTitleJson(resultJson ?? "{}");
    }

    private string GenerateScriptPrompt(string topic, string style, string language, string? keywords, int targetDurationSeconds)
        => $"""
        Create a compelling video script for a {style} video about: {topic}
        Language: {language}
        Target Duration: {targetDurationSeconds} seconds (~{targetDurationSeconds / 60} minutes)
        {(string.IsNullOrEmpty(keywords) ? "" : $"Keywords: {keywords}")}

        Return a JSON object with this structure:
        {{
            "title": "Video Title",
            "scenes": [
                {{
                    "order": 1,
                    "title": "Scene Title",
                    "narration": "Spoken script text...",
                    "brollQuery": "Search term for B-Roll video",
                    "durationSeconds": 30
                }}
            ]
        }}

        Make it engaging, professional, and optimized for {style} format.
        """;

    private ScriptResult ParseScriptJson(string json)
    {
        try
        {
            // Extract JSON from markdown code blocks if present
            var cleanJson = json;
            if (json.Contains("```json"))
                cleanJson = json.Split("```json")[1].Split("```")[0].Trim();
            else if (json.Contains("```"))
                cleanJson = json.Split("```")[1].Split("```")[0].Trim();

            var doc = JsonSerializer.Deserialize<JsonElement>(cleanJson);
            var title = doc.GetProperty("title").GetString() ?? "Untitled";
            var scenesArray = doc.GetProperty("scenes").EnumerateArray().ToList();

            var scenes = scenesArray.Select(s => new SceneData(
                s.GetProperty("order").GetInt32(),
                s.GetProperty("title").GetString() ?? "",
                s.GetProperty("narration").GetString() ?? "",
                s.GetProperty("brollQuery").GetString() ?? "",
                s.GetProperty("durationSeconds").GetInt32()
            )).OrderBy(x => x.Order).ToList();

            return new ScriptResult(title, json, scenes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse script JSON");
            throw new ExternalServiceException("Script Parser", "Could not parse AI response");
        }
    }

    private TitleResult ParseTitleJson(string json)
    {
        try
        {
            var cleanJson = json;
            if (json.Contains("```json"))
                cleanJson = json.Split("```json")[1].Split("```")[0].Trim();
            else if (json.Contains("```"))
                cleanJson = json.Split("```")[1].Split("```")[0].Trim();

            var doc = JsonSerializer.Deserialize<JsonElement>(cleanJson);
            var title = doc.GetProperty("title").GetString() ?? "";
            var desc = doc.GetProperty("description").GetString() ?? "";
            var tags = doc.GetProperty("tags").EnumerateArray()
                .Select(t => t.GetString() ?? "")
                .ToList();

            return new TitleResult(title, desc, tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse title JSON");
            throw new ExternalServiceException("Title Parser", "Could not parse AI response");
        }
    }
}
