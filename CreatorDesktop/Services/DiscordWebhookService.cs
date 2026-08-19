using System.Text;
using System.Text.Json.Nodes;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Posts new uploads as rich embeds to a Discord channel webhook. No bot/token
/// required — the URL itself is the authentication. Create one in Discord:
/// Channel → Edit → Integrations → Webhooks → New Webhook → Copy URL.
/// </summary>
public class DiscordWebhookService
{
    private static readonly HttpClient _http = new();
    private readonly AppSettings _settings;

    public DiscordWebhookService(AppSettings settings) => _settings = settings;

    public async Task PostUploadAsync(string title, string youtubeUrl, string videoId,
        IProgress<string>? log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.DiscordWebhookUrl))
            throw new InvalidOperationException("Discord-Webhook-URL fehlt.");

        // YouTube's default thumbnail URL pattern — hqdefault is reliable and always present
        var thumbUrl = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";

        var payload = new JsonObject
        {
            ["content"] = "🎬 **Neues Video ist online!**",
            ["embeds"] = new JsonArray
            {
                new JsonObject
                {
                    ["title"] = title.Length > 256 ? title[..256] : title,
                    ["url"] = youtubeUrl,
                    ["color"] = 16711680, // red
                    ["image"] = new JsonObject { ["url"] = thumbUrl },
                    ["footer"] = new JsonObject { ["text"] = "Auto-Post aus CreatorDesktop" }
                }
            }
        };

        var json = payload.ToJsonString();
        using var req = new HttpRequestMessage(HttpMethod.Post, _settings.DiscordWebhookUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Webhook-Fehler ({(int)resp.StatusCode}): {body}");
        }
        log?.Report("  ✓ Discord-Webhook gepostet.");
    }
}
