using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Appends everything the app logs to a daily file under %LocalAppData%\CreatorDesktop\logs\.
///
/// The on-screen log is a summary and it is gone when the app closes. Failures like YouTube's
/// 403 need the parts that never reach the UI: the exact yt-dlp command line, its full stderr,
/// and which fallback attempt produced which result. That is what lands here, so a failed run
/// can be reconstructed afterwards from one file.
/// </summary>
public static class RunLog
{
    private static readonly object Gate = new();
    private static bool _cleaned;

    /// <summary>Log files older than this are deleted on the first write of a session.</summary>
    private const int KeepDays = 14;

    public static string Dir => Path.Combine(AppSettings.DefaultDataDir, "logs");

    public static string TodayPath =>
        Path.Combine(Dir, $"creatordesktop-{DateTime.Now:yyyy-MM-dd}.log");

    /// <summary>Writes one timestamped line. Never throws — logging must not break a run.</summary>
    public static void Write(string line)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Dir);
                if (!_cleaned) { _cleaned = true; CleanupOldFiles(); }
                File.AppendAllText(TodayPath, $"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
            }
        }
        catch { /* a broken log must never surface as an error */ }
    }

    /// <summary>Writes a multi-line block (command output, stack trace) indented under a header.</summary>
    public static void WriteBlock(string header, string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) { Write(header); return; }

        var indented = string.Join(Environment.NewLine,
            body.Replace("\r\n", "\n").Split('\n').Select(l => "    " + l));
        Write(header + Environment.NewLine + indented);
    }

    /// <summary>Marks the start of a run so the file stays readable across several sessions.</summary>
    public static void WriteSessionHeader(string appVersion)
    {
        Write(new string('═', 70));
        Write($"CreatorDesktop {appVersion} gestartet — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Write($"Datenordner: {AppSettings.DefaultDataDir}");
        Write(new string('═', 70));
    }

    private static void CleanupOldFiles()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-KeepDays);
            foreach (var f in Directory.GetFiles(Dir, "creatordesktop-*.log"))
                if (File.GetLastWriteTime(f) < cutoff) File.Delete(f);
        }
        catch { /* best effort */ }
    }
}
