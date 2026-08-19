using System.IO.Compression;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

public static class FFmpegDownloader
{
    // Primary: GitHub release (stable, always available)
    // Fallback: gyan.dev essentials build
    private const string DownloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
    private const string FallbackUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

    public static async Task<string> EnsureAsync(AppSettings settings, IProgress<string>? log, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(settings.FFmpegPath) && File.Exists(settings.FFmpegPath))
            return settings.FFmpegPath;

        var ffmpegDir = Path.Combine(AppSettings.DefaultDataDir, "ffmpeg");
        var expected = Path.Combine(ffmpegDir, "ffmpeg.exe");

        if (File.Exists(expected))
        {
            settings.FFmpegPath = expected;
            settings.Save();
            return expected;
        }

        log?.Report("FFmpeg nicht gefunden, lade herunter (~120 MB)...");
        Directory.CreateDirectory(ffmpegDir);

        var zipPath = Path.Combine(ffmpegDir, "ffmpeg.zip");
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("CreatorDesktop");

        foreach (var url in new[] { DownloadUrl, FallbackUrl })
        {
            try
            {
                log?.Report($"  Quelle: {url}");
                using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                resp.EnsureSuccessStatusCode();
                await using var fs = File.Create(zipPath);
                await resp.Content.CopyToAsync(fs, ct);
                break; // success
            }
            catch (Exception ex)
            {
                log?.Report($"  Fehler: {ex.Message} — versuche nächste Quelle...");
                if (File.Exists(zipPath)) File.Delete(zipPath);
                if (url == FallbackUrl) throw;
            }
        }

        log?.Report("Entpacke FFmpeg...");
        var extractDir = Path.Combine(ffmpegDir, "extracted");
        if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
        ZipFile.ExtractToDirectory(zipPath, extractDir);

        var found = Directory.GetFiles(extractDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (found == null)
            throw new InvalidOperationException("ffmpeg.exe nicht im Download-Archiv gefunden");

        // Also need ffprobe.exe alongside
        var ffprobeSrc = Path.Combine(Path.GetDirectoryName(found)!, "ffprobe.exe");
        File.Copy(found, expected, true);
        if (File.Exists(ffprobeSrc))
            File.Copy(ffprobeSrc, Path.Combine(ffmpegDir, "ffprobe.exe"), true);

        File.Delete(zipPath);
        try { Directory.Delete(extractDir, true); } catch { }

        settings.FFmpegPath = expected;
        settings.Save();
        log?.Report("FFmpeg bereit.");
        return expected;
    }
}
