using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

public class PexelsService
{
    private readonly HttpClient _http;
    private readonly AppSettings _settings;

    public PexelsService(AppSettings settings)
    {
        _settings = settings;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public async Task DownloadBRollAsync(Scene scene, string outputPath, IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.PexelsApiKey))
            throw new InvalidOperationException("Pexels API Key fehlt — bitte unter Settings eintragen.");

        log?.Report($"B-Roll Suche '{scene.BRollQuery}'...");

        // Fetch a larger pool so we can prefer the longest landscape HD clip — important
        // for documentary chapters where one clip is looped over several minutes of audio.
        var searchUrl = $"https://api.pexels.com/videos/search?query={Uri.EscapeDataString(scene.BRollQuery)}&per_page=15&orientation=landscape";
        using var req = new HttpRequestMessage(HttpMethod.Get, searchUrl);
        req.Headers.Add("Authorization", _settings.PexelsApiKey);

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Pexels Fehler {(int)resp.StatusCode}: {err}");
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var videos = doc.RootElement.GetProperty("videos");

        if (videos.GetArrayLength() == 0)
            throw new InvalidOperationException($"Keine Pexels-Videos fuer Query: {scene.BRollQuery}");

        // Prefer the longest video — keeps loop seams to a minimum for chapter renders.
        var bestVideo = videos[0];
        int bestDur = bestVideo.TryGetProperty("duration", out var d0) ? d0.GetInt32() : 0;
        foreach (var v in videos.EnumerateArray())
        {
            if (v.TryGetProperty("duration", out var dur) && dur.GetInt32() > bestDur)
            {
                bestDur = dur.GetInt32();
                bestVideo = v;
            }
        }
        var files = bestVideo.GetProperty("video_files").EnumerateArray().ToList();

        JsonElement chosen = default;
        foreach (var f in files)
            if (f.GetProperty("quality").GetString() == "hd")
            {
                chosen = f;
                break;
            }
        if (chosen.ValueKind == JsonValueKind.Undefined)
            chosen = files[0];

        var videoUrl = chosen.GetProperty("link").GetString()
            ?? throw new InvalidOperationException("Pexels: keine Video-URL");

        log?.Report($"Download B-Roll Szene {scene.Order}...");
        var videoData = await _http.GetByteArrayAsync(videoUrl, ct);
        await File.WriteAllBytesAsync(outputPath, videoData, ct);
        scene.BRollPath = outputPath;
    }
}
