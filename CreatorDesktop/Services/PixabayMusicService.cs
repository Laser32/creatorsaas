using System.Net.Http;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Searches and downloads free background music from Pixabay (and Free Music Archive
/// via mirror). Pixabay's content licence allows commercial use including YouTube
/// monetization with NO attribution required. Same key as the Pixabay image API
/// works — the user has it from Pexels equivalent or registers free.
/// </summary>
public class PixabayMusicService
{
    public record MusicTrack(int Id, string Title, string Artist, int DurationSec,
        string PreviewUrl, string DownloadUrl, List<string> Tags);

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(2) };
    private readonly AppSettings _settings;

    public PixabayMusicService(AppSettings settings)
    {
        _settings = settings;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("CreatorDesktop");
    }

    public async Task<List<MusicTrack>> SearchAsync(string query, CancellationToken ct)
    {
        var results = new List<MusicTrack>();

        // Pixabay Music — needs a free key
        if (!string.IsNullOrWhiteSpace(_settings.PexelsApiKey) ||
            !string.IsNullOrWhiteSpace(_settings.PixabayMusicKey))
        {
            var key = !string.IsNullOrWhiteSpace(_settings.PixabayMusicKey)
                ? _settings.PixabayMusicKey : _settings.PexelsApiKey;
            try
            {
                var url = $"https://pixabay.com/api/music/?key={key}" +
                          $"&q={Uri.EscapeDataString(query)}&per_page=50";
                var json = await _http.GetStringAsync(url, ct);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("hits", out var hits))
                {
                    foreach (var t in hits.EnumerateArray())
                    {
                        results.Add(new MusicTrack(
                            t.GetProperty("id").GetInt32(),
                            t.GetProperty("title").GetString() ?? "",
                            t.GetProperty("user").GetString() ?? "",
                            t.TryGetProperty("duration", out var d) ? d.GetInt32() : 0,
                            t.TryGetProperty("preview_url", out var pr) ? pr.GetString() ?? "" : "",
                            t.TryGetProperty("audio", out var au) ? au.GetString() ?? "" : "",
                            t.TryGetProperty("tags", out var tg)
                                ? (tg.GetString() ?? "").Split(',', StringSplitOptions.TrimEntries).ToList()
                                : new()));
                    }
                }
            }
            catch { /* network/json error — return what we have */ }
        }

        // FMA via archive.org mirror (no key, free CC music)
        if (results.Count == 0)
        {
            try
            {
                var url = $"https://archive.org/advancedsearch.php?q=" +
                          Uri.EscapeDataString($"mediatype:audio AND ({query})") +
                          "&fl[]=identifier&fl[]=title&fl[]=creator&fl[]=runtime" +
                          "&rows=50&page=1&output=json&sort[]=downloads+desc";
                var json = await _http.GetStringAsync(url, ct);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("response", out var r) &&
                    r.TryGetProperty("docs", out var docs))
                {
                    foreach (var t in docs.EnumerateArray())
                    {
                        var id = t.GetProperty("identifier").GetString() ?? "";
                        var title = t.TryGetProperty("title", out var tt) ? tt.GetString() ?? id : id;
                        var creator = t.TryGetProperty("creator", out var c) ? c.GetString() ?? "" : "";
                        results.Add(new MusicTrack(0, title, creator, 0,
                            $"https://archive.org/details/{id}",
                            $"https://archive.org/download/{id}/{id}.mp3",
                            new() { "archive.org" }));
                    }
                }
            }
            catch { }
        }

        return results;
    }

    public async Task<string> DownloadAsync(MusicTrack track, string outputFolder, CancellationToken ct)
    {
        Directory.CreateDirectory(outputFolder);
        var safe = string.Concat(track.Title.Take(40).Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        var ext = track.DownloadUrl.Contains(".wav") ? ".wav" : ".mp3";
        var path = Path.Combine(outputFolder, $"music_{safe}{ext}");
        var bytes = await _http.GetByteArrayAsync(track.DownloadUrl, ct);
        await File.WriteAllBytesAsync(path, bytes, ct);
        return path;
    }
}
