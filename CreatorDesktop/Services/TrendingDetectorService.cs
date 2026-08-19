using System.Diagnostics;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Polls YouTube's German Trending feed via yt-dlp (no API key, no quota)
/// and matches video titles against the user's topic keywords. Returns
/// trending entries that overlap with the channel's themes.
/// </summary>
public class TrendingDetectorService
{
    public record TrendingHit(string VideoId, string Title, string ChannelName, long Views, string MatchedKeyword);

    public async Task<List<TrendingHit>> FindMatchesAsync(
        List<string> keywords, IProgress<string>? log, CancellationToken ct)
    {
        if (keywords.Count == 0) return [];

        var ytDlp = await YtDlpDownloader.EnsureAsync(log, ct);

        // German trending feed — flat-playlist lists 50+ videos cheaply.
        var args = "--flat-playlist --dump-json --no-warnings " +
                   YtDlpDownloader.CookieArg() +
                   "--extractor-args \"youtubetab:approximate_date\" " +
                   "\"https://www.youtube.com/feed/trending?gl=DE&hl=de\"";

        var (trendLines, _, _) = await YtDlpDownloader.RunYtDlpAsync(ytDlp, args, log, ct);

        var trending = new List<(string id, string title, string channel, long views)>();
        foreach (var line in trendLines)
        {
            if (string.IsNullOrWhiteSpace(line) || line[0] != '{') continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                var title = root.TryGetProperty("title", out var tEl) ? tEl.GetString() ?? "" : "";
                var channel = root.TryGetProperty("uploader", out var uEl) ? uEl.GetString() ?? "" : "";
                long views = root.TryGetProperty("view_count", out var vEl) && vEl.ValueKind == JsonValueKind.Number
                    ? vEl.GetInt64() : 0;
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(title))
                    trending.Add((id, title, channel, views));
            }
            catch { /* skip malformed lines */ }
        }

        var hits = new List<TrendingHit>();
        foreach (var (id, title, channel, views) in trending)
        {
            var match = keywords.FirstOrDefault(k =>
                !string.IsNullOrWhiteSpace(k) &&
                title.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (match == null) continue;
            hits.Add(new TrendingHit(id, title, channel, views, match));
        }
        return hits;
    }
}
