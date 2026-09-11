using System.Net.Http.Headers;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Polls a Discord channel for user wishes via the Bot API. For each new message
/// it searches YouTube via yt-dlp; matches are stored in wishes.json. Setup:
/// 1. Create a bot at https://discord.com/developers/applications
/// 2. Reset/copy token under "Bot" section
/// 3. OAuth2 URL generator → scope=bot, permission=Read Messages + Read Message History
/// 4. Open URL in browser → add bot to your server
/// 5. Enable "Developer Mode" in Discord settings, right-click target channel → Copy ID
/// </summary>
public class DiscordWishService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly AppSettings _settings;

    public DiscordWishService(AppSettings settings) => _settings = settings;

    private record DiscordMessage(string Id, string Content, string Author, DateTime Timestamp);

    private async Task<List<DiscordMessage>> FetchMessagesAsync(int limit, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.DiscordBotToken) ||
            string.IsNullOrWhiteSpace(_settings.DiscordWishChannelId))
            throw new InvalidOperationException("Discord Bot-Token oder Channel-ID fehlt.");

        var url = $"https://discord.com/api/v10/channels/{_settings.DiscordWishChannelId}/messages?limit={limit}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bot", _settings.DiscordBotToken);
        req.Headers.UserAgent.ParseAdd("CreatorDesktop/1.0");

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Discord-Fehler ({(int)resp.StatusCode}): {body}");

        var list = new List<DiscordMessage>();
        using var doc = JsonDocument.Parse(body);
        foreach (var m in doc.RootElement.EnumerateArray())
        {
            var id = m.GetProperty("id").GetString() ?? "";
            var content = m.GetProperty("content").GetString() ?? "";
            var author = m.GetProperty("author").GetProperty("username").GetString() ?? "";
            var ts = m.TryGetProperty("timestamp", out var tsEl) ? tsEl.GetDateTime() : DateTime.UtcNow;
            // Ignore bots posting in the channel (incl. ourselves)
            var isBot = m.GetProperty("author").TryGetProperty("bot", out var bEl) && bEl.GetBoolean();
            if (isBot) continue;
            if (string.IsNullOrWhiteSpace(content)) continue;
            list.Add(new DiscordMessage(id, content, author, ts));
        }
        return list;
    }

    /// <summary>
    /// Reads new messages from the wish channel, runs a YouTube search per wish,
    /// and persists matches into wishes.json. Returns the newly added wishes.
    /// </summary>
    public async Task<List<WishEntry>> PollAndSearchAsync(string language, IProgress<string>? log, CancellationToken ct)
    {
        log?.Report("Hole Discord-Wünsche...");
        var msgs = await FetchMessagesAsync(50, ct);
        log?.Report($"  {msgs.Count} Nachricht(en) gefunden.");

        var existing = AppSettings.LoadWishes();
        var existingIds = existing.Select(w => w.MessageId).ToHashSet();
        var addedNow = new List<WishEntry>();

        foreach (var msg in msgs)
        {
            if (existingIds.Contains(msg.Id)) continue;
            if (_settings.ProcessedWishMessageIds.Contains(msg.Id)) continue;

            log?.Report($"  Suche YouTube für: \"{msg.Content}\" (von {msg.Author})");
            try
            {
                var results = await YtDlpDownloader.SearchAsync(
                    msg.Content, 5, ccOnly: false, null, ct, language: language);

                // Only persist when we actually found something
                if (results.Count == 0)
                {
                    log?.Report("    Keine Treffer — überspringe.");
                    _settings.ProcessedWishMessageIds.Add(msg.Id); // don't keep searching same dud
                    continue;
                }

                var entry = new WishEntry
                {
                    MessageId = msg.Id,
                    User = msg.Author,
                    Wish = msg.Content,
                    FoundAt = DateTime.Now,
                    Results = results.Select(r => new WishResult
                    {
                        VideoId = r.id, Title = r.title, DurationSeconds = r.durationSeconds
                    }).ToList()
                };
                existing.Add(entry);
                addedNow.Add(entry);
                _settings.ProcessedWishMessageIds.Add(msg.Id);
                log?.Report($"    ✓ {results.Count} Treffer hinzugefügt.");
            }
            catch (Exception ex)
            {
                log?.Report($"    Fehler: {ex.Message}");
            }
        }

        if (addedNow.Count > 0)
        {
            AppSettings.SaveWishes(existing);
            _settings.Save();
        }
        _settings.LastWishPollAt = DateTime.Now;
        _settings.Save();
        return addedNow;
    }
}
