using System.Diagnostics;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Prepares TikTok-ready vertical clips and stages them in a folder so the user
/// can drag-drop them on the TikTok web upload page. TikTok's Content Posting API
/// requires per-app approval that's not realistic for personal use, so we go the
/// semi-automatic route: convert to 9:16 vertical, write metadata, open the upload URL.
/// </summary>
public class TikTokExportService
{
    private readonly string _ffmpegPath;

    public TikTokExportService(string ffmpegPath) => _ffmpegPath = ffmpegPath;

    /// <summary>
    /// Reformats <paramref name="sourceVideo"/> to 1080x1920 vertical (with blurred
    /// background fill so the source video stays uncropped) and writes a description
    /// .txt next to the output. Returns the path to the exported video.
    /// </summary>
    public async Task<string> ExportVerticalAsync(string sourceVideo, string title, string description,
        string targetFolder, IProgress<string>? log, CancellationToken ct)
    {
        Directory.CreateDirectory(targetFolder);
        var name = Path.GetFileNameWithoutExtension(sourceVideo);
        var outVideo = Path.Combine(targetFolder, $"{name}_tiktok.mp4");
        var outTxt = Path.Combine(targetFolder, $"{name}_tiktok.txt");

        // FFmpeg: blurred background fill behind centered original — works for any aspect ratio.
        // 1080x1920 portrait, 30fps, h264, aac.
        var filter =
            "[0:v]scale=1080:1920:force_original_aspect_ratio=increase,boxblur=20:1,crop=1080:1920[bg];" +
            "[0:v]scale=1080:1920:force_original_aspect_ratio=decrease[fg];" +
            "[bg][fg]overlay=(W-w)/2:(H-h)/2";

        var args = $"-y -i \"{sourceVideo}\" -filter_complex \"{filter}\" " +
                   "-c:v libx264 -preset veryfast -crf 22 -r 30 " +
                   "-c:a aac -b:a 128k -ar 44100 -ac 2 " +
                   $"-movflags +faststart \"{outVideo}\"";

        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath, Arguments = args,
            RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        log?.Report($"  TikTok: konvertiere {Path.GetFileName(sourceVideo)} → vertikal...");
        using var p = Process.Start(psi)!;
        var stderr = await p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"FFmpeg-Fehler bei TikTok-Export: {stderr[..Math.Min(500, stderr.Length)]}");

        var captionText =
            $"{title}\n\n" +
            $"{description}\n\n" +
            "#fyp #viral #foryoupage #foryou";
        await File.WriteAllTextAsync(outTxt, captionText, ct);

        log?.Report($"  TikTok: bereit → {Path.GetFileName(outVideo)}");
        return outVideo;
    }

    /// <summary>Opens the TikTok web upload page so the user can drop the prepared file.</summary>
    public static void OpenUploadPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "https://www.tiktok.com/upload", UseShellExecute = true });
        }
        catch { /* user can open it manually */ }
    }
}
