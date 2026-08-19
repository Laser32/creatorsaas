using System.Diagnostics;
using System.Globalization;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

public class FFmpegService
{
    private readonly string _ffmpegPath;

    public FFmpegService(string ffmpegPath) => _ffmpegPath = ffmpegPath;

    public async Task RenderSceneAsync(Scene scene, string outputPath, IProgress<string>? log, CancellationToken ct)
    {
        if (scene.BRollPath == null || !File.Exists(scene.BRollPath))
            throw new InvalidOperationException($"Szene {scene.Order}: B-Roll fehlt");
        if (scene.AudioPath == null || !File.Exists(scene.AudioPath))
            throw new InvalidOperationException($"Szene {scene.Order}: Audio fehlt");

        log?.Report($"Render Szene {scene.Order} ({scene.DurationSeconds}s)...");

        var duration = await GetMediaDurationAsync(scene.AudioPath, ct);
        var actualDur = duration > 0 ? duration : scene.DurationSeconds;
        var durStr = actualDur.ToString("0.##", CultureInfo.InvariantCulture);

        log?.Report($"  Dauer: {durStr}s, Video: {Path.GetFileName(scene.BRollPath)}");

        // Loop video to match audio length, scale to 1920x1080, combine audio + video
        var args = $"-y -stream_loop -1 -i \"{scene.BRollPath}\" -i \"{scene.AudioPath}\" " +
                   $"-t {durStr} " +
                   "-vf \"scale=1920:1080:force_original_aspect_ratio=increase,crop=1920:1080,fps=30\" " +
                   "-c:v libx264 -preset veryfast -crf 23 -c:a aac -b:a 192k -shortest " +
                   $"\"{outputPath}\"";

        await RunAsync(args, ct);
        scene.SegmentPath = outputPath;
    }

    public async Task ConcatenateAsync(List<Scene> scenes, string outputPath, IProgress<string>? log, CancellationToken ct)
    {
        log?.Report("Setze finale Videodatei zusammen...");

        var listPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "concat_list.txt");
        var entries = scenes
            .OrderBy(s => s.Order)
            .Where(s => s.SegmentPath != null)
            .Select(s => $"file '{s.SegmentPath!.Replace("\\", "/").Replace("'", "'\\''")}'");
        await File.WriteAllLinesAsync(listPath, entries, ct);

        var args = $"-y -f concat -safe 0 -i \"{listPath}\" -c copy \"{outputPath}\"";
        await RunAsync(args, ct);

        try { File.Delete(listPath); } catch { }
    }

    /// <summary>
    /// Re-encodes a downloaded CC YouTube video to 1920x1080 30fps with AAC audio.
    /// The original audio track is kept (no ElevenLabs needed).
    /// </summary>
    public async Task ReencodeToStandardAsync(string inputPath, string outputPath,
        IProgress<string>? log, CancellationToken ct)
    {
        log?.Report($"  Re-encode: {Path.GetFileName(inputPath)} → 1920x1080...");

        var args = $"-y -i \"{inputPath}\" " +
                   "-vf \"scale=1920:1080:force_original_aspect_ratio=increase,crop=1920:1080,fps=30\" " +
                   "-c:v libx264 -preset veryfast -crf 23 -c:a aac -b:a 192k " +
                   $"\"{outputPath}\"";

        await RunAsync(args, ct);
    }

    /// <summary>
    /// Generates a dark-blue solid-colour video as a B-Roll placeholder when Pexels is
    /// unavailable. This lets the render pipeline continue even without a Pexels API key.
    /// </summary>
    public async Task GeneratePlaceholderAsync(Scene scene, string outputPath, IProgress<string>? log, CancellationToken ct)
    {
        log?.Report($"  Platzhalter-Video für Szene {scene.Order} (Pexels nicht verfügbar)...");
        var seconds = Math.Max(5, scene.DurationSeconds).ToString(CultureInfo.InvariantCulture);
        var args = $"-y -f lavfi -i color=c=0x1a1a2e:s=1920x1080:r=30 -t {seconds} " +
                   "-c:v libx264 -preset veryfast -crf 28 " +
                   $"\"{outputPath}\"";
        await RunAsync(args, ct);
        scene.BRollPath = outputPath;
    }

    public async Task GenerateThumbnailAsync(string videoPath, string title, string outputPath, CancellationToken ct)
    {
        var safe = title
            .Replace("\\", "\\\\")
            .Replace(":", "\\:")
            .Replace("'", "");
        if (safe.Length > 50) safe = safe.Substring(0, 50);

        // Try with drawtext overlay first; fall back to plain frame if libfreetype is missing.
        var argsWithText = $"-y -ss 3 -i \"{videoPath}\" -frames:v 1 " +
                   $"-vf \"scale=1280:720,drawtext=text='{safe}':fontcolor=white:fontsize=56:" +
                   "x=(w-text_w)/2:y=h-160:box=1:boxcolor=black@0.7:boxborderw=24\" " +
                   $"\"{outputPath}\"";

        var argsPlain = $"-y -ss 3 -i \"{videoPath}\" -frames:v 1 " +
                        $"-vf \"scale=1280:720\" \"{outputPath}\"";

        try
        {
            await RunAsync(argsWithText, ct);
        }
        catch
        {
            // drawtext failed (no libfreetype) — extract plain frame
            await RunAsync(argsPlain, ct);
        }
    }

    private async Task<double> GetMediaDurationAsync(string path, CancellationToken ct)
    {
        var ffprobe = Path.Combine(Path.GetDirectoryName(_ffmpegPath)!, "ffprobe.exe");
        if (!File.Exists(ffprobe)) return 0;

        var psi = new ProcessStartInfo
        {
            FileName = ffprobe,
            Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{path}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi)!;
        var output = await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        return double.TryParse(output.Trim(), CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    private async Task RunAsync(string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi)!;
        var stderr = await p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0)
        {
            // Show last 800 chars — the actual error is at the end, not in the version banner
            var tail = stderr.Length > 800 ? "..." + stderr[^800..] : stderr;
            throw new InvalidOperationException($"FFmpeg exit {p.ExitCode}: {tail}");
        }
    }
}
