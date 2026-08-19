using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Generates click-worthy YouTube thumbnails via OpenAI DALL-E 3.
/// Returns a list of local file paths the user can choose from.
/// Falls back gracefully if no OpenAI key is configured.
/// </summary>
public class AiThumbnailService
{
    private readonly AppSettings _settings;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(3) };

    public AiThumbnailService(AppSettings settings)
    {
        _settings = settings;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.OpenAiApiKey);
    }

    public async Task<string> GenerateAsync(string title, string topic, string outputFolder,
        IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.OpenAiApiKey))
            throw new InvalidOperationException(
                "OpenAI API Key fehlt — bitte in Settings eintragen (DALL-E 3 nutzt diesen Key).");

        Directory.CreateDirectory(outputFolder);

        var prompt =
            $"YouTube thumbnail for a documentary video titled \"{title}\". Topic: {topic}. " +
            "Cinematic, dramatic lighting, photorealistic, high contrast, vivid colors, " +
            "rule of thirds composition, no text or letters anywhere in the image. " +
            "Movie poster quality, eye-catching, professional documentary aesthetic, " +
            "16:9 widescreen format, 1792x1024 resolution.";

        log?.Report("  KI generiert Thumbnail (DALL-E 3)...");
        var resp = await _http.PostAsJsonAsync(
            "https://api.openai.com/v1/images/generations",
            new
            {
                model = "dall-e-3",
                prompt,
                n = 1,
                size = "1792x1024",
                quality = "hd"
            }, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"DALL-E Fehler ({resp.StatusCode}): {err}");
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var imgUrl = doc.RootElement.GetProperty("data")[0].GetProperty("url").GetString();
        if (string.IsNullOrEmpty(imgUrl))
            throw new InvalidOperationException("DALL-E lieferte keine Bild-URL.");

        log?.Report("  Lade Bild herunter...");
        var bytes = await _http.GetByteArrayAsync(imgUrl, ct);
        var safe = string.Concat(title.Take(40).Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        var path = Path.Combine(outputFolder, $"ai_thumb_{safe}_{DateTime.Now:HHmmss}.png");
        await File.WriteAllBytesAsync(path, bytes, ct);
        log?.Report($"  Thumbnail gespeichert: {Path.GetFileName(path)}");
        return path;
    }
}
