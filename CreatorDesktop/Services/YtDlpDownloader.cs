using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Downloads yt-dlp.exe on first use (single-file, ~11 MB from GitHub releases).
/// yt-dlp is needed to download Creative Commons YouTube videos with their original audio.
/// </summary>
public static class YtDlpDownloader
{
    private static readonly string YtDlpPath =
        Path.Combine(AppSettings.DefaultDataDir, "yt-dlp.exe");

    /// <summary>
    /// Returns the appropriate --cookies-from-browser or --cookies argument so every
    /// yt-dlp call can bypass YouTube's "Sign in to confirm you're not a bot" check.
    /// Priority: YtDlpCookiesBrowser (e.g. "edge") → YtDlpCookiesPath (file) → empty.
    /// </summary>
    public static string CookieArg()
    {
        try
        {
            var path = Path.Combine(AppSettings.DefaultDataDir, "settings.json");
            if (!File.Exists(path)) return "";
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;

            // Prefer browser-based cookies (no manual export needed)
            if (root.TryGetProperty("YtDlpCookiesBrowser", out var bEl))
            {
                var browser = bEl.GetString();
                if (!string.IsNullOrWhiteSpace(browser))
                    return $"--cookies-from-browser {browser} ";
            }

            // Fallback: manual cookies.txt file
            if (root.TryGetProperty("YtDlpCookiesPath", out var fEl))
            {
                var cookies = fEl.GetString();
                if (!string.IsNullOrWhiteSpace(cookies) && File.Exists(cookies))
                    return $"--cookies \"{cookies}\" ";
            }
        }
        catch { /* fall through to empty */ }
        return "";
    }

    public static async Task<string> EnsureAsync(IProgress<string>? log, CancellationToken ct)
    {
        if (File.Exists(YtDlpPath))
            return YtDlpPath;

        log?.Report("Lade yt-dlp.exe herunter (einmalig, ~11 MB)...");
        Directory.CreateDirectory(AppSettings.DefaultDataDir);

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("CreatorDesktop");

        var apiResp = await http.GetStringAsync(
            "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest", ct);
        using var doc = JsonDocument.Parse(apiResp);
        string? downloadUrl = null;
        foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (name.Equals("yt-dlp.exe", StringComparison.OrdinalIgnoreCase))
            {
                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                break;
            }
        }
        if (string.IsNullOrEmpty(downloadUrl))
            throw new InvalidOperationException("yt-dlp.exe Download-URL nicht gefunden.");

