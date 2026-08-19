using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Downloads yt-dlp.exe on first use (single-file, ~11 MB from GitHub releases) and keeps
/// it up to date. yt-dlp is needed to download Creative Commons YouTube videos with their
/// original audio.
///
/// YouTube changes its player API every few weeks. A yt-dlp.exe that was downloaded once
/// and never refreshed starts failing with "HTTP Error 403: Forbidden" — the metadata still
/// extracts fine, but the actual media URLs are rejected. That is why EnsureAsync runs a
/// periodic self-update and why RunYtDlpAsync retries a 403 with other player clients.
/// </summary>
public static class YtDlpDownloader
{
    private static readonly string YtDlpPath =
        Path.Combine(AppSettings.DefaultDataDir, "yt-dlp.exe");

    /// <summary>Timestamp file recording the last successful update check.</summary>
    private static readonly string UpdateStampPath =
        Path.Combine(AppSettings.DefaultDataDir, "yt-dlp.updated");

    /// <summary>How often EnsureAsync checks for a new yt-dlp release.</summary>
    private const int UpdateIntervalDays = 7;

    /// <summary>
    /// Player clients tried in order when YouTube blocks a download. Each client uses a
    /// different player API and YouTube blocks them individually, so a client that fails
    /// today often works again next week. Clients unknown to the installed yt-dlp are skipped.
    ///
    /// DropCookies matters as much as the client: account cookies make YouTube treat the
    /// request as logged in, and it then demands a proof-of-origin token that yt-dlp cannot
    /// produce on its own. Anonymous attempts frequently succeed where the same client fails
    /// with cookies attached, so both variants are in the list.
    /// </summary>
    private static readonly (string Client, bool DropCookies)[] PlayerClientFallbacks =
    {
        ("tv",          false),
        ("web_safari",  false),
        ("ios",         true),
        ("mweb",        false),
        ("tv_embedded", true),
        ("web",         true),
    };

    private static bool _updateCheckedThisSession;
    private static bool _updateRanThisSession;

    /// <summary>
    /// Returns the appropriate --cookies-from-browser or --cookies argument so every
    /// yt-dlp call can bypass YouTube's "Sign in to confirm you're not a bot" check.
    /// Priority: YtDlpCookiesBrowser (e.g. "edge") → YtDlpCookiesPath (file) → empty.
    /// </summary>
    public static string CookieArg()
    {
        var browser = ReadStringSetting("YtDlpCookiesBrowser");
        if (!string.IsNullOrWhiteSpace(browser))
            return $"--cookies-from-browser {browser} ";

        var cookies = ReadStringSetting("YtDlpCookiesPath");
        if (!string.IsNullOrWhiteSpace(cookies) && File.Exists(cookies))
            return $"--cookies \"{cookies}\" ";

        return "";
    }

    public static async Task<string> EnsureAsync(IProgress<string>? log, CancellationToken ct)
    {
        if (File.Exists(YtDlpPath))
        {
            await MaybeUpdateAsync(log, ct);
            return YtDlpPath;
        }

        log?.Report("Lade yt-dlp.exe herunter (einmalig, ~11 MB)...");
        await DownloadLatestAsync(ct);
        MarkUpdateChecked();
        log?.Report("yt-dlp.exe bereit.");
        return YtDlpPath;
    }

    /// <summary>Fetches the current yt-dlp.exe from GitHub releases, replacing any existing copy.</summary>
    private static async Task DownloadLatestAsync(CancellationToken ct)
    {
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
    }

    /// <summary>
    /// Runs the update check at most once per session and at most every UpdateIntervalDays.
    /// Set "YtDlpAutoUpdate": false in settings.json to skip it entirely.
    /// </summary>
    private static async Task MaybeUpdateAsync(IProgress<string>? log, CancellationToken ct)
    {
        if (_updateCheckedThisSession) return;
        _updateCheckedThisSession = true;

        if (!ReadBoolSetting("YtDlpAutoUpdate", true)) return;

        var last = LastUpdateCheckUtc();
        if (last != null && (DateTime.UtcNow - last.Value).TotalDays < UpdateIntervalDays)
            return;

        await UpdateAsync(log, ct);
    }

