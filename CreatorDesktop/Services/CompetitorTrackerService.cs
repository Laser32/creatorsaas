using System.Xml.Linq;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Watches competitor YouTube channels via their public RSS feeds —
/// no API key, no quota, unlimited polling. Each channel exposes its last 15
/// uploads at https://www.youtube.com/feeds/videos.xml?channel_id=&lt;id&gt;.
/// </summary>
public class CompetitorTrackerService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    /// <summary>
    /// Checks license + region restrictions for a video via yt-dlp.
    /// Returns:
    /// • CreativeCommons   → license="creative_commons" or text matches  → safe to re-upload
    /// • RegionRestricted  → blocked in some countries — colour yellow
    /// • Blocked           → not playable for us at all
    /// • StandardYouTube   → playable but copyrighted → download possible, re-upload risky
    /// </summary>
    public async Task<VideoCheckResult> CheckVideoAsync(string videoId, IProgress<string>? log, CancellationToken ct)
    {
        var ytDlp = await YtDlpDownloader.EnsureAsync(log, ct);
        var url = $"https://www.youtube.com/watch?v={videoId}";
        var args = $"--dump-single-json --skip-download --no-warnings --no-playlist " +
                   YtDlpDownloader.CookieArg() +
                   $"\"{url}\"";

        var (checkLines, stderr, checkExit) = await YtDlpDownloader.RunYtDlpAsync(ytDlp, args, log, ct);
        var stdout = string.Join("\n", checkLines);

        if (checkExit != 0)
        {
            var lower = stderr.ToLowerInvariant();
            if (lower.Contains("blocked") || lower.Contains("not available") || lower.Contains("private"))
                return new VideoCheckResult(videoId, LicenseStatus.Blocked, "", "nicht abspielbar", false);
            return new VideoCheckResult(videoId, LicenseStatus.Unknown, "", $"yt-dlp Fehler", false);
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(stdout);
            var root = doc.RootElement;
            var license = root.TryGetProperty("license", out var lEl) ? lEl.GetString() ?? "" : "";

            // Region info — yt-dlp surfaces region restrictions via `availability` and `geo_blocked` / `not_available_in`.
            var availability = root.TryGetProperty("availability", out var aEl) ? aEl.GetString() ?? "" : "";
            var allowedRegions = new HashSet<string>();
            var blockedRegions = new HashSet<string>();
            if (root.TryGetProperty("__has_drm", out var drm) && drm.ValueKind == System.Text.Json.JsonValueKind.True)
                return new VideoCheckResult(videoId, LicenseStatus.Blocked, license, "DRM-geschützt", false);

            // YouTube returns region info inside the country list — yt-dlp puts blocked list into "_available_countries"
            if (root.TryGetProperty("_available_countries", out var rcEl) && rcEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                foreach (var c in rcEl.EnumerateArray()) allowedRegions.Add(c.GetString() ?? "");

            bool allowedInDe = allowedRegions.Count == 0 || allowedRegions.Contains("DE");
            bool regionLocked = allowedRegions.Count > 0 && allowedRegions.Count < 50;

            // Treat "needs_auth", "subscriber_only", "premium_only" etc. as blocked
            if (availability == "needs_auth" || availability == "subscriber_only" ||
                availability == "premium_only" || availability == "private")
                return new VideoCheckResult(videoId, LicenseStatus.Blocked, license, availability, allowedInDe);

            var isCc = license.Contains("creative commons", StringComparison.OrdinalIgnoreCase)
                       || license.Equals("creative_commons", StringComparison.OrdinalIgnoreCase);

            if (isCc && !regionLocked)
                return new VideoCheckResult(videoId, LicenseStatus.CreativeCommons, license, "CC-BY", true);

            if (regionLocked && allowedInDe)
                return new VideoCheckResult(videoId, LicenseStatus.RegionRestricted, license,
                    $"nur in DE/{allowedRegions.Count} Ländern", true);

            if (regionLocked && !allowedInDe)
                return new VideoCheckResult(videoId, LicenseStatus.Blocked, license,
                    "in DE gesperrt", false);

            return new VideoCheckResult(videoId, LicenseStatus.StandardYouTube, license, "Standard-Lizenz", true);
        }
        catch
        {
            return new VideoCheckResult(videoId, LicenseStatus.Unknown, "", "JSON-Fehler", false);
        }
    }

    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace YtNs = "http://www.youtube.com/xml/schemas/2015";
    private static readonly XNamespace Media = "http://search.yahoo.com/mrss/";

    public record CompetitorVideo(
        string ChannelName,
        string ChannelId,
        string VideoId,
        string Title,
        string Description,
        DateTime Published);

    public enum LicenseStatus
    {
        Unknown,
        CreativeCommons,     // green  — re-uploadable
        StandardYouTube,     // yellow — viewable, not re-uploadable
        RegionRestricted,    // yellow — allowed in some countries, blocked in others
        Blocked              // red    — completely unavailable
    }

    public record VideoCheckResult(string VideoId, LicenseStatus Status, string License, string Reason, bool AllowedInGermany);

    public async Task<List<CompetitorVideo>> FetchChannelAsync(string channelId, CancellationToken ct)
    {
        var url = $"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}";
        var xml = await _http.GetStringAsync(url, ct);
        var doc = XDocument.Parse(xml);

        var channelName = doc.Root?.Element(Atom + "title")?.Value ?? channelId;
        var entries = doc.Root?.Elements(Atom + "entry") ?? Enumerable.Empty<XElement>();

        var list = new List<CompetitorVideo>();
        foreach (var entry in entries)
        {
            var videoId = entry.Element(YtNs + "videoId")?.Value ?? "";
            var title = entry.Element(Atom + "title")?.Value ?? "";
            var publishedStr = entry.Element(Atom + "published")?.Value ?? "";
            var group = entry.Element(Media + "group");
            var description = group?.Element(Media + "description")?.Value ?? "";
            DateTime.TryParse(publishedStr, out var published);
            list.Add(new CompetitorVideo(channelName, channelId, videoId, title, description, published));
        }
        return list;
    }

    /// <summary>
    /// Fetches recent videos from every competitor channel, dedupes by VideoId,
    /// sorts newest first.
    /// </summary>
    public async Task<List<CompetitorVideo>> FetchAllAsync(List<CompetitorChannel> channels,
        IProgress<string>? log, CancellationToken ct)
    {
        var results = new List<CompetitorVideo>();
        foreach (var ch in channels)
        {
            if (string.IsNullOrWhiteSpace(ch.ChannelId)) continue;
            try
            {
                log?.Report($"  Hole RSS von {ch.Name}...");
                var vids = await FetchChannelAsync(ch.ChannelId, ct);
                results.AddRange(vids);
            }
            catch (Exception ex)
            {
                log?.Report($"  Fehler {ch.Name}: {ex.Message}");
            }
        }
        return results.OrderByDescending(v => v.Published).ToList();
    }

    /// <summary>
    /// Tries to resolve a YouTube URL (handle, @-name, /c/, /channel/, etc.) to a
    /// raw channel ID (UCxxx). For /channel/UC... URLs this is instant; for others
    /// we scrape the page once and look for the canonical channelId.
    /// </summary>
    public async Task<string?> ResolveChannelIdAsync(string urlOrId, CancellationToken ct)
    {
        var s = urlOrId.Trim();
        if (s.StartsWith("UC") && s.Length >= 20) return s;

        // /channel/UCxxx URL
        var marker = "/channel/";
        var idx = s.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var id = s[(idx + marker.Length)..].Split('/', '?', '&')[0];
            if (id.StartsWith("UC")) return id;
        }

        // Otherwise scrape the page and look for "channelId":"UC..."
        if (!s.StartsWith("http")) s = "https://www.youtube.com/" + s.TrimStart('/');
        var html = await _http.GetStringAsync(s, ct);
        var key = "\"channelId\":\"";
        var p = html.IndexOf(key, StringComparison.Ordinal);
        if (p < 0) return null;
        var start = p + key.Length;
        var end = html.IndexOf('"', start);
        return end > start ? html[start..end] : null;
    }
}
