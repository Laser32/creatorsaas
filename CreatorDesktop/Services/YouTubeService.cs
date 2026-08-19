using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

public class YouTubeService
{
    private readonly HttpClient _http = new();
    private readonly AppSettings _settings;

    public YouTubeService(AppSettings settings) => _settings = settings;

    public async Task<string> EnsureAccessTokenAsync(IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.YouTubeClientId) || string.IsNullOrWhiteSpace(_settings.YouTubeClientSecret))
            throw new InvalidOperationException("YouTube ClientId/Secret fehlt. Settings ausfuellen.");

        if (string.IsNullOrWhiteSpace(_settings.YouTubeRefreshToken))
            return await DoOAuthFlowAsync(log, ct);

        return await RefreshAccessTokenAsync(_settings.YouTubeRefreshToken!, ct);
    }

    private async Task<string> DoOAuthFlowAsync(IProgress<string>? log, CancellationToken ct)
    {
        log?.Report("Starte YouTube OAuth (Browser oeffnet sich)...");

        var listener = new HttpListener();
        var port = GetFreePort();
        var redirectUri = $"http://localhost:{port}/";
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        var scope = Uri.EscapeDataString(
            "https://www.googleapis.com/auth/youtube.upload " +
            "https://www.googleapis.com/auth/youtube " +
            "https://www.googleapis.com/auth/youtube.force-ssl " +
            "https://www.googleapis.com/auth/yt-analytics.readonly");
        var authUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
                      $"?client_id={Uri.EscapeDataString(_settings.YouTubeClientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      "&response_type=code" +
                      $"&scope={scope}" +
                      "&access_type=offline" +
                      "&prompt=consent";

        Process.Start(new ProcessStartInfo { FileName = authUrl, UseShellExecute = true });

        var ctxTask = listener.GetContextAsync();
        var completed = await Task.WhenAny(ctxTask, Task.Delay(TimeSpan.FromMinutes(5), ct));
        if (completed != ctxTask) throw new InvalidOperationException("OAuth Timeout");

        var ctx = await ctxTask;
        var code = ctx.Request.QueryString["code"];

        var responseHtml = Encoding.UTF8.GetBytes("<html><body><h2>Login erfolgreich. Fenster kann geschlossen werden.</h2></body></html>");
        ctx.Response.ContentLength64 = responseHtml.Length;
        await ctx.Response.OutputStream.WriteAsync(responseHtml, ct);
        ctx.Response.OutputStream.Close();
        listener.Stop();

        if (string.IsNullOrEmpty(code))
            throw new InvalidOperationException("Kein Authorization Code erhalten");

        var tokenResp = await _http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _settings.YouTubeClientId,
                ["client_secret"] = _settings.YouTubeClientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }), ct);

        var tokenBody = await tokenResp.Content.ReadAsStringAsync(ct);
        if (!tokenResp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Token Tausch fehlgeschlagen: {tokenBody}");

        using var doc = JsonDocument.Parse(tokenBody);
        var access = doc.RootElement.GetProperty("access_token").GetString()!;
        var refresh = doc.RootElement.GetProperty("refresh_token").GetString();
        if (!string.IsNullOrEmpty(refresh))
        {
            _settings.YouTubeRefreshToken = refresh;
            _settings.Save();
        }
        return access;
    }

    private async Task<string> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct)
    {
        var resp = await _http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["client_id"] = _settings.YouTubeClientId,
                ["client_secret"] = _settings.YouTubeClientSecret,
                ["grant_type"] = "refresh_token"
            }), ct);

        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Refresh-Token fehlgeschlagen: {body}\nBitte Settings -> Disconnect YouTube und neu verbinden.");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    /// <summary>
    /// Maps topic/title keywords to YouTube category IDs.
    /// Full list: https://developers.google.com/youtube/v3/docs/videoCategories
    /// </summary>
    public static string DetectCategory(string topic, string title)
    {
        var t = (topic + " " + title).ToLowerInvariant();
        bool Has(params string[] kw) => kw.Any(t.Contains);

        if (Has("tier", "haie", "hai", "hund", "katze", "löwe", "tiger", "schlange", "vogel",
                "fisch", "delfin", "wal", "bär", "wolf", "zoo", "natur", "wildtier"))         return "15"; // Pets & Animals
        if (Has("sport", "fußball", "tennis", "klettern", "surfen", "boxen", "kampfsport",
                "leichtathletik", "schwimmen", "radfahren", "ski", "extremsport"))             return "17"; // Sports
        if (Has("gaming", "game", "minecraft", "fortnite", "gta", "spiel", "videospiel"))     return "20"; // Gaming
        if (Has("musik", "music", "song", "lied", "konzert", "band", "rapper", "pop", "hip hop")) return "10"; // Music
        if (Has("reise", "travel", "urlaub", "städte", "stadt", "land", "welt", "backpacking"))   return "19"; // Travel & Events
        if (Has("weltall", "weltraum", "planet", "stern", "mond", "nasa", "raumfahrt",
                "wissenschaft", "physik", "chemie", "technologie", "ki ", "roboter",
                "computer", "quantencomputer", "cern", "genetik", "biologie"))                 return "28"; // Science & Technology
        if (Has("geschichte", "krieg", "römer", "wikinger", "hitler", "stalin", "diktator",
                "nazi", "pharao", "politik", "revolution", "krimi", "mörder", "killer",
                "serienmörder", "verbrechen", "mafia", "gang", "gefängnis", "true crime",
                "wahre verbrechen"))                                                            return "25"; // News & Politics
        if (Has("lustig", "fail", "komödie", "comedy", "witz", "stunt", "prank"))             return "23"; // Comedy
        if (Has("kochen", "backen", "rezept", "küche", "essen", "mode", "style", "makeup",
                "beauty", "diy"))                                                               return "26"; // Howto & Style
        if (Has("lernen", "tutorial", "erklär", "bildung", "schule", "uni", "kurs"))          return "27"; // Education
        return "24"; // Entertainment (default)
    }

    public async Task<string> UploadVideoAsync(VideoJob job, string videoPath, string? thumbnailPath,
        string visibility, IProgress<string>? log, CancellationToken ct,
        DateTime? publishAt = null, string? categoryId = null,
        Dictionary<string, (string Title, string Description)>? localizations = null)
    {
        var accessToken = await EnsureAccessTokenAsync(log, ct);
        log?.Report("Initialisiere YouTube Upload (resumable)...");

        var privacy = visibility?.ToLowerInvariant() switch
        {
            "public" => "public",
            "unlisted" => "unlisted",
            _ => "private"
        };

        // Build JSON with optional publishAt (scheduled publishing)
        var statusNode = new JsonObject
        {
            ["privacyStatus"] = publishAt.HasValue ? "private" : privacy,
            ["selfDeclaredMadeForKids"] = false,
            ["madeForKids"] = false,
            ["embeddable"] = true,
            ["publicStatsViewable"] = true,
            ["license"] = "youtube"
        };
        if (publishAt.HasValue)
        {
            statusNode["publishAt"] = publishAt.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            log?.Report($"  Geplante Veröffentlichung: {publishAt.Value:dd.MM.yyyy HH:mm} Uhr");
        }

        var tagsArray = new JsonArray();
        foreach (var tag in job.Tags) tagsArray.Add(JsonValue.Create(tag));

        var lang = string.IsNullOrWhiteSpace(job.Language) ? "de" : job.Language;
        var snippetNode = new JsonObject
        {
            ["title"] = job.Title,
            ["description"] = job.Description,
            ["tags"] = tagsArray,
            ["categoryId"] = categoryId ?? "24",
            ["defaultLanguage"] = lang,
            ["defaultAudioLanguage"] = lang
        };

        // Recording date — pretend the video was recorded today (helps with "fresh content" signal)
        var recordingNode = new JsonObject
        {
            ["recordingDate"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        var metaNode = new JsonObject
        {
            ["snippet"] = snippetNode,
            ["status"] = statusNode,
            ["recordingDetails"] = recordingNode
        };

        // Per-language localizations — shown to viewers whose YouTube UI matches the code.
        string parts = "snippet,status,recordingDetails";
        if (localizations != null && localizations.Count > 0)
        {
            var locNode = new JsonObject();
            foreach (var (code, (lt, ld)) in localizations)
                locNode[code] = new JsonObject { ["title"] = lt, ["description"] = ld };
            metaNode["localizations"] = locNode;
            parts += ",localizations";
        }

        var metaJson = metaNode.ToJsonString();

        var fileInfo = new FileInfo(videoPath);
        using var initReq = new HttpRequestMessage(HttpMethod.Post,
            $"https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part={parts}&notifySubscribers=true");
        initReq.Headers.Add("Authorization", $"Bearer {accessToken}");
        initReq.Headers.Add("X-Upload-Content-Length", fileInfo.Length.ToString());
        initReq.Headers.Add("X-Upload-Content-Type", "video/*");
        initReq.Content = new StringContent(metaJson, Encoding.UTF8, "application/json");

        using var initResp = await _http.SendAsync(initReq, ct);
        if (!initResp.IsSuccessStatusCode)
        {
            var err = await initResp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"YouTube init Fehler {(int)initResp.StatusCode}: {err}");
        }
        var uploadUrl = initResp.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("Keine Upload-URL erhalten");

        log?.Report($"Lade {fileInfo.Length / 1024 / 1024} MB hoch...");

        await using var fs = File.OpenRead(videoPath);
        using var content = new StreamContent(fs);
        content.Headers.Add("Content-Type", "video/*");

        using var upReq = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = content };
        upReq.Headers.Add("Authorization", $"Bearer {accessToken}");

        using var http = new HttpClient { Timeout = TimeSpan.FromHours(1) };
        using var upResp = await http.SendAsync(upReq, ct);
        var upBody = await upResp.Content.ReadAsStringAsync(ct);
        if (!upResp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Upload Fehler {(int)upResp.StatusCode}: {upBody}");

        using var doc = JsonDocument.Parse(upBody);
        var videoId = doc.RootElement.GetProperty("id").GetString()!;
        log?.Report($"Hochgeladen: {videoId}");

        if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
        {
            try
            {
                // Thumbnail API has a low quota — wait a moment after the video upload
                // then retry up to 3 times with exponential backoff on 429.
                await Task.Delay(3000, ct);
                await UploadThumbnailAsync(videoId, thumbnailPath, accessToken, ct);
                log?.Report("Thumbnail gesetzt.");
            }
            catch (Exception ex)
            {
                log?.Report($"Warnung: Thumbnail-Upload fehlgeschlagen ({ex.Message}).");
            }
        }

        return $"https://www.youtube.com/watch?v={videoId}";
    }

    private async Task UploadThumbnailAsync(string videoId, string thumbPath, string accessToken, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(thumbPath, ct);
        int[] delays = [5, 15, 30]; // seconds between retries
        for (int attempt = 0; attempt <= delays.Length; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post,
                $"https://www.googleapis.com/upload/youtube/v3/thumbnails/set?videoId={videoId}");
            req.Headers.Add("Authorization", $"Bearer {accessToken}");
            req.Content = new ByteArrayContent(bytes);
            req.Content.Headers.Add("Content-Type", "image/jpeg");
            using var resp = await _http.SendAsync(req, ct);

            if (resp.IsSuccessStatusCode) return;

            if ((int)resp.StatusCode == 429 && attempt < delays.Length)
            {
                // Respect Retry-After header if present, else use our backoff
                int wait = delays[attempt];
                if (resp.Headers.TryGetValues("Retry-After", out var ra) &&
                    int.TryParse(ra.FirstOrDefault(), out var raVal))
                    wait = Math.Max(wait, raVal);
                await Task.Delay(TimeSpan.FromSeconds(wait), ct);
                continue;
            }

            resp.EnsureSuccessStatusCode(); // throws for other error codes
        }
    }

    public record VideoStats(string VideoId, string Title, long Views, long Likes, long Comments, DateTime PublishedAt);

    /// <summary>
    /// Posts a top-level comment on a video. Used right after upload to seed engagement.
    /// </summary>
    public async Task PostCommentAsync(string videoId, string text, string accessToken, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["snippet"] = new JsonObject
            {
                ["videoId"] = videoId,
                ["topLevelComment"] = new JsonObject
                {
                    ["snippet"] = new JsonObject { ["textOriginal"] = text }
                }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post,
            "https://www.googleapis.com/youtube/v3/commentThreads?part=snippet");
        req.Headers.Add("Authorization", $"Bearer {accessToken}");
        req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Kommentar fehlgeschlagen: {err}");
        }
    }

    /// <summary>
    /// Fetches statistics (views, likes, comments, publishedAt, title) for a batch of video IDs.
    /// Public API — no Analytics scope required.
    /// </summary>
    public async Task<List<VideoStats>> GetVideoStatsAsync(List<string> videoIds, string accessToken, CancellationToken ct)
    {
        var stats = new List<VideoStats>();
        for (int offset = 0; offset < videoIds.Count; offset += 50)
        {
            var batch = videoIds.Skip(offset).Take(50);
            var ids = string.Join(",", batch);
            var url = $"https://www.googleapis.com/youtube/v3/videos?part=snippet,statistics&id={ids}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("Authorization", $"Bearer {accessToken}");
            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) continue;

            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                var vid = item.GetProperty("id").GetString() ?? "";
                var snippet = item.GetProperty("snippet");
                var st = item.GetProperty("statistics");

                var title = snippet.GetProperty("title").GetString() ?? "";
                var published = snippet.TryGetProperty("publishedAt", out var pEl)
                    ? pEl.GetDateTime() : DateTime.MinValue;

                long views = st.TryGetProperty("viewCount", out var vEl) && long.TryParse(vEl.GetString(), out var vv) ? vv : 0;
                long likes = st.TryGetProperty("likeCount", out var lEl) && long.TryParse(lEl.GetString(), out var ll) ? ll : 0;
                long comments = st.TryGetProperty("commentCount", out var cEl) && long.TryParse(cEl.GetString(), out var cc) ? cc : 0;

                stats.Add(new VideoStats(vid, title, views, likes, comments, published));
            }
        }
        return stats;
    }

    /// <summary>
    /// Updates an existing video's title and/or tags. We must fetch the current snippet first
    /// because YouTube requires the full snippet on PUT (categoryId in particular).
    /// </summary>
    public async Task UpdateVideoMetadataAsync(string videoId, string? newTitle, List<string>? newTags, string accessToken, CancellationToken ct)
    {
        // Step 1: fetch current snippet
        var getUrl = $"https://www.googleapis.com/youtube/v3/videos?part=snippet&id={videoId}";
        using var getReq = new HttpRequestMessage(HttpMethod.Get, getUrl);
        getReq.Headers.Add("Authorization", $"Bearer {accessToken}");
        using var getResp = await _http.SendAsync(getReq, ct);
        var getBody = await getResp.Content.ReadAsStringAsync(ct);
        if (!getResp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Snippet laden fehlgeschlagen: {getBody}");

        using var doc = JsonDocument.Parse(getBody);
        var items = doc.RootElement.GetProperty("items");
        if (items.GetArrayLength() == 0)
            throw new InvalidOperationException("Video nicht gefunden.");

        var snippet = items[0].GetProperty("snippet");
        var title = newTitle ?? snippet.GetProperty("title").GetString() ?? "";
        var description = snippet.TryGetProperty("description", out var dEl) ? dEl.GetString() ?? "" : "";
        var categoryId = snippet.TryGetProperty("categoryId", out var cEl) ? cEl.GetString() ?? "24" : "24";

        var tagsArray = new JsonArray();
        if (newTags != null)
        {
            foreach (var t in newTags) tagsArray.Add(JsonValue.Create(t));
        }
        else if (snippet.TryGetProperty("tags", out var tEl) && tEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in tEl.EnumerateArray()) tagsArray.Add(JsonValue.Create(t.GetString()));
        }

        var update = new JsonObject
        {
            ["id"] = videoId,
            ["snippet"] = new JsonObject
            {
                ["title"] = title,
                ["description"] = description,
                ["categoryId"] = categoryId,
                ["tags"] = tagsArray
            }
        };

        using var putReq = new HttpRequestMessage(HttpMethod.Put,
            "https://www.googleapis.com/youtube/v3/videos?part=snippet");
        putReq.Headers.Add("Authorization", $"Bearer {accessToken}");
        putReq.Content = new StringContent(update.ToJsonString(), Encoding.UTF8, "application/json");
        using var putResp = await _http.SendAsync(putReq, ct);
        if (!putResp.IsSuccessStatusCode)
        {
            var err = await putResp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Update fehlgeschlagen: {err}");
        }
    }

    public record CommentThread(string ThreadId, string CommentId, string AuthorName, string Text);

    /// <summary>Lists top-level comments on a video.</summary>
    public async Task<List<CommentThread>> ListCommentsAsync(string videoId, string accessToken, CancellationToken ct, int max = 20)
    {
        var url = $"https://www.googleapis.com/youtube/v3/commentThreads?part=snippet&videoId={videoId}&maxResults={max}&order=time";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Authorization", $"Bearer {accessToken}");
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return [];

        var body = await resp.Content.ReadAsStringAsync(ct);
        var list = new List<CommentThread>();
        using var doc = JsonDocument.Parse(body);
        foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
        {
            var threadId = item.GetProperty("id").GetString() ?? "";
            var top = item.GetProperty("snippet").GetProperty("topLevelComment");
            var commentId = top.GetProperty("id").GetString() ?? "";
            var snippet = top.GetProperty("snippet");
            var author = snippet.TryGetProperty("authorDisplayName", out var aEl) ? aEl.GetString() ?? "" : "";
            var text = snippet.TryGetProperty("textOriginal", out var txEl) ? txEl.GetString() ?? "" : "";
            list.Add(new CommentThread(threadId, commentId, author, text));
        }
        return list;
    }

    /// <summary>Posts a reply to an existing comment thread.</summary>
    public async Task ReplyToCommentAsync(string parentCommentId, string text, string accessToken, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["snippet"] = new JsonObject
            {
                ["parentId"] = parentCommentId,
                ["textOriginal"] = text
            }
        };
        using var req = new HttpRequestMessage(HttpMethod.Post,
            "https://www.googleapis.com/youtube/v3/comments?part=snippet");
        req.Headers.Add("Authorization", $"Bearer {accessToken}");
        req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Reply fehlgeschlagen: {err}");
        }
    }

    /// <summary>
    /// Returns the playlist ID for the given topic, creating the playlist if it doesn't exist yet.
    /// </summary>
    public async Task<string> EnsurePlaylistAsync(string topic, string accessToken, CancellationToken ct)
    {
        // Create playlist
        var metaNode = new JsonObject
        {
            ["snippet"] = new JsonObject
            {
                ["title"] = topic,
                ["description"] = $"Automatisch erstellte Playlist zum Thema: {topic}",
                ["defaultLanguage"] = "de"
            },
            ["status"] = new JsonObject { ["privacyStatus"] = "public" }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post,
            "https://www.googleapis.com/youtube/v3/playlists?part=snippet,status");
        req.Headers.Add("Authorization", $"Bearer {accessToken}");
        req.Content = new StringContent(metaNode.ToJsonString(), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Playlist erstellen fehlgeschlagen: {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    /// <summary>Adds a video to an existing playlist.</summary>
    public async Task AddToPlaylistAsync(string playlistId, string videoId, string accessToken, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["snippet"] = new JsonObject
            {
                ["playlistId"] = playlistId,
                ["resourceId"] = new JsonObject
                {
                    ["kind"] = "youtube#video",
                    ["videoId"] = videoId
                }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post,
            "https://www.googleapis.com/youtube/v3/playlistItems?part=snippet");
        req.Headers.Add("Authorization", $"Bearer {accessToken}");
        req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Playlist-Item fehlgeschlagen: {err}");
        }
    }

    /// <summary>
    /// Checks every uploaded video ID for blocks or copyright strikes.
    /// Deletes the video automatically if YouTube has rejected it, and returns
    /// a list of (videoId, reason) pairs for each video that was removed.
    /// </summary>
    public async Task<List<(string videoId, string reason)>> DeleteBlockedVideosAsync(
        List<string> videoIds, IProgress<string>? log, CancellationToken ct)
    {
        if (videoIds.Count == 0) return [];

        var accessToken = await EnsureAccessTokenAsync(log, ct);
        var deleted = new List<(string, string)>();

        // Fetch status for up to 50 IDs at once (API limit per request)
        for (int offset = 0; offset < videoIds.Count; offset += 50)
        {
            var batch = videoIds.Skip(offset).Take(50).ToList();
            var ids = string.Join(",", batch.Select(Uri.EscapeDataString));
            var url = $"https://www.googleapis.com/youtube/v3/videos?part=status&id={ids}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("Authorization", $"Bearer {accessToken}");
            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) continue;

            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);

            var returnedIds = new HashSet<string>();
            foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                var vid = item.GetProperty("id").GetString() ?? "";
                returnedIds.Add(vid);

                var status = item.GetProperty("status");
                var uploadStatus = status.TryGetProperty("uploadStatus", out var us)
                    ? us.GetString() ?? "" : "";
                var rejectionReason = status.TryGetProperty("rejectionReason", out var rr)
                    ? rr.GetString() ?? "" : "";

                if (uploadStatus is "rejected" or "failed")
                {
                    log?.Report($"Video {vid} gesperrt ({uploadStatus}: {rejectionReason}) — lösche...");
                    await DeleteVideoAsync(vid, accessToken, ct);
                    deleted.Add((vid, $"{uploadStatus}: {rejectionReason}"));
                }
            }

            // IDs missing from API response = already removed by YouTube
            foreach (var id in batch.Where(id => !returnedIds.Contains(id)))
            {
                log?.Report($"Video {id} nicht mehr vorhanden — von YouTube entfernt.");
                deleted.Add((id, "von YouTube entfernt"));
            }
        }
        return deleted;
    }

    private async Task DeleteVideoAsync(string videoId, string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete,
            $"https://www.googleapis.com/youtube/v3/videos?id={videoId}");
        req.Headers.Add("Authorization", $"Bearer {accessToken}");
        using var resp = await _http.SendAsync(req, ct);
        // 204 No Content = success; anything else = already gone
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
