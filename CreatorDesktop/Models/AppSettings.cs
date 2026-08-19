using System.Text.Json;
using System.Text.Json.Serialization;

namespace CreatorDesktop.Models;

public class AppSettings
{
    // API keys
    public string AiProvider { get; set; } = "anthropic";   // anthropic | openai | gemini

    public string AnthropicApiKey { get; set; } = "";
    public string AnthropicModel { get; set; } = "claude-sonnet-4-6";

    public string OpenAiApiKey { get; set; } = "";
    public string OpenAiModel { get; set; } = "gpt-4o";

    public string GeminiApiKey { get; set; } = "";
    public string GeminiModel { get; set; } = "gemini-2.0-flash";

    // Pixabay Music API (free) — separate from Pexels. Same registration page.
    public string PixabayMusicKey { get; set; } = "";

    public string ElevenLabsApiKey { get; set; } = "";
    public string ElevenLabsVoiceId { get; set; } = "21m00Tcm4TlvDq8ikWAM";
    public string PexelsApiKey { get; set; } = "";
    public string YouTubeClientId { get; set; } = "";
    public string YouTubeClientSecret { get; set; } = "";
    public string? YouTubeRefreshToken { get; set; }
    public string OutputFolder { get; set; } = "";
    public string FFmpegPath { get; set; } = "";

    // Upload behavior
    public bool AutoUploadAfterRender { get; set; } = false;
    public string UploadVisibility { get; set; } = "public";   // private | unlisted | public

    // YouTube publish automation
    public bool AutoCategory { get; set; } = true;    // detect category from topic keywords
    public bool ScheduleUpload { get; set; } = false; // schedule instead of instant publish
    public int ScheduleDaysFromNow { get; set; } = 1; // publish in N days
    public int ScheduleHour { get; set; } = 15;       // publish at this hour (0-23)

    // Whether the duration field is active (false = download everything, no time limit)
    public bool UseDuration { get; set; } = true;

    // History of YouTube video IDs that have already been downloaded — used to
    // prevent duplicate downloads. Stored in settings.json so it survives restarts.
    public List<string> DownloadedVideoIds { get; set; } = new();

    // IDs of videos we uploaded — checked periodically for blocks/strikes.
    public List<string> UploadedVideoIds { get; set; } = new();

    // Auto-playlist: create one playlist per topic and add every upload to it.
    public bool AutoPlaylist { get; set; } = true;
    // Maps topic name → YouTube playlist ID so we reuse existing playlists.
    public Dictionary<string, string> TopicPlaylistIds { get; set; } = new();

    // Auto-comment: pin a first comment under every upload to seed engagement.
    public bool AutoPostComment { get; set; } = true;
    public string AutoCommentTemplate { get; set; } = "Was hat dich am meisten überrascht? Schreib's in die Kommentare! 👇";

    // 24h CTR check: warn (and optionally update tags) for under-performers.
    public bool AutoOptimize { get; set; } = true;
    public double OptimizeCtrThreshold { get; set; } = 0.04; // 4 %

    // Auto-reply to viewer comments using AI.
    public bool AutoReplyComments { get; set; } = false;
    public string AutoReplyTemplate { get; set; } = "Danke für deinen Kommentar! 🙌";
    // Maps videoId → last seen comment IDs so we don't reply twice.
    public Dictionary<string, List<string>> RepliedCommentIds { get; set; } = new();

    // Reddit cross-posting (script app — reddit.com/prefs/apps).
    public string RedditClientId { get; set; } = "";
    public string RedditClientSecret { get; set; } = "";
    public string RedditUsername { get; set; } = "";
    public string RedditPassword { get; set; } = "";
    public bool AutoPostReddit { get; set; } = false;
    public List<string> RedditSubreddits { get; set; } = new() { "Damnthatsinteresting", "Documentaries", "InterestingAsFuck" };

