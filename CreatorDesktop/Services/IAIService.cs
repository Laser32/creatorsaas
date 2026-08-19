using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

public interface IAIService
{
    Task<VideoJob> GenerateScriptAsync(string topic, string style, string language,
        int targetSeconds, IProgress<string>? log, CancellationToken ct);

    Task<List<string>> GenerateTopicSuggestionsAsync(string channelTheme, int count,
        string language, IProgress<string>? log, CancellationToken ct);

    /// <summary>
    /// Low-level: send a prompt and get raw text back. Used by the documentary
    /// pipeline (outline + per-chapter narration) and by the methods above.
    /// </summary>
    Task<string> GenerateTextAsync(string prompt, int maxTokens, CancellationToken ct);
}

public static class AIServiceFactory
{
    public static IAIService Create(AppSettings settings) =>
        settings.AiProvider?.ToLowerInvariant() switch
        {
            "openai" => new OpenAIService(settings),
            "gemini" => new GeminiService(settings),
            _        => new ClaudeService(settings)
        };
}