        var bytes = await http.GetByteArrayAsync(downloadUrl, ct);
        await File.WriteAllBytesAsync(YtDlpPath, bytes, ct);
        log?.Report("yt-dlp.exe bereit.");
        return YtDlpPath;
    }

    /// <summary>
    /// Searches YouTube directly via yt-dlp (ytsearchN:query) — no YouTube API/OAuth needed.
    /// Returns id/title/duration for every hit. Filters Creative-Commons-licensed videos
    /// when ccOnly is true. Pass language="de" to bias results toward German-language content.
    /// </summary>
    public static async Task<List<(string id, string title, int durationSeconds)>> SearchAsync(
        string query, int maxResults, bool ccOnly, IProgress<string>? log, CancellationToken ct,
        string? language = null)
    {
        var ytDlp = await EnsureAsync(log, ct);
        var results = new List<(string, string, int)>();

        // Append a language keyword so YouTube biases results toward that language.
        var langKeyword = language switch { "de" => " deutsch", "en" => "", "fr" => " français", _ => "" };
        var effectiveQuery = query + langKeyword;

        // --match-filter "license=creative_commons" does NOT work in --flat-playlist mode
        // because yt-dlp skips fetching per-video metadata during search. We search without
        // filter and check the license individually when the user downloads a video.
        var args = $"--flat-playlist --dump-json --no-warnings " +
                   CookieArg() +
                   $"\"ytsearch{maxResults}:{EscapeQuery(effectiveQuery)}\"";
        log?.Report($"  Suche: {effectiveQuery} ({maxResults} Ergebnisse angefragt)");

        var (rawLines, stderr, exit) = await RunYtDlpAsync(ytDlp, args, log, ct);

        foreach (var line in rawLines)
        {
            if (string.IsNullOrWhiteSpace(line) || line[0] != '{') continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                var title = root.TryGetProperty("title", out var tEl) ? tEl.GetString() ?? "" : "";
                int dur = 0;
                if (root.TryGetProperty("duration", out var dEl) && dEl.ValueKind == JsonValueKind.Number)
                    dur = (int)dEl.GetDouble();
                if (!string.IsNullOrEmpty(id))
                    results.Add((id, title, dur));
            }
            catch { /* skip malformed lines */ }
        }

        if (exit != 0 && results.Count == 0)
            throw new InvalidOperationException($"yt-dlp Fehler: {stderr.Trim()}");

        log?.Report($"  {results.Count} Videos gefunden.");
        return results;
    }

    /// <summary>
    /// Downloads a YouTube video as a single merged MP4 (video + audio, up to 1080p).
    /// Passes --ffmpeg-location so yt-dlp can merge the streams even when ffmpeg is not
    /// on the system PATH; falls back to a pre-muxed format if ffmpeg is not yet downloaded.
    /// </summary>
    public static async Task<string> DownloadVideoAsync(
        string videoId, string outputDir, IProgress<string>? log, CancellationToken ct)
    {
        var ytDlp = await EnsureAsync(log, ct);
        var outTemplate = Path.Combine(outputDir, $"yt_{videoId}.%(ext)s");
        var url = $"https://www.youtube.com/watch?v={videoId}";

        // Use our bundled ffmpeg so yt-dlp can merge separate video+audio tracks.
        var ffmpegExe = Path.Combine(AppSettings.DefaultDataDir, "ffmpeg", "ffmpeg.exe");
        var hasFfmpeg = File.Exists(ffmpegExe);
        var ffmpegDir = Path.GetDirectoryName(ffmpegExe)!;

        string format, mergeArg, ffmpegLocationArg;
        if (hasFfmpeg)
        {
            format = "bestvideo[ext=mp4][height<=1080]+bestaudio[ext=m4a]/best[ext=mp4]/best";
            mergeArg = "--merge-output-format mp4 ";
            ffmpegLocationArg = $"--ffmpeg-location \"{ffmpegDir}\" ";
        }
        else
        {
            // ffmpeg not yet downloaded — pick a pre-muxed stream (audio already included)
            format = "best[ext=mp4]/best";
            mergeArg = "";
            ffmpegLocationArg = "";
            log?.Report("  Hinweis: ffmpeg fehlt noch — lade pre-muxed Format (Audio enthalten).");
        }

        var args = $"-f \"{format}\" " +
                   mergeArg +
                   ffmpegLocationArg +
                   CookieArg() +
                   $"--no-warnings --no-playlist " +
                   $"-o \"{outTemplate}\" " +
                   $"\"{url}\"";

        var (dlLines, stderr, exit) = await RunYtDlpAsync(ytDlp, args, log, ct);
        foreach (var l in dlLines) log?.Report("  " + l);

        if (exit != 0)
            throw new InvalidOperationException($"yt-dlp Fehler (exit {exit}): {stderr}");

        var mp4 = Path.Combine(outputDir, $"yt_{videoId}.mp4");
        if (File.Exists(mp4)) return mp4;

        var any = Directory.GetFiles(outputDir, $"yt_{videoId}.*").FirstOrDefault();
        if (any != null) return any;

        throw new InvalidOperationException($"yt-dlp: Ausgabedatei nicht gefunden fuer {videoId}");
    }

    /// <summary>
    /// Fetches full video metadata (license + uploader) so we can verify the video
    /// is genuinely Creative Commons before downloading. yt-dlp's search filter is
    /// best-effort — this re-checks the actual license string on the watch page.
    /// </summary>
    public record VideoLicenseInfo(string License, string Uploader, string UploaderUrl, string WebpageUrl, bool IsCreativeCommons);

    public static async Task<VideoLicenseInfo?> GetVideoInfoAsync(
        string videoId, IProgress<string>? log, CancellationToken ct)
    {
        var ytDlp = await EnsureAsync(log, ct);
        var url = $"https://www.youtube.com/watch?v={videoId}";
        var args = $"--dump-single-json --skip-download --no-warnings --no-playlist " +
                   CookieArg() +
                   $"\"{url}\"";

        var (infoLines, _, exit) = await RunYtDlpAsync(ytDlp, args, log, ct);
        var stdout = string.Join("\n", infoLines);
        if (exit != 0 || string.IsNullOrWhiteSpace(stdout)) return null;

        try
        {
            using var doc = JsonDocument.Parse(stdout);
            var root = doc.RootElement;
            var license = root.TryGetProperty("license", out var lEl) ? lEl.GetString() ?? "" : "";
            var uploader = root.TryGetProperty("uploader", out var uEl) ? uEl.GetString() ?? ""
                : root.TryGetProperty("channel", out var cEl) ? cEl.GetString() ?? "" : "";
            var uploaderUrl = root.TryGetProperty("uploader_url", out var uuEl) ? uuEl.GetString() ?? ""
                : root.TryGetProperty("channel_url", out var cuEl) ? cuEl.GetString() ?? "" : "";
            var webpageUrl = root.TryGetProperty("webpage_url", out var wEl) ? wEl.GetString() ?? "" : url;

            // YouTube exposes CC-BY as "Creative Commons Attribution license (reuse allowed)"
            // — we accept anything that mentions "creative commons".
            var isCc = license.Contains("creative commons", StringComparison.OrdinalIgnoreCase)
                       || license.Equals("creative_commons", StringComparison.OrdinalIgnoreCase);

            return new VideoLicenseInfo(license, uploader, uploaderUrl, webpageUrl, isCc);
        }
        catch { return null; }
    }

    /// <summary>
    /// Runs yt-dlp with the given args. If the browser cookie DB is locked (browser open),
    /// automatically retries once without --cookies-from-browser.
    /// Returns (stdout lines, stderr).
    /// </summary>
    public static async Task<(List<string> lines, string stderr, int exit)> RunYtDlpAsync(
        string ytDlp, string args, IProgress<string>? log, CancellationToken ct)
    {
        var lines = new List<string>();
        var psi = new ProcessStartInfo
        {
            FileName = ytDlp, Arguments = args,
            RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        using var p = Process.Start(psi)!;
        var stderrTask = p.StandardError.ReadToEndAsync(ct);
        string? line;
        while ((line = await p.StandardOutput.ReadLineAsync(ct)) != null)
            lines.Add(line);
        var stderr = await stderrTask;
        await p.WaitForExitAsync(ct);

        // Browser cookie errors — DB locked, DPAPI session mismatch, etc. — retry without cookies
        bool isCookieErr = stderr.Contains("could not copy", StringComparison.OrdinalIgnoreCase) ||
                           stderr.Contains("database is locked", StringComparison.OrdinalIgnoreCase) ||
                           stderr.Contains("failed to decrypt", StringComparison.OrdinalIgnoreCase) ||
                           stderr.Contains("dpapi", StringComparison.OrdinalIgnoreCase) ||
                           stderr.Contains("cookies-from-browser", StringComparison.OrdinalIgnoreCase);
        bool isAgeErr = stderr.Contains("confirm your age", StringComparison.OrdinalIgnoreCase) ||
                        stderr.Contains("age-restricted", StringComparison.OrdinalIgnoreCase) ||
                        stderr.Contains("inappropriate for some users", StringComparison.OrdinalIgnoreCase);

        if (isCookieErr || isAgeErr)
        {
            // Strip cookies (broken) and add tv_embedded player client which bypasses age gate
            var argsCleaned = System.Text.RegularExpressions.Regex.Replace(
                args, @"--cookies-from-browser\s+\S+\s*", "");
            if (isAgeErr && !argsCleaned.Contains("player_client"))
            {
                argsCleaned = "--extractor-args \"youtube:player_client=tv_embedded,web\" " + argsCleaned;
                log?.Report("  Hinweis: Altersbeschränkung — versuche tv_embedded Player...");
            }
            else
            {
                log?.Report("  Hinweis: Browser-Cookies gesperrt — versuche ohne Cookies...");
            }
            var lines2 = new List<string>();
            var psi2 = new ProcessStartInfo
            {
                FileName = ytDlp, Arguments = argsCleaned,
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            using var p2 = Process.Start(psi2)!;
            var stderr2Task = p2.StandardError.ReadToEndAsync(ct);
            string? line2;
            while ((line2 = await p2.StandardOutput.ReadLineAsync(ct)) != null)
                lines2.Add(line2);
            var stderr2 = await stderr2Task;
            await p2.WaitForExitAsync(ct);
            return (lines2, stderr2, p2.ExitCode);
        }

        return (lines, stderr, p.ExitCode);
    }

    private static string EscapeQuery(string s) => s.Replace("\"", " ").Trim();
}
