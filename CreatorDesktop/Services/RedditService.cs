using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Posts YouTube uploads to Reddit via the OAuth "password" grant flow (script app).
/// User registers a script app on reddit.com/prefs/apps and provides client id/secret
/// plus their Reddit username + password. 2FA must be off OR an app-specific password used.
/// </summary>
public class RedditService
{
    private static readonly HttpClient _http = new();
    private readonly AppSettings _settings;

    public RedditService(AppSettings settings) => _settings = settings;

    private string UserAgent =>
        $"CreatorDesktop/1.0 by {(_settings.RedditUsername is var u && !string.IsNullOrWhiteSpace(u) ? u : "anon")}";

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.RedditClientId) ||
            string.IsNullOrWhiteSpace(_settings.RedditClientSecret) ||
            string.IsNullOrWhiteSpace(_settings.RedditUsername) ||
            string.IsNullOrWhiteSpace(_settings.RedditPassword))
            throw new InvalidOperationException("Reddit-Zugangsdaten fehlen — in Settings ausfüllen.");

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token");
        var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes(
            $"{_settings.RedditClientId}:{_settings.RedditClientSecret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        req.Headers.UserAgent.ParseAdd(UserAgent);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = _settings.RedditUsername,
            ["password"] = _settings.RedditPassword
        });

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Reddit-Login fehlgeschlagen: {body}");

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("access_token", out var tokEl))
            throw new InvalidOperationException($"Reddit gab keinen access_token zurück: {body}");
        return tokEl.GetString()!;
    }

    /// <summary>Submits a link post (with the YouTube URL) to one subreddit.</summary>
    public async Task<string> SubmitLinkAsync(string subreddit, string title, string url,
        string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://oauth.reddit.com/api/submit");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Headers.UserAgent.ParseAdd(UserAgent);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["api_type"] = "json",
            ["kind"] = "link",
            ["sr"] = subreddit,
            ["title"] = title.Length > 300 ? title[..300] : title,
            ["url"] = url,
            ["resubmit"] = "true",
            ["sendreplies"] = "false"
        });

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Reddit-Post fehlgeschlagen ({(int)resp.StatusCode}): {body}");

        // Reddit returns errors inside a 200 OK — check the JSON
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("json", out var jsonEl) &&
            jsonEl.TryGetProperty("errors", out var errsEl) &&
            errsEl.ValueKind == JsonValueKind.Array && errsEl.GetArrayLength() > 0)
        {
            var msg = string.Join("; ", errsEl.EnumerateArray().Select(e => e.ToString()));
            throw new InvalidOperationException($"Reddit-Fehler: {msg}");
        }

        return body;
    }

    /// <summary>Posts the same link to every configured subreddit, returning per-subreddit results.</summary>
    public async Task<List<(string subreddit, bool ok, string message)>> CrossPostAsync(
        string title, string youtubeUrl, IProgress<string>? log, CancellationToken ct)
    {
        var results = new List<(string, bool, string)>();
        if (_settings.RedditSubreddits.Count == 0)
        {
            log?.Report("  Keine Subreddits konfiguriert.");
            return results;
        }

        var token = await GetAccessTokenAsync(ct);
        foreach (var sr in _settings.RedditSubreddits)
        {
            var sub = sr.Trim().TrimStart('r', '/').TrimStart('/');
            if (string.IsNullOrEmpty(sub)) continue;
            try
            {
                await SubmitLinkAsync(sub, title, youtubeUrl, token, ct);
                log?.Report($"  ✓ r/{sub} — gepostet.");
                results.Add((sub, true, "ok"));
                // Reddit rate-limits aggressive cross-posting — pace ourselves.
                await Task.Delay(2000, ct);
            }
            catch (Exception ex)
            {
                log?.Report($"  r/{sub} — {ex.Message}");
                results.Add((sub, false, ex.Message));
            }
        }
        return results;
    }
}