    // TikTok: vertical 9:16 export to a folder so the user can drop the file on tiktok.com/upload.
    public bool TikTokAutoExport { get; set; } = false;
    public string TikTokExportFolder { get; set; } = "";
    public bool TikTokAutoOpen { get; set; } = true;

    // Telegram cross-posting (Bot API).
    public bool AutoPostTelegram { get; set; } = false;
    public string TelegramBotToken { get; set; } = "";
    public string TelegramChatId { get; set; } = "";
    public string TelegramTemplate { get; set; } = "🎬 {title}\n\n{url}";

    // Competitor tracker — polls competitor channels' RSS feed every X hours.
    public bool CompetitorTrackerEnabled { get; set; } = false;
    public int CompetitorCheckIntervalHours { get; set; } = 6;
    public DateTime? LastCompetitorCheckAt { get; set; }
    // List of last-seen videoIds per channel, so we only notify about NEW ones.
    public Dictionary<string, List<string>> CompetitorSeenVideos { get; set; } = new();

    // Discord wish-channel: bot reads messages from a channel, each becomes a wish.
    public string DiscordBotToken { get; set; } = "";
    public string DiscordWishChannelId { get; set; } = "";
    public string DiscordInviteLink { get; set; } = ""; // appears in every video description
    public bool WishPollEnabled { get; set; } = false;
    public int WishPollIntervalHours { get; set; } = 2;
    public DateTime? LastWishPollAt { get; set; }
    public List<string> ProcessedWishMessageIds { get; set; } = new();

    // Multilingual titles: AI translates the title (+ description) into target languages
    // and uploads them as YouTube localizations. Video stays German; foreign viewers
    // see the localized title in their language and can enable YouTube auto-captions.
    public bool AutoMultilingual { get; set; } = false;
    public List<string> MultilingualTargets { get; set; } = new() { "en", "es", "tr", "fr" };

    // Discord webhook for community announcements (separate from wish bot).
    public bool AutoPostDiscordWebhook { get; set; } = false;
    public string DiscordWebhookUrl { get; set; } = "";

    // AI-generated, context-aware replies instead of the static template.
    public bool AutoReplyUseAI { get; set; } = false;

    // Trending detector — polls YouTube trending feed and matches against
    // the user's topic keywords.
    public bool TrendingDetectorEnabled { get; set; } = false;
    public int TrendingCheckIntervalHours { get; set; } = 12;
    public DateTime? LastTrendingCheckAt { get; set; }
    public bool TrendingAutoAddToQueue { get; set; } = false;

    // Backup: stores config + playlists + wishes + competitors as a ZIP.
    public string BackupFolder { get; set; } = "";
    public bool AutoBackupEnabled { get; set; } = false;
    public int AutoBackupIntervalHours { get; set; } = 24;
    public int AutoBackupKeepDays { get; set; } = 30;
    public DateTime? LastBackupAt { get; set; }

    // Seasonal topic suggestions — checks today against a calendar of anniversaries
    // and seasonal windows, surfaces matching topics in the tray + log.
    public bool SeasonalSuggestionsEnabled { get; set; } = true;
    public bool SeasonalAutoAddToQueue { get; set; } = false;
    public DateTime? LastSeasonalCheckAt { get; set; }

    // Browser to read cookies from via --cookies-from-browser (e.g. "edge", "chrome",
    // "firefox"). Bypasses YouTube's bot check without any manual export. Leave empty
    // to use YtDlpCookiesPath instead.
    public string YtDlpCookiesBrowser { get; set; } = "edge";

    // Path to a Netscape-format cookies.txt (fallback if no browser is set).
    public string YtDlpCookiesPath { get; set; } = "";

    // CC-only mode: fetch + concat Creative Commons YouTube videos, no AI/TTS
    public bool CcOnlyMode { get; set; } = false;

    // When CC-only mode is active: re-encode each clip to 1080p and concatenate into one file.
    // Off by default — individual downloaded files are kept as-is for manual upload.
    public bool CcMergeAndReencode { get; set; } = false;

