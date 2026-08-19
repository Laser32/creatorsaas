using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Builds YouTube chapter timestamps ("00:00 Intro\n02:15 Hintergrund...") from
/// a VideoJob's scenes. YouTube auto-detects chapters when the description starts
/// with "00:00" on its own line followed by more timestamps.
/// </summary>
public static class ChapterBuilderService
{
    /// <summary>
    /// Returns a chapter block ready to paste at the top of a video description.
    /// First entry MUST be "00:00" — otherwise YouTube ignores all chapters.
    /// </summary>
    public static string Build(VideoJob job)
    {
        if (job.Scenes.Count < 2) return "";
        var lines = new List<string>();
        double cursor = 0;
        for (int i = 0; i < job.Scenes.Count; i++)
        {
            var s = job.Scenes[i];
            var ts = i == 0 ? "00:00" : FormatTimestamp(cursor);
            var title = string.IsNullOrWhiteSpace(s.Title) ? $"Kapitel {i + 1}" : s.Title.Trim();
            if (title.Length > 60) title = title[..60].TrimEnd() + "…";
            lines.Add($"{ts} {title}");

            // Use chapter duration when available, else fall back to a reasonable default
            var dur = EstimateSceneSeconds(s);
            cursor += dur;
        }
        return string.Join("\n", lines);
    }

    /// <summary>Inserts the chapter block at the very top of the description.</summary>
    public static string PrependChapters(VideoJob job, string description)
    {
        var chapters = Build(job);
        if (string.IsNullOrEmpty(chapters)) return description;
        return chapters + "\n\n" + description;
    }

    private static double EstimateSceneSeconds(Scene s)
    {
        if (s.DurationSeconds > 0) return s.DurationSeconds;
        var words = (s.Narration ?? "").Split(' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        return Math.Max(15, words / 3.0);
    }

    private static string FormatTimestamp(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return t.Hours > 0
            ? $"{t.Hours}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{t.Minutes:D2}:{t.Seconds:D2}";
    }
}
