using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Matches today's date against the seasonal.json calendar. Returns active
/// entries (within ±7 days of an anniversary, or inside a month-window).
/// </summary>
public static class SeasonalSuggesterService
{
    public record Hit(SeasonalEntry Entry, string Reason);

    public static List<Hit> GetActiveSuggestions(DateTime today)
    {
        var entries = AppSettings.LoadSeasonal();
        var hits = new List<Hit>();

        foreach (var e in entries)
        {
            // Anniversary check (±7 days)
            if (!string.IsNullOrWhiteSpace(e.Anniversary))
            {
                var parts = e.Anniversary.Split('-');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var d))
                {
                    try
                    {
                        var anniv = new DateTime(today.Year, m, d);
                        var days = (anniv - today.Date).Days;
                        if (Math.Abs(days) <= 7)
                        {
                            var reason = days == 0 ? "heute"
                                : days > 0 ? $"in {days} Tag(en)"
                                : $"vor {-days} Tag(en)";
                            hits.Add(new Hit(e, $"Jahrestag {reason}"));
                            continue;
                        }
                    }
                    catch { /* invalid date — skip */ }
                }
            }

            // Seasonal window check
            if (e.MonthStart.HasValue && e.MonthEnd.HasValue)
            {
                if (InRange(today.Month, e.MonthStart.Value, e.MonthEnd.Value))
                    hits.Add(new Hit(e, $"Saison Monat {e.MonthStart}–{e.MonthEnd}"));
            }
        }
        return hits;
    }

    // Supports wraparound (e.g. Nov–Feb = 11..2)
    private static bool InRange(int month, int start, int end)
    {
        if (start <= end) return month >= start && month <= end;
        return month >= start || month <= end;
    }
}
