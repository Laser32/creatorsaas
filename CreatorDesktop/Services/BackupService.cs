using System.IO.Compression;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Packs all CreatorDesktop state (settings.json, playlists.json, competitors.json,
/// wishes.json) into a single ZIP. Restore extracts and overwrites the existing files.
/// </summary>
public static class BackupService
{
    private static readonly string[] FilesToBackup =
    [
        "settings.json",
        "playlists.json",
        "competitors.json",
        "wishes.json"
    ];

    public static string CreateBackup(string targetFolder)
    {
        Directory.CreateDirectory(targetFolder);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var zipPath = Path.Combine(targetFolder, $"creatordesktop_backup_{stamp}.zip");

        using var fs = File.Create(zipPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        foreach (var name in FilesToBackup)
        {
            var src = Path.Combine(AppSettings.DefaultDataDir, name);
            if (!File.Exists(src)) continue;
            zip.CreateEntryFromFile(src, name, CompressionLevel.Optimal);
        }
        return zipPath;
    }

    public static int RestoreBackup(string zipPath)
    {
        if (!File.Exists(zipPath))
            throw new FileNotFoundException("Backup-Datei nicht gefunden.", zipPath);

        Directory.CreateDirectory(AppSettings.DefaultDataDir);
        using var zip = ZipFile.OpenRead(zipPath);
        int restored = 0;
        foreach (var entry in zip.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;
            // Only restore files we know about — protects against path traversal
            if (!FilesToBackup.Contains(entry.Name)) continue;
            var dest = Path.Combine(AppSettings.DefaultDataDir, entry.Name);
            entry.ExtractToFile(dest, overwrite: true);
            restored++;
        }
        return restored;
    }

    /// <summary>Deletes backups in the folder older than keepDays.</summary>
    public static int PruneOldBackups(string folder, int keepDays)
    {
        if (!Directory.Exists(folder)) return 0;
        var cutoff = DateTime.Now.AddDays(-keepDays);
        int deleted = 0;
        foreach (var file in Directory.GetFiles(folder, "creatordesktop_backup_*.zip"))
        {
            try
            {
                if (File.GetCreationTime(file) < cutoff)
                {
                    File.Delete(file);
                    deleted++;
                }
            }
            catch { /* keep going */ }
        }
        return deleted;
    }

    public static List<(string Path, DateTime Date, long SizeBytes)> ListBackups(string folder)
    {
        var list = new List<(string, DateTime, long)>();
        if (!Directory.Exists(folder)) return list;
        foreach (var file in Directory.GetFiles(folder, "creatordesktop_backup_*.zip"))
        {
            var info = new FileInfo(file);
            list.Add((file, info.CreationTime, info.Length));
        }
        return list.OrderByDescending(b => b.Item2).ToList();
    }
}