    /// <summary>
    /// Updates yt-dlp.exe in place (yt-dlp -U) and clears its player cache. Falls back to a
    /// fresh download from GitHub when the self-update cannot replace the file (locked,
    /// missing permissions, or a build without the updater). Returns true if yt-dlp is
    /// current afterwards. Never throws — an update failure must not kill the download.
    /// </summary>
    public static async Task<bool> UpdateAsync(IProgress<string>? log, CancellationToken ct)
    {
        _updateRanThisSession = true;
        // The updater talks to GitHub; cap it so a hanging request cannot block the pipeline.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));

        try
        {
            log?.Report("  Pruefe auf yt-dlp-Update...");
            var (lines, stderr, exit) = await ExecAsync(YtDlpPath, "-U", timeout.Token);

            if (exit != 0)
            {
                log?.Report("  Selbst-Update fehlgeschlagen — lade yt-dlp.exe neu herunter...");
                await DownloadLatestAsync(timeout.Token);
            }
            else
            {
                var status = lines.LastOrDefault(l => l.Contains("yt-dlp", StringComparison.OrdinalIgnoreCase));
                log?.Report("  " + (status ?? "yt-dlp ist aktuell."));
                if (!string.IsNullOrWhiteSpace(stderr))
                    log?.Report("  " + stderr.Trim());
            }

            // A stale player cache reproduces the same 403 even with a fresh binary.
            await ExecAsync(YtDlpPath, "--rm-cache-dir", timeout.Token);
            MarkUpdateChecked();
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;   // user cancelled — propagate
        }
        catch (Exception ex)
        {
            log?.Report($"  yt-dlp-Update fehlgeschlagen: {ex.Message}");
            return false;
        }
    }

