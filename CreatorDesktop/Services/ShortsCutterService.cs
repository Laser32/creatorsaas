using System.Diagnostics;
using System.Globalization;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Cuts a finished video into 1-3 minute YouTube Shorts (9:16 vertical, 1080x1920).
/// Each shorts clip gets a blurred-background fill so 16:9 source fits the frame.
/// Picks N evenly-spaced segments from the source — the user can then trim/edit
/// further before upload if desired.
/// </summary>
public static class ShortsCutterService
{
    public record ShortsClip(string Path, double StartSeconds, double DurationSeconds);

    /// <summary>
    /// Cuts the given video into N short clips (default 8) of ~50 seconds each,
    /// each saved as 1080x1920 vertical MP4 in outputFolder.
    /// </summary>
    public static async Task<List<ShortsClip>> CutAsync(string videoPath, string outputFolder,
        int count, int durationSec, IProgress<string>? log, CancellationToken ct)
    {
        Directory.CreateDirectory(outputFolder);
        var ffmpegExe = Path.Combine(AppSettings.DefaultDataDir, "ffmpeg", "ffmpeg.exe");
        if (!File.Exists(ffmpegExe))
            throw new InvalidOperationException("ffmpeg fehlt — bitte einmal eine Doku erstellen, dann ist es da.");
        var ffprobe = Path.Combine(Path.GetDirectoryName(ffmpegExe)!, "ffprobe.exe");

        var total = await GetDurationAsync(ffprobe, videoPath, ct);
        if (total < 60)
            throw new InvalidOperationException($"Video zu kurz ({total:F0}s) für Shorts-Schnitt.");

        var clips = new List<ShortsClip>();
        // Distribute starts evenly, leaving 10s buffer at each end
        var usable = Math.Max(60, total - 20);
        var step = usable / count;
        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var start = 10 + i * step;
            var dur = Math.Min(durationSec, total - start - 1);
            if (dur < 15) break;

            var outPath = Path.Combine(outputFolder, $"short_{i + 1:D2}.mp4");
            log?.Report($"  Schneide Short {i + 1}/{count} (Start {FormatTime(start)})...");
            await CutOneAsync(ffmpegExe, videoPath, start, dur, outPath, ct);
            clips.Add(new ShortsClip(outPath, start, dur));
        }
        return clips;
    }

    private static async Task CutOneAsync(string ffmpeg, string src, double start, double dur,
        string outPath, CancellationToken ct)
    {
        // Vertical 1080x1920 with blurred fill behind the original 16:9 frame
        var vf = "[0:v]split=2[bg][main];" +
                 "[bg]scale=1080:1920:force_original_aspect_ratio=increase,crop=1080:1920,boxblur=24:6[bg];" +
                 "[main]scale=1080:-1[scaled];" +
                 "[bg][scaled]overlay=(W-w)/2:(H-h)/2[v]";

        var args = $"-y -ss {start.ToString(CultureInfo.InvariantCulture)} -i \"{src}\" " +
                   $"-t {dur.ToString(CultureInfo.InvariantCulture)} " +
                   $"-filter_complex \"{vf}\" -map \"[v]\" -map 0:a? " +
                   "-c:v libx264 -preset veryfast -crf 23 -c:a aac -b:a 128k " +
                   $"\"{outPath}\"";

        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg, Arguments = args,
            RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        using var p = Process.Start(psi)!;
        var stderr = await p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"FFmpeg exit {p.ExitCode}: {stderr[^Math.Min(400, stderr.Length)..]}");
    }

    private static async Task<double> GetDurationAsync(string ffprobe, string path, CancellationToken ct)
    {
        if (!File.Exists(ffprobe)) return 0;
        var psi = new ProcessStartInfo
        {
            FileName = ffprobe,
            Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{path}\"",
            RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
        };
        using var p = Process.Start(psi)!;
        var output = await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        return double.TryParse(output.Trim(), CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    private static string FormatTime(double seconds) =>
        TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
}
