using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Researches a topic using the YouTube Data API:
///   - Fetches video titles + descriptions (always works, no caption required)
///   - Tries to add full transcripts on top via the timedtext API (best-effort)
/// </summary>
public class YouTubeResearchService
{
    private readonly HttpClient _http;
    private readonly AppSettings _settings;

    public YouTubeResearchService(AppSettings settings)
    {
        _settings = settings;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) CreatorDesktop");
    }

    public async Task<string> ResearchAsync(string topic, string language, int maxVideos,
        IProgress<string>? log, CancellationToken ct)
    {
        log?.Report($"Recherche: YouTube-Suche zu '{topic}'...");

        if (string.IsNullOrWhiteSpace(_settings.YouTubeRefreshToken))
        {
            log?.Report("  YouTube nicht verbunden — fahre ohne Recherche fort.");
            return "";
        }

        var yt = new YouTubeService(_settings);
        string accessToken;
        try { accessToken = await yt.EnsureAccessTokenAsync(null, ct); }
        catch (Exception ex)
        {
            log?.Report($"  YouTube Token-Fehler: {ex.Message}");
            return "";
        }

        // ── Step 1: Search + fetch video details (title + description) ───────
        var videoDetails = await FetchVideoDetailsAsync(topic, maxVideos, accessToken, ct);
        log?.Report($"  {videoDetails.Count} Videos gefunden.");

        if (videoDetails.Count == 0)
        {
            log?.Report("  Keine Videos — fahre ohne Recherche fort.");
            return "";
        }

        var combined = new StringBuilder();
        int withTranscript = 0;

        foreach (var v in videoDetails)
        {
            ct.ThrowIfCancellationRequested();

            // Always add title + description as baseline material
            combined.AppendLine($"=== {v.title} ===");
            if (!string.IsNullOrWhiteSpace(v.description))
                combined.AppendLine(v.description.Trim());

            // Try to enrich with full transcript (best-effort, silent on failure)
            try
            {
                var (transcript, lang) = await GetTranscriptAsync(v.id, language, ct);
                if (!string.IsNullOrWhiteSpace(transcript))
                {
                    combined.AppendLine("[Transcript:]");
                    combined.AppendLine(transcript);
                    withTranscript++;
                    log?.Report($"  + {v.id}: Transcript ({lang}, {transcript.Length} Z).");
                }
                else
                {
                    log?.Report($"  + {v.id}: Beschreibung geladen (kein Transcript).");
                }
            }
            catch
            {
                log?.Report($"  + {v.id}: Beschreibung geladen.");
            }
            combined.AppendLine();
        }

        var result = combined.ToString();
        const int maxChars = 80_000;
        if (result.Length > maxChars)
            result = result[..maxChars] + "\n...(gekuerzt)";

        log?.Report($"Recherche fertig: {videoDetails.Count} Videos, " +
                    $"{withTranscript} mit Transcript, {result.Length} Zeichen Material.");
        return result;
    }

    /// <summary>
    /// Searches YouTube and fetches snippet (title + description) for all results.
    /// Uses the Data API — reliable, no caption scraping required.
    /// </summary>
    private async Task<List<(string id, string title, string description)>> FetchVideoDetailsAsync(
        string query, int maxResults, string accessToken, CancellationToken ct)
    {
        // Search
        var searchUrl = "https://www.googleapis.com/youtube/v3/search" +
                        $"?part=snippet&type=video&maxResults={maxResults}&order=relevance" +
                        $"&q={Uri.EscapeDataString(query)}";
        using var searchReq = new HttpRequestMessage(HttpMethod.Get, searchUrl);
        searchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var searchResp = await _http.SendAsync(searchReq, ct);
        if (!searchResp.IsSuccessStatusCode) return new();

        var searchBody = await searchResp.Content.ReadAsStringAsync(ct);
        using var searchDoc = JsonDocument.Parse(searchBody);
        var ids = new List<string>();
        foreach (var item in searchDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (item.TryGetProperty("id", out var idObj)
                && idObj.TryGetProperty("videoId", out var vid))
            {
                var id = vid.GetString();
                if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }
        }
        if (ids.Count == 0) return new();

        // Fetch full snippets (descriptions can be truncated in search results)
        var detailUrl = "https://www.googleapis.com/youtube/v3/videos" +
                        $"?part=snippet&id={string.Join(",", ids)}";
        using var detailReq = new HttpRequestMessage(HttpMethod.Get, detailUrl);
        detailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var detailResp = await _http.SendAsync(detailReq, ct);
        if (!detailResp.IsSuccessStatusCode) return new();

        var detailBody = await detailResp.Content.ReadAsStringAsync(ct);
        using var detailDoc = JsonDocument.Parse(detailBody);
        var result = new List<(string, string, string)>();
        foreach (var item in detailDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            var id = item.GetProperty("id").GetString() ?? "";
            var snippet = item.GetProperty("snippet");
            var title = snippet.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            var desc = snippet.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
            if (desc.Length > 1000) desc = desc[..1000];  // cap per-video description
            result.Add((id, title, desc));
        }
        return result;
    }

    /// <summary>
    /// Lists Creative Commons YouTube videos for a topic with their durations.
    /// Used by the "CC-only" mode in the pipeline.
    /// </summary>
    public async Task<List<(string id, string title, int durationSeconds)>> ListCCVideosAsync(
        string query, int maxResults, CancellationToken ct)
    {
        var result = new List<(string, string, int)>();
        if (string.IsNullOrWhiteSpace(_settings.YouTubeRefreshToken))
            return result;

        var yt = new YouTubeService(_settings);
        var accessToken = await yt.EnsureAccessTokenAsync(null, ct);

        var searchUrl = "https://www.googleapis.com/youtube/v3/search" +
                        $"?part=snippet&type=video&maxResults={maxResults}&order=relevance" +
                        $"&videoLicense=creativeCommon" +
                        $"&q={Uri.EscapeDataString(query)}";
        using var searchReq = new HttpRequestMessage(HttpMethod.Get, searchUrl);
        searchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var searchResp = await _http.SendAsync(searchReq, ct);
        if (!searchResp.IsSuccessStatusCode) return result;

        var searchBody = await searchResp.Content.ReadAsStringAsync(ct);
        using var searchDoc = JsonDocument.Parse(searchBody);
        var ids = new List<string>();
        foreach (var item in searchDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (item.TryGetProperty("id", out var idObj)
                && idObj.TryGetProperty("videoId", out var vid))
            {
                var id = vid.GetString();
                if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }
        }
        if (ids.Count == 0) return result;

        // Get duration from contentDetails + title from snippet
        var detailUrl = "https://www.googleapis.com/youtube/v3/videos" +
                        $"?part=snippet,contentDetails&id={string.Join(",", ids)}";
        using var detailReq = new HttpRequestMessage(HttpMethod.Get, detailUrl);
        detailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var detailResp = await _http.SendAsync(detailReq, ct);
        if (!detailResp.IsSuccessStatusCode) return result;

        var detailBody = await detailResp.Content.ReadAsStringAsync(ct);
        using var detailDoc = JsonDocument.Parse(detailBody);
        foreach (var item in detailDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            var id = item.GetProperty("id").GetString() ?? "";
            var title = item.GetProperty("snippet").GetProperty("title").GetString() ?? "";
            var iso = item.GetProperty("contentDetails").GetProperty("duration").GetString() ?? "PT0S";
            var seconds = ParseIso8601Duration(iso);
            if (!string.IsNullOrEmpty(id))
                result.Add((id, title, seconds));
        }
        return result;
    }

    /// <summary>Parse YouTube's ISO 8601 duration (PT1H2M3S → 3723).</summary>
    private static int ParseIso8601Duration(string iso)
    {
        var m = Regex.Match(iso, @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?");
        if (!m.Success) return 0;
        int h = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 0;
        int min = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
        int s = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
        return h * 3600 + min * 60 + s;
    }

    /// <summary>
    /// Finds a single Creative Commons YouTube video for a given search query.
    /// Returns null if nothing is found.
    /// </summary>
    public async Task<string?> FindCCVideoAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.YouTubeRefreshToken))
            return null;

        try
        {
            var yt = new YouTubeService(_settings);
            var accessToken = await yt.EnsureAccessTokenAsync(null, ct);

            var url = "https://www.googleapis.com/youtube/v3/search" +
                      $"?part=snippet&type=video&maxResults=5&order=relevance" +
                      $"&videoLicense=creativeCommon" +
                      $"&videoDuration=medium" +
                      $"&q={Uri.EscapeDataString(query)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                if (item.TryGetProperty("id", out var idObj)
                    && idObj.TryGetProperty("videoId", out var vid))
                {
                    var id = vid.GetString();
                    if (!string.IsNullOrEmpty(id)) return id;
                }
            }
        }
        catch { }
        return null;
    }

    // ── Transcript fetching (best-effort) ───────────────────────────────────

    private async Task<(string text, string lang)> GetTranscriptAsync(
        string videoId, string preferredLang, CancellationToken ct)
    {
        var tracks = await GetCaptionTrackListAsync(videoId, ct);
        if (tracks.Count > 0)
        {
            var ordered = tracks.OrderBy(t =>
            {
                if (t.lang == preferredLang && !t.isAsr) return 0;
                if (t.lang == preferredLang) return 1;
                if (t.lang.StartsWith(preferredLang)) return 2;
                if (t.lang == "en" && !t.isAsr) return 3;
                if (t.lang == "en") return 4;
                return 5;
            }).ToList();

            foreach (var track in ordered)
            {
                try
                {
                    var xml = await _http.GetStringAsync(
                        $"https://www.youtube.com/api/timedtext?v={videoId}&lang={track.lang}&name={Uri.EscapeDataString(track.name)}", ct);
                    var text = ParseTimedTextXml(xml);
                    if (!string.IsNullOrWhiteSpace(text))
                        return (text, track.lang);
                }
                catch { }
            }
        }

        return await GetTranscriptFromWatchPageAsync(videoId, preferredLang, ct);
    }

    private async Task<List<(string lang, string name, bool isAsr)>> GetCaptionTrackListAsync(
        string videoId, CancellationToken ct)
    {
        var result = new List<(string, string, bool)>();
        try
        {
            var xml = await _http.GetStringAsync(
                $"https://www.youtube.com/api/timedtext?v={videoId}&type=list", ct);
            if (string.IsNullOrWhiteSpace(xml)) return result;
            var xdoc = new XmlDocument();
            xdoc.LoadXml(xml);
            foreach (XmlNode track in xdoc.GetElementsByTagName("track"))
            {
                var lang = track.Attributes?["lang_code"]?.Value ?? "";
                var name = track.Attributes?["name"]?.Value ?? "";
                var translated = track.Attributes?["lang_translated"]?.Value ?? "";
                bool isAsr = translated.Contains("auto", StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(lang))
                    result.Add((lang, name, isAsr));
            }
        }
        catch { }
        return result;
    }

    private static string ParseTimedTextXml(string xml)
    {
        try
        {
            var xdoc = new XmlDocument();
            xdoc.LoadXml(xml);
            var sb = new StringBuilder();
            foreach (XmlNode node in xdoc.GetElementsByTagName("text"))
            {
                var decoded = WebUtility.HtmlDecode(node.InnerText).Replace("\n", " ").Trim();
                if (decoded.Length > 0) sb.Append(decoded).Append(' ');
            }
            return sb.ToString().Trim();
        }
        catch { return ""; }
    }

    private async Task<(string text, string lang)> GetTranscriptFromWatchPageAsync(
        string videoId, string preferredLang, CancellationToken ct)
    {
        try
        {
            var html = await _http.GetStringAsync(
                $"https://www.youtube.com/watch?v={videoId}&hl={preferredLang}", ct);
            var match = Regex.Match(html, "\"captionTracks\":(\\[.+?\\])", RegexOptions.Singleline);
            if (!match.Success) return ("", "");

            JsonDocument tracks;
            try { tracks = JsonDocument.Parse(match.Groups[1].Value); }
            catch { return ("", ""); }

            using (tracks)
            {
                string? baseUrl = null;
                string foundLang = "";
                foreach (var t in tracks.RootElement.EnumerateArray())
                {
                    if (t.TryGetProperty("languageCode", out var lc) && lc.GetString() == preferredLang
                        && t.TryGetProperty("baseUrl", out var b))
                    { baseUrl = b.GetString(); foundLang = preferredLang; break; }
                }
                if (baseUrl == null)
                    foreach (var t in tracks.RootElement.EnumerateArray())
                    {
                        if (t.TryGetProperty("baseUrl", out var b))
                        {
                            baseUrl = b.GetString();
                            foundLang = t.TryGetProperty("languageCode", out var lc2)
                                ? lc2.GetString() ?? "" : "";
                            break;
                        }
                    }
                if (string.IsNullOrEmpty(baseUrl)) return ("", "");
                var xml = await _http.GetStringAsync(baseUrl, ct);
                return (ParseTimedTextXml(xml), foundLang);
            }
        }
        catch { return ("", ""); }
    }
}
