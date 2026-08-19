using System.Text.Json;
using System.Web;

namespace CreatorDesktop.Services;

/// <summary>
/// Searches the Internet Archive (archive.org) for public-domain films,
/// documentaries and historical footage. Uses the free advancedsearch JSON API —
/// no key, no quota, no bot checks. Downloads happen through yt-dlp which has
/// built-in archive.org support.
/// </summary>
public class ArchiveOrgService
{
    public enum LicenseClass
    {
        Unknown,            // grey  — license not specified
        PublicDomain,       // green — fully free
        CreativeCommons,    // green — free with attribution
        NonCommercial,      // yellow — CC-BY-NC etc, problematic for monetized YT
        Restricted          // red   — all rights reserved / unclear
    }

    public record ArchiveItem(
        string Identifier,
        string Title,
        string Description,
        int? Year,
        string Creator,
        int DurationSeconds,
        string LicenseUrl,
        string Collection,
        LicenseClass License);

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// Searches movies/films on archive.org. yearFrom/yearTo are optional —
    /// e.g. (1900, 1928) returns only definitely-public-domain content.
    /// </summary>
    public async Task<List<ArchiveItem>> SearchAsync(string query, int maxResults,
        int? yearFrom, int? yearTo, CancellationToken ct, bool germanOnly = false)
    {
        var q = "mediatype:(movies)";
        if (!string.IsNullOrWhiteSpace(query))
            q += $" AND ({query})";
        if (yearFrom.HasValue || yearTo.HasValue)
            q += $" AND year:[{yearFrom ?? 1850} TO {yearTo ?? DateTime.Now.Year}]";
        if (germanOnly)
            q += " AND language:(german OR ger OR deu OR deutsch)";

        var encoded = HttpUtility.UrlEncode(q);
        var url = $"https://archive.org/advancedsearch.php?q={encoded}" +
                  "&fl[]=identifier&fl[]=title&fl[]=description&fl[]=year" +
                  "&fl[]=creator&fl[]=runtime&fl[]=licenseurl&fl[]=collection" +
                  $"&rows={maxResults}&page=1&output=json&sort[]=downloads+desc";

        var json = await _http.GetStringAsync(url, ct);
        var results = new List<ArchiveItem>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("response", out var resp)) return results;
            if (!resp.TryGetProperty("docs", out var docs)) return results;

            foreach (var item in docs.EnumerateArray())
            {
                var id = item.TryGetProperty("identifier", out var idEl) ? idEl.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(id)) continue;

                var title = item.TryGetProperty("title", out var tEl) ? GetStringOrFirst(tEl) : "";
                var desc = item.TryGetProperty("description", out var dEl) ? GetStringOrFirst(dEl) : "";
                if (desc.Length > 300) desc = desc[..300] + "…";

                int? year = null;
                if (item.TryGetProperty("year", out var yEl))
                {
                    if (yEl.ValueKind == JsonValueKind.Number && yEl.TryGetInt32(out var yi)) year = yi;
                    else if (yEl.ValueKind == JsonValueKind.String && int.TryParse(yEl.GetString(), out var ys)) year = ys;
                }

                var creator = item.TryGetProperty("creator", out var cEl) ? GetStringOrFirst(cEl) : "";

                int dur = 0;
                if (item.TryGetProperty("runtime", out var rEl))
                {
                    var runtimeStr = GetStringOrFirst(rEl);
                    dur = ParseRuntime(runtimeStr);
                }

                var licenseUrl = item.TryGetProperty("licenseurl", out var lEl) ? GetStringOrFirst(lEl) : "";
                var collection = "";
                if (item.TryGetProperty("collection", out var colEl))
                {
                    collection = colEl.ValueKind == JsonValueKind.Array
                        ? string.Join(",", colEl.EnumerateArray().Select(e => e.GetString() ?? ""))
                        : (colEl.GetString() ?? "");
                }

                var licenseClass = ClassifyLicense(licenseUrl, collection);
                results.Add(new ArchiveItem(id, title, desc, year, creator, dur,
                    licenseUrl, collection, licenseClass));
            }
        }
        catch { /* malformed JSON — return what we have */ }
        return results;
    }

    /// <summary>
    /// Downloads an archive.org item as MP4 via yt-dlp (which handles archive.org natively).
    /// Returns the absolute path of the downloaded file.
    /// </summary>
    public static async Task<string> DownloadAsync(string identifier, string outputDir,
        IProgress<string>? log, CancellationToken ct)
    {
        var ytDlp = await YtDlpDownloader.EnsureAsync(log, ct);
        Directory.CreateDirectory(outputDir);
        var outTemplate = Path.Combine(outputDir, $"ia_{identifier}.%(ext)s");
        var url = $"https://archive.org/details/{identifier}";

        var args = $"-f \"best[ext=mp4]/best\" --no-warnings " +
                   $"-o \"{outTemplate}\" \"{url}\"";

        var (lines, stderr, exit) = await YtDlpDownloader.RunYtDlpAsync(ytDlp, args, log, ct);
        foreach (var l in lines) log?.Report("  " + l);
        if (exit != 0)
            throw new InvalidOperationException($"yt-dlp Fehler (exit {exit}): {stderr}");

        var any = Directory.GetFiles(outputDir, $"ia_{identifier}.*").FirstOrDefault();
        if (any != null) return any;
        throw new InvalidOperationException($"Archive.org-Download fehlgeschlagen: {identifier}");
    }

    /// <summary>
    /// Maps a licenseurl + collection list to one of our 5 categories.
    /// Known-safe collections (prelinger, nasa, classic_tv etc.) override missing license.
    /// </summary>
    public static LicenseClass ClassifyLicense(string licenseUrl, string collection)
    {
        var lu = (licenseUrl ?? "").ToLowerInvariant();
        var col = (collection ?? "").ToLowerInvariant();

        // Explicit license URL wins
        if (lu.Contains("publicdomain") || lu.Contains("/zero/") || lu.Contains("mark/"))
            return LicenseClass.PublicDomain;
        if (lu.Contains("/by-nc") || lu.Contains("/by-nd") || lu.Contains("noncommercial"))
            return LicenseClass.NonCommercial;
        if (lu.Contains("creativecommons.org") || lu.Contains("/cc-by") || lu.Contains("/by/"))
            return LicenseClass.CreativeCommons;

        // No license URL — trust well-known PD collections
        if (col.Contains("prelinger") || col.Contains("nasa") || col.Contains("classic_tv") ||
            col.Contains("newsreels") || col.Contains("feature_films") ||
            col.Contains("publicmovies") || col.Contains("ephemeral_films") ||
            col.Contains("opensource_movies"))
            return LicenseClass.PublicDomain;

        return LicenseClass.Unknown;
    }

    private static string GetStringOrFirst(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString() ?? "",
        JsonValueKind.Array  => el.EnumerateArray().FirstOrDefault().GetString() ?? "",
        _ => ""
    };

    // archive.org runtime is HH:MM:SS or MM:SS — convert to seconds.
    private static int ParseRuntime(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        var parts = s.Split(':');
        int total = 0;
        foreach (var p in parts)
        {
            total = total * 60 + (int.TryParse(p, out var n) ? n : 0);
        }
        return total;
    }
}
