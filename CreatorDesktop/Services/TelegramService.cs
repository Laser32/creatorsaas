using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Posts YouTube upload links to a Telegram channel/group via the Bot API.
/// Setup: chat with @BotFather → /newbot → copy token. Add the bot as admin to your
/// channel. Send any message in the channel, then visit
/// https://api.telegram.org/bot&lt;TOKEN&gt;/getUpdates to read the chat id (negative number).
/// </summary>
public class TelegramService
{
    private static readonly HttpClient _http = new();
    private readonly AppSettings _settings;

    public TelegramService(AppSettings settings) => _settings = settings;

    /// <summary>
    /// Sends a message to the configured chat. Telegram auto-renders a YouTube preview
    /// when the URL is in the text — no need for a separate upload.
    /// </summary>
    public async Task SendMessageAsync(string title, string youtubeUrl, IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.TelegramBotToken) ||
            string.IsNullOrWhiteSpace(_settings.TelegramChatId))
            throw new InvalidOperationException("Telegram Bot-Token oder Chat-ID fehlt — in Settings ausfüllen.");

        var text = string.IsNullOrWhiteSpace(_settings.TelegramTemplate)
            ? $"🎬 {title}\n\n{youtubeUrl}"
            : _settings.TelegramTemplate
                .Replace("{title}", title)
                .Replace("{url}", youtubeUrl);

        var url = $"https://api.telegram.org/bot{_settings.TelegramBotToken}/sendMessage";
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["chat_id"] = _settings.TelegramChatId,
            ["text"] = text,
            ["disable_web_page_preview"] = "false",
            ["parse_mode"] = "HTML"
        });

        using var resp = await _http.PostAsync(url, form, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Telegram-Fehler ({(int)resp.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("ok", out var okEl) && !okEl.GetBoolean())
        {
            var desc = doc.RootElement.TryGetProperty("description", out var dEl) ? dEl.GetString() : "unknown";
            throw new InvalidOperationException($"Telegram: {desc}");
        }
        log?.Report("  ✓ Telegram-Post abgesetzt.");
    }
}