    // Shorts mode: search only for CC videos ≤60 s and upload them as #Shorts.
    public bool CcShortsMode { get; set; } = false;

    // Auto-queue / scheduler
    public List<string> QueuedTopics { get; set; } = new();
    public string ChannelTheme { get; set; } = "";
    public string DefaultStyle { get; set; } = "documentary";
    public string DefaultLanguage { get; set; } = "de";
    public int DefaultDurationSeconds { get; set; } = 1800;  // 30 min — documentary mode
    public bool SchedulerEnabled { get; set; } = false;
    public int SchedulerIntervalHours { get; set; } = 24;
    public DateTime? LastAutoRunAt { get; set; }

    [JsonIgnore]
    public string SettingsPath { get; private set; } = "";

    public static string DefaultDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CreatorDesktop");

    public static AppSettings Load()
    {
        Directory.CreateDirectory(DefaultDataDir);
        var path = Path.Combine(DefaultDataDir, "settings.json");
        AppSettings settings;
        if (File.Exists(path))
        {
            try
            {
                settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
            }
            catch
            {
                settings = new AppSettings();
            }
        }
        else
        {
            settings = new AppSettings();
        }
        settings.SettingsPath = path;
        if (string.IsNullOrEmpty(settings.OutputFolder))
            settings.OutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "CreatorDesktop");
        Directory.CreateDirectory(settings.OutputFolder);
        return settings;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public static string PlaylistsPath => Path.Combine(DefaultDataDir, "playlists.json");

    public static List<PlaylistEntry> LoadPlaylists()
    {
        if (!File.Exists(PlaylistsPath)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<PlaylistEntry>>(File.ReadAllText(PlaylistsPath)) ?? new();
        }
        catch { return new(); }
    }

    public static void SavePlaylists(List<PlaylistEntry> playlists)
    {
        Directory.CreateDirectory(DefaultDataDir);
        var json = JsonSerializer.Serialize(playlists, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(PlaylistsPath, json);
    }

    public static string SeasonalPath => Path.Combine(DefaultDataDir, "seasonal.json");

    public static List<SeasonalEntry> LoadSeasonal()
    {
        if (!File.Exists(SeasonalPath))
        {
            SaveSeasonal(SeasonalEntry.Defaults());
        }
        try { return JsonSerializer.Deserialize<List<SeasonalEntry>>(File.ReadAllText(SeasonalPath)) ?? new(); }
        catch { return SeasonalEntry.Defaults(); }
    }

    public static void SaveSeasonal(List<SeasonalEntry> entries)
    {
        Directory.CreateDirectory(DefaultDataDir);
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SeasonalPath, json);
    }

    public static string WishesPath => Path.Combine(DefaultDataDir, "wishes.json");

    public static List<WishEntry> LoadWishes()
    {
        if (!File.Exists(WishesPath)) return new();
        try { return JsonSerializer.Deserialize<List<WishEntry>>(File.ReadAllText(WishesPath)) ?? new(); }
        catch { return new(); }
    }

    public static void SaveWishes(List<WishEntry> wishes)
    {
        Directory.CreateDirectory(DefaultDataDir);
        var json = JsonSerializer.Serialize(wishes, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(WishesPath, json);
    }

    public static string CompetitorsPath => Path.Combine(DefaultDataDir, "competitors.json");

    public static List<CompetitorChannel> LoadCompetitors()
    {
        if (!File.Exists(CompetitorsPath)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<CompetitorChannel>>(File.ReadAllText(CompetitorsPath)) ?? new();
        }
        catch { return new(); }
    }

    public static void SaveCompetitors(List<CompetitorChannel> competitors)
    {
        Directory.CreateDirectory(DefaultDataDir);
        var json = JsonSerializer.Serialize(competitors, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(CompetitorsPath, json);
    }
}

public class PlaylistEntry
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
}

public class CompetitorChannel
{
    public string Name { get; set; } = "";
    public string ChannelId { get; set; } = "";
}

public class SeasonalEntry
{
    public string Name { get; set; } = "";
    public List<string> Topics { get; set; } = new();
    // Seasonal window — month 1-12. Null means anniversary-only.
    public int? MonthStart { get; set; }
    public int? MonthEnd { get; set; }
    // Anniversary in MM-DD format, e.g. "04-14" for April 14th. Matched within ±7 days.
    public string? Anniversary { get; set; }

