using System.Net.Http;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Pulls per-video statistics (views, likes, comments, watch-time, CTR)
/// for the user's own channel via YouTube Data API + Analytics API.
/// </summary>
public class YouTubeStatsService
{
    public record VideoStats(string VideoId, string Title, DateTime PublishedAt,
        long Views, long Likes, long Comments, double? CtrPct, double? AvgViewSecs);

    private readonly YouTubeService _yt;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly AppSettings _settings;

    public YouTubeStatsService(YouTubeService yt, AppSettings settings)
    {
        _yt = yt;
        _settings = settings;
    }

    /// <summary>
    /// Returns the most recent N uploaded videos with stats.
    /// CTR + average view duration come from the Analytics API and may be null
    /// for very new videos or if the channel lacks Analytics access.
    /// </summary>
    public async Task<List<VideoStats>> GetRecentAsync(int max, IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_settings.YouTubeRefreshToken))
            throw new InvalidOperationException("YouTube nicht verbunden — bitte zuerst in Settings.");

        var token = await _yt.EnsureAccessTokenAsync(log, ct);
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 1. Get the channel's uploads playlist
        var chJson = await _http.GetStringAsync(
            "https://www.googleapis.com/youtube/v3/channels?part=contentDetails&mine=true", ct);
        using var chDoc = JsonDocument.Parse(chJson);
        var uploadsId = chDoc.RootElement.GetProperty("items")[0]
            .GetProperty("contentDetails").GetProperty("relatedPlaylists")
            .GetProperty("uploads").GetString();

        // 2. Get recent video IDs
        var plJson = await _http.GetStringAsync(
            $"https://www.googleapis.com/youtube/v3/playlistItems?part=snippet" +
            $"&playlistId={uploadsId}&maxResults={Math.Clamp(max, 1, 50)}", ct);
        using var plDoc = JsonDocument.Parse(plJson);
        var ids = new List<string>();
        var titles = new Dictionary<string, string>();
        var dates = new Dictionary<string, DateTime>();
        foreach (var item in plDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            var sn = item.GetProperty("snippet");
            var vid = sn.GetProperty("resourceId").GetProperty("videoId").GetString() ?? "";
            ids.Add(vid);
            titles[vid] = sn.GetProperty("title").GetString() ?? "";
            dates[vid] = sn.GetProperty("publishedAt").GetDateTime();
        }
        if (ids.Count == 0) return new();

        // 3. Per-video stats (views/likes/comments)
        var statsJson = await _http.GetStringAsync(
            $"https://www.googleapis.com/youtube/v3/videos?part=statistics" +
            $"&id={string.Join(",", ids)}", ct);
        using var statsDoc = JsonDocument.Parse(statsJson);
        var statsById = new Dictionary<string, (long v, long l, long c)>();
        foreach (var v in statsDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            var s = v.GetProperty("statistics");
            statsById[v.GetProperty("id").GetString() ?? ""] = (
                ParseLong(s, "viewCount"),
                ParseLong(s, "likeCount"),
                ParseLong(s, "commentCount"));
        }

        // 4. Analytics — CTR + avg view duration
        var analyticsById = new Dictionary<string, (double ctr, double avg)>();
        try
        {
            var since = DateTime.UtcNow.AddDays(-90).ToString("yyyy-MM-dd");
            var until = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var anJson = await _http.GetStringAsync(
                "https://youtubeanalytics.googleapis.com/v2/reports" +
                "?ids=channel==MINE" +
                $"&startDate={since}&endDate={until}" +
                "&metrics=cardImpressionsClickThroughRate,averageViewDuration" +
                "&dimensions=video" +
                $"&filters=video=={string.Join(",", ids)}", ct);
            using var anDoc = JsonDocument.Parse(anJson);
            if (anDoc.RootElement.TryGetProperty("rows", out var rows))
            {
                foreach (var row in rows.EnumerateArray())
                {
                    var vid = row[0].GetString() ?? "";
                    var ctr = row[1].GetDouble() * 100;
                    var avg = row[2].GetDouble();
                    analyticsById[vid] = (ctr, avg);
                }
            }
        }
        catch (Exception ex) { log?.Report($"  Analytics teilweise nicht verfügbar: {ex.Message}"); }

        var results = new List<VideoStats>();
        foreach (var vid in ids)
        {
            var (v, l, c) = statsById.TryGetValue(vid, out var sv) ? sv : (0, 0, 0);
            var (ctr, avg) = analyticsById.TryGetValue(vid, out var an) ? an : (double.NaN, double.NaN);
            results.Add(new VideoStats(
                vid, titles.GetValueOrDefault(vid, vid),
                dates.GetValueOrDefault(vid, DateTime.MinValue),
                v, l, c,
                double.IsNaN(ctr) ? null : ctr,
                double.IsNaN(avg) ? null : avg));
        }
        return results;
    }

    private static long ParseLong(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && long.TryParse(p.GetString(), out var n) ? n : 0;
}