    private static DateTime? LastUpdateCheckUtc()
    {
        try
        {
            if (!File.Exists(UpdateStampPath)) return null;
            var text = File.ReadAllText(UpdateStampPath).Trim();
            return DateTime.TryParse(text, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToUniversalTime()
                : (DateTime?)null;
        }
        catch { return null; }
    }

    private static void MarkUpdateChecked()
    {
        try
        {
            Directory.CreateDirectory(AppSettings.DefaultDataDir);
            File.WriteAllText(UpdateStampPath, DateTime.UtcNow.ToString("o"));
        }
        catch { /* stamp is best-effort — a missing stamp only means we check again */ }
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
            throw new InvalidOperationException($"yt-dlp Fehler: {DescribeFailure(stderr)}");

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
                   // Single fragments are dropped under load — retry them before giving up.
                   $"--retries 10 --fragment-retries 10 " +
                   $"--no-warnings --no-playlist " +
                   $"-o \"{outTemplate}\" " +
                   $"\"{url}\"";

        var (dlLines, stderr, exit) = await RunYtDlpAsync(ytDlp, args, log, ct);
        foreach (var l in dlLines) log?.Report("  " + l);

        if (exit != 0)
            throw new InvalidOperationException($"yt-dlp Fehler (exit {exit}): {DescribeFailure(stderr)}");

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
    /// Runs yt-dlp with the given args and recovers from the three failures YouTube throws
    /// at us: a locked browser cookie DB, an age gate, and a 403 on the media URLs.
    /// Returns (stdout lines, stderr, exit code) of the last attempt.
    /// </summary>
    public static async Task<(List<string> lines, string stderr, int exit)> RunYtDlpAsync(
        string ytDlp, string args, IProgress<string>? log, CancellationToken ct)
    {
        var result = await ExecAsync(ytDlp, args, ct);
        if (result.exit == 0) return result;

        // Browser cookie errors — DB locked, DPAPI session mismatch, etc. — retry without cookies
        if (IsCookieError(result.stderr) || IsAgeError(result.stderr))
        {
            var cleaned = StripCookieArgs(args);
            if (IsAgeError(result.stderr) && !cleaned.Contains("player_client"))
            {
                // tv_embedded serves age-restricted videos without a login.
                cleaned = "--extractor-args \"youtube:player_client=tv_embedded,web_safari\" " + cleaned;
                log?.Report("  Hinweis: Altersbeschraenkung — versuche tv_embedded Player...");
            }
            else
            {
                log?.Report("  Hinweis: Browser-Cookies gesperrt — versuche ohne Cookies...");
            }

            result = await ExecAsync(ytDlp, cleaned, ct);
            if (result.exit == 0) return result;
            args = cleaned;   // keep going without the broken cookies
        }

        if (IsForbidden(result.stderr))
            result = await RetryForbiddenAsync(ytDlp, args, result, log, ct);

        return result;
    }

    /// <summary>
    /// Recovers from "HTTP Error 403: Forbidden". Two causes, tried in that order:
    /// an outdated yt-dlp (by far the most common — YouTube's player changed under it),
    /// and a player client that YouTube currently blocks without a proof-of-origin token.
    /// </summary>
    private static async Task<(List<string> lines, string stderr, int exit)> RetryForbiddenAsync(
        string ytDlp, string args, (List<string> lines, string stderr, int exit) last,
        IProgress<string>? log, CancellationToken ct)
    {
        if (!_updateRanThisSession)
        {
            log?.Report("  HTTP 403 — yt-dlp ist vermutlich veraltet, aktualisiere...");
            if (await UpdateAsync(log, ct))
            {
                var retry = await ExecAsync(ytDlp, args, ct);
                if (retry.exit == 0)
                {
                    log?.Report("  ✓ Nach dem Update erfolgreich.");
                    return retry;
                }
                last = retry;
                if (IsTerminalError(retry.stderr)) return retry;   // the video is gone, not blocked
            }
        }

        // Caller already pinned a client (age-gate path) — don't fight it.
        if (args.Contains("player_client")) return last;

        foreach (var (client, dropCookies) in PlayerClientFallbacks)
        {
            ct.ThrowIfCancellationRequested();
            var attemptArgs = dropCookies ? StripCookieArgs(args) : args;
            log?.Report($"  Blockiert — versuche Player-Client '{client}'" +
                        (dropCookies ? " (ohne Cookies)..." : "..."));

            var retry = await ExecAsync(ytDlp, $"--extractor-args \"youtube:player_client={client}\" " + attemptArgs, ct);
            if (retry.exit == 0)
            {
                log?.Report($"  ✓ Player-Client '{client}' funktioniert.");
                return retry;
            }

            // This yt-dlp build doesn't know the client — not a real result, try the next one.
            if (IsUnknownClientError(retry.stderr)) continue;

            last = retry;

            // Every client answers a block differently ("403", "page needs to be reloaded",
            // "sign in to confirm"...). Only the video itself being gone ends the chain —
            // anything else is worth trying on the next client.
            if (IsTerminalError(retry.stderr)) return retry;
        }

        return last;
    }

    /// <summary>
    /// Starts yt-dlp once and collects stdout/stderr. Every call and its result go to the
    /// log file — the command line and full stderr are what a later diagnosis needs, and
    /// neither of them reaches the on-screen log.
    /// </summary>
    private static async Task<(List<string> lines, string stderr, int exit)> ExecAsync(
        string ytDlp, string args, CancellationToken ct)
    {
        RunLog.Write($"→ yt-dlp {args}");
        var started = DateTime.Now;

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

        var seconds = (DateTime.Now - started).TotalSeconds;
        RunLog.Write($"← exit {p.ExitCode} nach {seconds:0.0}s, {lines.Count} Zeilen stdout");
        if (p.ExitCode != 0)
        {
            RunLog.WriteBlock("  stderr:", stderr);
            // --dump-json floods stdout with one huge line; the tail is enough to place the error.
            var tail = lines.TakeLast(5).Select(l => l.Length > 300 ? l[..300] + " […]" : l);
            RunLog.WriteBlock("  stdout (letzte Zeilen):", string.Join("\n", tail));
        }

        return (lines, stderr, p.ExitCode);
    }

    private static bool IsCookieError(string stderr) =>
        stderr.Contains("could not copy", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("database is locked", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("failed to decrypt", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("dpapi", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("cookies-from-browser", StringComparison.OrdinalIgnoreCase);

    private static bool IsAgeError(string stderr) =>
        stderr.Contains("confirm your age", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("age-restricted", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("inappropriate for some users", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True for the whole 403 family: the media URLs are rejected, the player response is
    /// missing, or YouTube demands a bot check — all fixed by the same update/client retry.
    /// </summary>
    private static bool IsForbidden(string stderr) =>
        stderr.Contains("HTTP Error 403", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("403: Forbidden", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("unable to download video data", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("not a bot", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("failed to extract any player response", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("nsig extraction failed", StringComparison.OrdinalIgnoreCase) ||
        // The tv client's way of saying it needs a proof-of-origin token.
        stderr.Contains("page needs to be reloaded", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("PO Token", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when the video itself is the problem, so trying another player client is pointless.
    /// Everything else — however YouTube words the block — ends up on the next client.
    /// </summary>
    private static bool IsTerminalError(string stderr) =>
        stderr.Contains("Private video", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("Video unavailable", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("members-only", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("has been removed", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("account associated with this video has been terminated", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("not available in your country", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("Unsupported URL", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("Incomplete YouTube ID", StringComparison.OrdinalIgnoreCase);

    private static bool IsUnknownClientError(string stderr) =>
        stderr.Contains("player_client", StringComparison.OrdinalIgnoreCase) &&
        (stderr.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
         stderr.Contains("unsupported", StringComparison.OrdinalIgnoreCase) ||
         stderr.Contains("unknown", StringComparison.OrdinalIgnoreCase));

    /// <summary>Removes both cookie flavours so a retry cannot trip over the same broken cookies.</summary>
    private static string StripCookieArgs(string args)
    {
        args = System.Text.RegularExpressions.Regex.Replace(
            args, @"--cookies-from-browser\s+\S+\s*", "");
        return System.Text.RegularExpressions.Regex.Replace(
            args, @"--cookies\s+""[^""]*""\s*", "");
    }

    /// <summary>Turns yt-dlp's stderr into something the user can act on.</summary>
    private static string DescribeFailure(string stderr)
    {
        var s = stderr.Trim();

        if (IsForbidden(s))
            return "YouTube blockiert den Download. yt-dlp ist aktuell und alle Player-Clients " +
                   "wurden mit und ohne Cookies erfolglos durchprobiert — YouTube verlangt fuer " +
                   "dieses Video einen PO-Token. Moeglichkeiten: spaeter erneut versuchen (die " +
                   "Sperren wechseln staendig), ein anderes Video nehmen, oder einen PO-Token-" +
                   $"Provider als yt-dlp-Plugin installieren.\n{s}";

        if (s.Contains("Private video", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Video unavailable", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("members-only", StringComparison.OrdinalIgnoreCase))
            return $"Video ist nicht (mehr) oeffentlich abrufbar.\n{s}";

        if (IsAgeError(s))
            return $"Altersbeschraenktes Video — Cookies eines eingeloggten Kontos noetig.\n{s}";

        return s;
    }

    private static string? ReadStringSetting(string key)
    {
        try
        {
            var path = Path.Combine(AppSettings.DefaultDataDir, "settings.json");
            if (!File.Exists(path)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String
                ? el.GetString()
                : null;
        }
        catch { return null; }
    }

    private static bool ReadBoolSetting(string key, bool fallback)
    {
        try
        {
            var path = Path.Combine(AppSettings.DefaultDataDir, "settings.json");
            if (!File.Exists(path)) return fallback;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty(key, out var el) &&
                (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False))
                return el.GetBoolean();
        }
        catch { /* fall through */ }
        return fallback;
    }

    private static string EscapeQuery(string s) => s.Replace("\"", " ").Trim();
}