    public static List<SeasonalEntry> Defaults() => new()
    {
        // Annual anniversaries — content peaks around the date
        new() { Name = "Titanic-Jahrestag",     Topics = ["Titanic"],                       Anniversary = "04-14" },
        new() { Name = "Tschernobyl-Jahrestag", Topics = ["Tschernobyl"],                   Anniversary = "04-26" },
        new() { Name = "WW2-Ende",              Topics = ["Zweiter Weltkrieg", "Hitler"],   Anniversary = "05-08" },
        new() { Name = "Mondlandung-Jahrestag", Topics = ["Mondlandung", "NASA", "Weltall"], Anniversary = "07-21" },
        new() { Name = "Hiroshima-Jahrestag",   Topics = ["Hiroshima Nagasaki"],            Anniversary = "08-06" },
        new() { Name = "9/11-Jahrestag",        Topics = ["9/11", "Terrorismus"],           Anniversary = "09-11" },
        new() { Name = "WW2-Beginn",            Topics = ["Zweiter Weltkrieg", "Hitler"],   Anniversary = "09-01" },
        new() { Name = "Mauerfall",             Topics = ["Berliner Mauer", "DDR"],         Anniversary = "11-09" },
        new() { Name = "Pearl Harbor",          Topics = ["Pearl Harbor", "Zweiter Weltkrieg"], Anniversary = "12-07" },

        // Seasonal windows — month ranges where these topics outperform
        new() { Name = "Hai-Saison Sommer",     Topics = ["Haie", "Hai-Angriffe", "Tiefsee Monster"],
                MonthStart = 6, MonthEnd = 8 },
        new() { Name = "Naturkatastrophen Sommer", Topics = ["Vulkane", "Tsunamis", "Naturkatastrophen"],
                MonthStart = 6, MonthEnd = 9 },
        new() { Name = "Halloween-Saison",      Topics = ["Geister", "Mysterien", "UFOs", "Lost Places"],
                MonthStart = 10, MonthEnd = 10 },
        new() { Name = "True-Crime Herbst",     Topics = ["Serienmörder", "Wahre Verbrechen", "Mafia", "Ungelöste Morde"],
                MonthStart = 10, MonthEnd = 11 },
        new() { Name = "Geschichte Winter",     Topics = ["Zweiter Weltkrieg", "Hitler", "Diktatoren", "Römer"],
                MonthStart = 11, MonthEnd = 2 },
        new() { Name = "Survival Winter",       Topics = ["Extremsport", "Klettern", "Naturkatastrophen"],
                MonthStart = 1, MonthEnd = 2 },
        new() { Name = "Weltall Frühling",      Topics = ["Weltall", "Mars", "NASA", "Planeten"],
                MonthStart = 3, MonthEnd = 5 },
        new() { Name = "Antike Frühling/Sommer", Topics = ["Ägypten Pyramiden", "Römer", "Wikinger", "Antike Kulturen"],
                MonthStart = 4, MonthEnd = 8 },
    };
}

public class WishEntry
{
    public string MessageId { get; set; } = "";
    public string User { get; set; } = "";
    public string Wish { get; set; } = "";
    public DateTime FoundAt { get; set; }
    public List<WishResult> Results { get; set; } = new();
}

public class WishResult
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public int DurationSeconds { get; set; }
}
