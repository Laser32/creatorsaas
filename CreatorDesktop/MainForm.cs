using System.Diagnostics;
using CreatorDesktop.Models;
using CreatorDesktop.Services;

namespace CreatorDesktop;

public class MainForm : Form
{
    private readonly AppSettings _settings;

    // Create tab
    private ComboBox _topicBox = null!;
    private ComboBox _styleBox = null!;
    private ComboBox _langBox = null!;
    private NumericUpDown _durationBox = null!;
    private CheckBox _useDurationCheck = null!;
    private CheckBox _autoUploadCheck = null!;
    private CheckBox _ccOnlyCheck = null!;
    private CheckBox _ccMergeCheck = null!;
    private CheckBox _ccShortsCheck = null!;
    private CheckedListBox _playlistList = null!;
    private List<PlaylistEntry> _playlists = new();

    // Competitor tracker
    private CheckBox _competitorEnabledCheck = null!;
    private NumericUpDown _competitorIntervalBox = null!;
    private ListBox _competitorList = null!;
    private ListView _competitorFeed = null!;
    private List<CompetitorChannel> _competitors = new();
    private System.Windows.Forms.Timer? _competitorTimer;

    // Seasonal suggestions
    private CheckBox _seasonalAutoQueueCheck = null!;
    private Label _seasonalLabel = null!;
    private System.Windows.Forms.Timer? _seasonalTimer;
    private Button _searchVideosBtn = null!;
    private ListView _videoListView = null!;
    private Button _selectAllBtn = null!;
    private Button _selectNoneBtn = null!;
    private Button _downloadCheckedBtn = null!;
    private Button _checkLicensesBtn = null!;
    private NumericUpDown _maxResultsBox = null!;
    private Label _searchStatusLabel = null!;

    // Tools tab
    private TextBox _toolsLogBox = null!;
    private NumericUpDown _shortsCountBox = null!;
    private NumericUpDown _shortsDurationBox = null!;
    private TextBox _thumbTitleBox = null!;
    private TextBox _thumbTopicBox = null!;
    private TextBox _musicQueryBox = null!;
    private ListView _musicListView = null!;

    // Stats tab
    private ListView _statsListView = null!;
    private Label _statsHeaderLabel = null!;

    // Archive.org tab
    private ComboBox _archiveTopicBox = null!;
    private CheckBox _archiveGermanOnly = null!;
    private TextBox _archiveQueryBox = null!;
    private NumericUpDown _archiveYearFromBox = null!;
    private NumericUpDown _archiveYearToBox = null!;
    private NumericUpDown _archiveMaxBox = null!;
    private ListView _archiveListView = null!;
    private Label _archiveStatusLabel = null!;
    private Button _archiveSearchBtn = null!;
    private Button _archiveDownloadBtn = null!;
    private ComboBox _visibilityBox = null!;
    private Button _generateBtn = null!;
    private Button _cancelBtn = null!;
    private Button _openFolderBtn = null!;
    private Button _uploadBtn = null!;
    private TextBox _logBox = null!;
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;

    // Auto tab
    private TextBox _channelTheme = null!;
    private NumericUpDown _topicCount = null!;
    private Button _genTopicsBtn = null!;
    private TextBox _queueBox = null!;
    private Label _queueCountLabel = null!;
    private Button _saveQueueBtn = null!;
    private Button _clearQueueBtn = null!;
    private Button _processOneBtn = null!;
    private Button _processAllBtn = null!;
    private Button _stopQueueBtn = null!;
    private CheckBox _schedulerCheck = null!;
    private NumericUpDown _intervalHours = null!;
    private Label _nextRunLabel = null!;
    private TextBox _autoLogBox = null!;

    // Settings tab
    private ComboBox _aiProviderBox = null!;
    private TextBox _anthropicKey = null!;
    private ComboBox _anthropicModel = null!;
    private TextBox _openAiKey = null!;
    private ComboBox _openAiModel = null!;
    private TextBox _geminiKey = null!;
    private ComboBox _geminiModel = null!;
    private TextBox _elevenKey = null!;
    private TextBox _elevenVoice = null!;
    private TextBox _pexelsKey = null!;
    private TextBox _ytClientId = null!;
    private TextBox _ytClientSecret = null!;
    private Label _ytStatus = null!;
    private TextBox _outputFolder = null!;
    private Button _saveSettingsBtn = null!;
    private Button _ytConnectBtn = null!;
    private Button _ytDisconnectBtn = null!;
    private CheckBox _autoCategoryCheck = null!;
    private CheckBox _autoCommentCheck = null!;
    private TextBox _autoCommentText = null!;
    private CheckBox _autoOptimizeCheck = null!;
    private CheckBox _autoReplyCheck = null!;
    private TextBox _autoReplyText = null!;
    private CheckBox _redditAutoPostCheck = null!;
    private TextBox _redditClientId = null!;
    private TextBox _redditClientSecret = null!;
    private TextBox _redditUsername = null!;
    private TextBox _redditPassword = null!;
    private TextBox _redditSubreddits = null!;
    private CheckBox _tiktokAutoExportCheck = null!;
    private CheckBox _tiktokAutoOpenCheck = null!;
    private TextBox _tiktokExportFolder = null!;
    private CheckBox _telegramAutoPostCheck = null!;
    private TextBox _telegramBotToken = null!;
    private TextBox _telegramChatId = null!;
    private TextBox _telegramTemplate = null!;
    private TextBox _discordBotToken = null!;
    private TextBox _discordWishChannelId = null!;
    private TextBox _discordInviteLink = null!;
    private CheckBox _wishPollEnabledCheck = null!;
    private NumericUpDown _wishPollIntervalBox = null!;
    private CheckBox _multilingualCheck = null!;
    private TextBox _multilingualTargets = null!;
    private CheckBox _discordWebhookCheck = null!;
    private TextBox _discordWebhookUrl = null!;
    private CheckBox _aiReplyCheck = null!;
    private CheckBox _trendingEnabledCheck = null!;
    private CheckBox _trendingAutoQueueCheck = null!;
    private NumericUpDown _trendingIntervalBox = null!;
    private System.Windows.Forms.Timer? _trendingTimer;
    private CheckBox _scheduleUploadCheck = null!;
    private NumericUpDown _scheduleDaysBox = null!;
    private NumericUpDown _scheduleHourBox = null!;
    private ComboBox _cookiesBrowserBox = null!;
    private CheckBox _ytDlpAutoUpdateCheck = null!;
    private Button _ytDlpUpdateBtn = null!;

    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _queueCts;
    private VideoJob? _lastJob;

    // Tray + scheduler
    private NotifyIcon? _trayIcon;
    private System.Windows.Forms.Timer? _schedulerTimer;
    private System.Windows.Forms.Timer? _blockCheckTimer;
    private QueueProcessor? _queueProcessor;

    // ─── Design tokens ──────────────────────────────────────────────────────
    private const int FormWidth = 920;
    private const int FormHeight = 640;
    private const int ContentWidth = 880;
    private const int PadX = 28;
    private const int LabelH = 22;
    private const int InputH = 32;
    private const int HintH = 18;
    private const int FieldGap = 18;
    private const int ButtonH = 44;

    private static readonly Font FontBase = new("Segoe UI", 11f);
    private static readonly Font FontBold = new("Segoe UI", 11f, FontStyle.Bold);
    private static readonly Font FontLabel = new("Segoe UI", 10f);
    private static readonly Font FontHint = new("Segoe UI", 9f, FontStyle.Italic);
    private static readonly Font FontTitle = new("Segoe UI", 16f, FontStyle.Bold);
    private static readonly Font FontSection = new("Segoe UI", 12f, FontStyle.Bold);
    private static readonly Font FontMono = new("Consolas", 10f);

    private static readonly Color BgColor = Color.FromArgb(246, 248, 251);
    private static readonly Color CardColor = Color.White;
    private static readonly Color BorderColor = Color.FromArgb(218, 225, 235);
    private static readonly Color PrimaryColor = Color.FromArgb(50, 110, 220);
    private static readonly Color AccentRed = Color.FromArgb(220, 60, 60);
    private static readonly Color AccentGreen = Color.FromArgb(0, 150, 80);
    private static readonly Color TextDark = Color.FromArgb(35, 45, 65);
    private static readonly Color TextMuted = Color.FromArgb(110, 120, 135);
    private static readonly Color HintColor = Color.FromArgb(70, 110, 180);

    public MainForm()
    {
        AutoScaleMode = AutoScaleMode.None;
        Font = FontBase;
        Text = "CreatorDesktop — AI Video Generator";
        ClientSize = new Size(FormWidth, FormHeight);
        MinimumSize = new Size(700, 480);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgColor;
        // Force-visible on startup so a previously enabled scheduler doesn't trap the window in the tray
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Visible = true;

        _settings = AppSettings.Load();
        _queueProcessor = new QueueProcessor(_settings);

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = FontBase,
            Padding = new Point(18, 8)
        };
        Controls.Add(tabs);

        var createTab = new TabPage("  Create Video  ") { BackColor = BgColor };
        BuildCreateTab(createTab);
        tabs.TabPages.Add(createTab);

        var autoTab = new TabPage("  Auto-Modus  ") { BackColor = BgColor };
        BuildAutoTab(autoTab);
        tabs.TabPages.Add(autoTab);

        var archiveTab = new TabPage("  Archive.org  ") { BackColor = BgColor };
        BuildArchiveTab(archiveTab);
        tabs.TabPages.Add(archiveTab);

        var toolsTab = new TabPage("  Tools  ") { BackColor = BgColor };
        BuildToolsTab(toolsTab);
        tabs.TabPages.Add(toolsTab);

        var statsTab = new TabPage("  Stats  ") { BackColor = BgColor };
        BuildStatsTab(statsTab);
        tabs.TabPages.Add(statsTab);

        var wishTab = new TabPage("  Wish  ") { BackColor = BgColor };
        BuildWishTab(wishTab);
        tabs.TabPages.Add(wishTab);

        var backupTab = new TabPage("  Backup  ") { BackColor = BgColor };
        BuildBackupTab(backupTab);
        tabs.TabPages.Add(backupTab);

        var settingsTab = new TabPage("  Settings  ") { BackColor = BgColor };
        BuildSettingsTab(settingsTab);
        tabs.TabPages.Add(settingsTab);

        LoadSettingsToForm();
        InitTrayIcon();
        InitScheduler();

        // Make sure the window pops to the front on startup
        Shown += (_, _) =>
        {
            WindowState = FormWindowState.Normal;
            Show();
            BringToFront();
            Activate();
        };

        FormClosing += OnFormClosing;
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized && _settings.SchedulerEnabled)
            {
                Hide();
                _trayIcon!.Visible = true;
                _trayIcon.ShowBalloonTip(2000, "CreatorDesktop", "Läuft im Hintergrund weiter.", ToolTipIcon.Info);
            }
        };
    }

    // ═══ Tray / Scheduler ═══════════════════════════════════════════════════

    private void InitTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Fenster anzeigen", null, (_, _) => RestoreFromTray());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => { _trayIcon!.Visible = false; Application.Exit(); });

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "CreatorDesktop",
            ContextMenuStrip = menu,
            Visible = false
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        _trayIcon!.Visible = false;
    }

    private void InitScheduler()
    {
        _schedulerTimer = new System.Windows.Forms.Timer { Interval = 60_000 };  // 1 minute
        _schedulerTimer.Tick += async (_, _) => await SchedulerTickAsync();
        _schedulerTimer.Start();

        // Check for blocked/removed uploads every 2 hours
        _blockCheckTimer = new System.Windows.Forms.Timer { Interval = 2 * 60 * 60 * 1000 };
        _blockCheckTimer.Tick += async (_, _) => await CheckBlockedUploadsAsync();
        _blockCheckTimer.Start();

        // Competitor RSS poll — interval is read from settings on each tick
        _competitorTimer = new System.Windows.Forms.Timer { Interval = 60_000 }; // tick once a minute
        _competitorTimer.Tick += async (_, _) =>
        {
            if (!_settings.CompetitorTrackerEnabled) return;
            var hours = Math.Max(1, _settings.CompetitorCheckIntervalHours);
            var last = _settings.LastCompetitorCheckAt;
            if (last.HasValue && DateTime.Now - last.Value < TimeSpan.FromHours(hours)) return;
            await RunCompetitorCheck(showPopup: false);
        };
        _competitorTimer.Start();

        // Trending detector — same pattern, ticks every minute, fires on interval
        _trendingTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _trendingTimer.Tick += async (_, _) =>
        {
            if (!_settings.TrendingDetectorEnabled) return;
            var hours = Math.Max(1, _settings.TrendingCheckIntervalHours);
            var last = _settings.LastTrendingCheckAt;
            if (last.HasValue && DateTime.Now - last.Value < TimeSpan.FromHours(hours)) return;
            await RunTrendingCheckAsync();
        };
        _trendingTimer.Start();

        // Seasonal suggestions — check daily (skip if same day already checked)
        _seasonalTimer = new System.Windows.Forms.Timer { Interval = 60 * 60 * 1000 }; // hourly tick
        _seasonalTimer.Tick += (_, _) =>
        {
            if (!_settings.SeasonalSuggestionsEnabled) return;
            var last = _settings.LastSeasonalCheckAt;
            if (last.HasValue && last.Value.Date == DateTime.Now.Date) return;
            RunSeasonalCheck(showPopup: false);
        };
        _seasonalTimer.Start();
    }

    /// <summary>
    /// Compares today's date to the seasonal.json calendar (anniversaries +
    /// month windows) and surfaces matches in the log, the in-tab label and,
    /// when enabled, the Auto-Mode queue.
    /// </summary>
    private void RunSeasonalCheck(bool showPopup)
    {
        try
        {
            var hits = SeasonalSuggesterService.GetActiveSuggestions(DateTime.Now);
            _settings.LastSeasonalCheckAt = DateTime.Now;

            if (hits.Count == 0)
            {
                AppendAutoLog("Saisonal: heute keine passenden Trends.");
                _settings.Save();
                ReloadSeasonalLabel();
                if (showPopup)
                    MessageBox.Show(this, "Heute keine saisonalen Treffer.",
                        "Saisonal-Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AppendAutoLog($"Saisonal: {hits.Count} Treffer für heute:");
            foreach (var h in hits)
                AppendAutoLog($"  📅 {h.Entry.Name} ({h.Reason}) → {string.Join(", ", h.Entry.Topics)}");

            // Tray notification
            var preview = string.Join("\n", hits.Take(4).Select(h => $"• {h.Entry.Name}"));
            _trayIcon?.ShowBalloonTip(8000, "📅 Saisonale Themen heute",
                $"{hits.Count} passende Trends:\n{preview}", ToolTipIcon.Info);

            // Auto-queue
            if (_settings.SeasonalAutoAddToQueue)
            {
                int added = 0;
                foreach (var topic in hits.SelectMany(h => h.Entry.Topics).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!_settings.QueuedTopics.Contains(topic, StringComparer.OrdinalIgnoreCase))
                    {
                        _settings.QueuedTopics.Add(topic);
                        added++;
                    }
                }
                if (added > 0) AppendAutoLog($"  → {added} Thema/Themen in Auto-Queue eingereiht.");
            }

            _settings.Save();
            ReloadSeasonalLabel();

            if (showPopup)
            {
                var details = string.Join("\n",
                    hits.Select(h => $"• {h.Entry.Name} ({h.Reason}) → {string.Join(", ", h.Entry.Topics)}"));
                MessageBox.Show(this, $"Heute passende Themen:\n\n{details}",
                    "Saisonal-Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            AppendAutoLog($"Saisonal-Check Fehler: {ex.Message}");
        }
    }

    private void ReloadSeasonalLabel()
    {
        try
        {
            var hits = SeasonalSuggesterService.GetActiveSuggestions(DateTime.Now);
            _seasonalLabel.Text = hits.Count == 0
                ? "Heute keine saisonalen Treffer."
                : "Heutige Treffer:\n" + string.Join("\n",
                    hits.Take(3).Select(h => $"  📅 {h.Entry.Name}: {string.Join(", ", h.Entry.Topics)}"));
        }
        catch { _seasonalLabel.Text = "Saisonal-Daten nicht ladbar."; }
    }

    /// <summary>
    /// Polls YouTube DE trending feed and matches against the user's topic keywords.
    /// Matches surface as a tray-tip; optionally get auto-queued for Auto-Mode.
    /// </summary>
    private async Task RunTrendingCheckAsync()
    {
        try
        {
            // Build the matcher keyword set from queued topics + channel theme + the default topics
            var keywords = new List<string>();
            keywords.AddRange(_settings.QueuedTopics);
            if (!string.IsNullOrWhiteSpace(_settings.ChannelTheme))
                keywords.Add(_settings.ChannelTheme);
            keywords.AddRange(DocumentaryTopics);

            var log = (IProgress<string>)new Progress<string>(s => AppendAutoLog(s));
            log.Report("Trending-Check läuft...");

            var detector = new TrendingDetectorService();
            var hits = await detector.FindMatchesAsync(keywords, log, CancellationToken.None);

            _settings.LastTrendingCheckAt = DateTime.Now;
            if (hits.Count == 0)
            {
                log.Report("  Keine Trending-Treffer für deine Themen.");
                _settings.Save();
                return;
            }

            var preview = string.Join("\n", hits.Take(5)
                .Select(h => $"• {h.Title} ({h.Views:N0} Views, Thema: {h.MatchedKeyword})"));
            _trayIcon?.ShowBalloonTip(8000,
                "🔥 Trending-Treffer",
                $"{hits.Count} passende Themen heute trending:\n{preview}",
                ToolTipIcon.Info);
            log.Report($"  ✓ {hits.Count} Treffer.");
            foreach (var h in hits.Take(10))
                log.Report($"    • {h.Title} — {h.Views:N0} Views (Match: {h.MatchedKeyword})");

            // Optional: matched keyword wandert in die Auto-Queue (kein Duplikat)
            if (_settings.TrendingAutoAddToQueue)
            {
                int added = 0;
                foreach (var h in hits)
                {
                    if (!_settings.QueuedTopics.Contains(h.MatchedKeyword, StringComparer.OrdinalIgnoreCase))
                    {
                        _settings.QueuedTopics.Add(h.MatchedKeyword);
                        added++;
                    }
                }
                if (added > 0) log.Report($"  → {added} Thema/Themen in Auto-Queue eingereiht.");
            }
            _settings.Save();
        }
        catch (Exception ex)
        {
            AppendAutoLog($"Trending-Check Fehler: {ex.Message}");
        }
    }

    private async Task CheckBlockedUploadsAsync()
    {
        if (_settings.UploadedVideoIds.Count == 0) return;
        if (string.IsNullOrWhiteSpace(_settings.YouTubeClientId)) return;

        try
        {
            var yt = new YouTubeService(_settings);
            var log = (IProgress<string>)new Progress<string>(s =>
            {
                AppendAutoLog(s);
                _statusLabel.Text = s;
            });

            // 1) Block / strike check
            var removed = await yt.DeleteBlockedVideosAsync(
                _settings.UploadedVideoIds.ToList(), log, CancellationToken.None);
            if (removed.Count > 0)
            {
                foreach (var (vid, _) in removed) _settings.UploadedVideoIds.Remove(vid);
                _settings.Save();
                var msg = string.Join("\n", removed.Select(r => $"• {r.videoId} ({r.reason})"));
                MessageBox.Show(this,
                    $"{removed.Count} Video(s) wurden automatisch gelöscht:\n\n{msg}",
                    "Videos gesperrt & gelöscht", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (_settings.UploadedVideoIds.Count == 0) return;
            var accessToken = await yt.EnsureAccessTokenAsync(log, CancellationToken.None);

            // 2) Performance check + auto-optimize (24h after publish)
            if (_settings.AutoOptimize)
            {
                var stats = await yt.GetVideoStatsAsync(_settings.UploadedVideoIds.ToList(),
                    accessToken, CancellationToken.None);
                var underPerformers = new List<YouTubeService.VideoStats>();
                foreach (var s in stats)
                {
                    var ageHours = (DateTime.UtcNow - s.PublishedAt.ToUniversalTime()).TotalHours;
                    if (ageHours < 24) continue;
                    // Proxy for CTR: tiny views after 24h = poor performance
                    if (s.Views < 50) underPerformers.Add(s);
                }
                if (underPerformers.Count > 0)
                {
                    var lines = underPerformers.Take(5)
                        .Select(u => $"• {u.Title} — {u.Views} Views, {u.Likes} Likes");
                    _trayIcon?.ShowBalloonTip(8000,
                        "Schwache Performance erkannt",
                        $"{underPerformers.Count} Video(s) unter Performance-Grenze:\n{string.Join("\n", lines)}",
                        ToolTipIcon.Warning);
                    log.Report($"⚠ {underPerformers.Count} Video(s) mit schwacher Performance.");
                }
            }

            // 3) Auto-reply to new viewer comments
            if (_settings.AutoReplyComments && !string.IsNullOrWhiteSpace(_settings.AutoReplyTemplate))
            {
                foreach (var vid in _settings.UploadedVideoIds.ToList())
                {
                    try
                    {
                        var threads = await yt.ListCommentsAsync(vid, accessToken, CancellationToken.None, 20);
                        if (!_settings.RepliedCommentIds.TryGetValue(vid, out var seen))
                            seen = _settings.RepliedCommentIds[vid] = new List<string>();

                        foreach (var t in threads)
                        {
                            if (seen.Contains(t.CommentId)) continue;
                            // Don't reply to our own auto-comment
                            if (t.Text.Equals(_settings.AutoCommentTemplate, StringComparison.OrdinalIgnoreCase))
                            {
                                seen.Add(t.CommentId);
                                continue;
                            }
                            try
                            {
                                // AI-generated reply if enabled, else static template
                                var replyText = _settings.AutoReplyTemplate;
                                if (_settings.AutoReplyUseAI)
                                {
                                    try
                                    {
                                        replyText = await GenerateAiReplyAsync(t.AuthorName, t.Text, CancellationToken.None);
                                    }
                                    catch { /* fall back to template on AI failure */ }
                                }
                                await yt.ReplyToCommentAsync(t.CommentId, replyText,
                                    accessToken, CancellationToken.None);
                                seen.Add(t.CommentId);
                                log.Report($"  💬 Geantwortet auf {t.AuthorName}");
                            }
                            catch (Exception rex) { log.Report($"  Reply fehlgeschlagen: {rex.Message}"); }
                        }
                    }
                    catch { /* video has comments disabled or other issue — skip */ }
                }
                _settings.Save();
            }
        }
        catch (Exception ex)
        {
            AppendAutoLog($"Auto-Check Fehler: {ex.Message}");
        }
    }

    private async Task SchedulerTickAsync()
    {
        if (!_settings.SchedulerEnabled) return;
        if (_queueProcessor!.IsRunning) return;
        if (_settings.QueuedTopics.Count == 0) return;

        var last = _settings.LastAutoRunAt;
        var due = last == null || (DateTime.UtcNow - last.Value).TotalHours >= _settings.SchedulerIntervalHours;
        if (!due) { UpdateNextRunLabel(); return; }

        _queueCts = new CancellationTokenSource();
        var progress = new Progress<string>(s =>
        {
            AppendAutoLog(s);
            _statusLabel.Text = s;
        });
        AppendAutoLog($"⏰ Scheduler: starte nächsten Job (Intervall: {_settings.SchedulerIntervalHours}h)");
        await _queueProcessor.ProcessOneAsync(progress, _queueCts.Token);
        RefreshQueueDisplay();
        UpdateNextRunLabel();
    }

    private void UpdateNextRunLabel()
    {
        if (_nextRunLabel == null) return;
        if (!_settings.SchedulerEnabled)
        {
            _nextRunLabel.Text = "Scheduler ist aus.";
            _nextRunLabel.ForeColor = TextMuted;
            return;
        }
        if (_settings.QueuedTopics.Count == 0)
        {
            _nextRunLabel.Text = "Warteschlange leer — Scheduler wartet.";
            _nextRunLabel.ForeColor = TextMuted;
            return;
        }
        var next = (_settings.LastAutoRunAt ?? DateTime.UtcNow.AddDays(-1))
            .AddHours(_settings.SchedulerIntervalHours);
        if (next < DateTime.UtcNow) next = DateTime.UtcNow.AddMinutes(1);
        var localNext = next.ToLocalTime();
        _nextRunLabel.Text = $"Nächste Ausführung: {localNext:dd.MM.yyyy HH:mm}";
        _nextRunLabel.ForeColor = AccentGreen;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_settings.SchedulerEnabled && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _trayIcon!.Visible = true;
            _trayIcon.ShowBalloonTip(3000, "CreatorDesktop läuft weiter",
                "Scheduler ist aktiv. Über das Tray-Icon wieder öffnen.", ToolTipIcon.Info);
            return;
        }
        _trayIcon?.Dispose();
        _schedulerTimer?.Dispose();
    }

    // ═══ Scrollable tab helper ══════════════════════════════════════════════

    private Panel BuildScrollableTab(TabPage tab, int contentHeight)
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = BgColor
        };
        tab.Controls.Add(scroll);

        var content = new Panel
        {
            Top = 0, Left = 0,
            Width = ContentWidth,
            Height = contentHeight,
            BackColor = BgColor
        };
        scroll.Controls.Add(content);
        return content;
    }

    // ═══ Create tab ═════════════════════════════════════════════════════════

    private void BuildCreateTab(TabPage tab)
    {
        var content = BuildScrollableTab(tab, 1180);
        int y = 24;
        int innerW = ContentWidth - PadX * 2;

        content.Controls.Add(new Label
        {
            Text = "Neues Video erstellen",
            Font = FontTitle, ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 32,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        y += 46;

        y = AddFieldRow(content, y, innerW, "Topic / Thema — Auswahl oder eigenen Text eingeben",
            _topicBox = NewEditableCombo("", DocumentaryTopics));
        // Auto-search when user picks from dropdown
        _topicBox.SelectedIndexChanged += async (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_topicBox.Text))
                await SearchVideos();
        };

        // ── CC-Video-Suche & Download-Liste ─────────────────────────────────
        _searchVideosBtn = NewPrimaryButton("🔍  Suchen", 140);
        _searchVideosBtn.Top = y; _searchVideosBtn.Left = PadX;
        _searchVideosBtn.Click += async (_, _) => await SearchVideos();
        content.Controls.Add(_searchVideosBtn);

        content.Controls.Add(new Label
        {
            Text = "Max:", Font = FontLabel, ForeColor = TextDark,
            Top = y + 6, Left = PadX + 150, Width = 36, Height = LabelH,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        _maxResultsBox = new NumericUpDown
        {
            Top = y + 4, Left = PadX + 188, Width = 70, Height = ButtonH,
            Minimum = 50, Maximum = 1000, Increment = 50, Value = 250,
            Font = FontLabel
        };
        content.Controls.Add(_maxResultsBox);

        _checkLicensesBtn = NewPrimaryButton("Lizenzen prüfen (grün/gelb/rot)", 280);
        _checkLicensesBtn.BackColor = Color.FromArgb(40, 140, 60);
        _checkLicensesBtn.ForeColor = Color.White;
        _checkLicensesBtn.Top = y; _checkLicensesBtn.Left = PadX + 268;
        _checkLicensesBtn.Enabled = false;
        _checkLicensesBtn.Click += async (_, _) => await CheckLicensesAsync();
        content.Controls.Add(_checkLicensesBtn);

        y += ButtonH + 6;

        _searchStatusLabel = new Label
        {
            Text = "Thema wählen → 'Suchen' → Videos markieren → 'Herunterladen'",
            Font = FontHint, ForeColor = TextMuted,
            Top = y, Left = PadX, Width = innerW, Height = HintH, AutoSize = false
        };
        content.Controls.Add(_searchStatusLabel);
        y += HintH + 4;

        _videoListView = new ListView
        {
            View = View.Details, CheckBoxes = true, FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Top = y, Left = PadX, Width = innerW, Height = 230,
            Font = FontLabel, BackColor = CardColor, BorderStyle = BorderStyle.FixedSingle
        };
        _videoListView.Columns.Add("Titel", innerW - 115);
        _videoListView.Columns.Add("Dauer", 85);
        _videoListView.Columns.Add("ID", 0);
        content.Controls.Add(_videoListView);
        y += 238;

        _selectAllBtn = NewButton("☑ Alle", 88);
        _selectAllBtn.Top = y; _selectAllBtn.Left = PadX;
        _selectAllBtn.Click += (_, _) => { foreach (ListViewItem it in _videoListView.Items) it.Checked = true; };
        content.Controls.Add(_selectAllBtn);

        _selectNoneBtn = NewButton("☐ Keine", 88);
        _selectNoneBtn.Top = y; _selectNoneBtn.Left = PadX + 96;
        _selectNoneBtn.Click += (_, _) => { foreach (ListViewItem it in _videoListView.Items) it.Checked = false; };
        content.Controls.Add(_selectNoneBtn);

        var clearHistoryBtn = NewButton("🗑 History", 110);
        clearHistoryBtn.Top = y; clearHistoryBtn.Left = PadX + 192;
        clearHistoryBtn.Click += (_, _) =>
        {
            var n = _settings.DownloadedVideoIds.Count;
            if (n == 0) { MessageBox.Show(this, "History ist leer.", "Info"); return; }
            if (MessageBox.Show(this, $"{n} Einträge in der Download-History löschen?\n\nDanach können diese Videos wieder heruntergeladen werden.",
                "History löschen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _settings.DownloadedVideoIds.Clear();
                _settings.Save();
                _searchStatusLabel.Text = $"{n} History-Einträge gelöscht. Neu suchen um Markierungen zu aktualisieren.";
            }
        };
        content.Controls.Add(clearHistoryBtn);

        _downloadCheckedBtn = NewPrimaryButton("▼  Markierte Videos herunterladen", 290);
        _downloadCheckedBtn.Top = y; _downloadCheckedBtn.Left = PadX + innerW - 290;
        _downloadCheckedBtn.Enabled = false;
        _downloadCheckedBtn.Click += async (_, _) => await DownloadCheckedVideos();
        content.Controls.Add(_downloadCheckedBtn);
        y += ButtonH + 20;

        content.Controls.Add(new Label
        {
            Text = "── Vollständige Video-Pipeline ──────────────────────────────────────────",
            Font = FontHint, ForeColor = TextMuted,
            Top = y, Left = PadX, Width = innerW, Height = HintH, AutoSize = false
        });
        y += HintH + 8;

        y = AddFieldRow(content, y, innerW, "Stil",
            _styleBox = NewCombo("documentary", "vlog", "tutorial", "shorts", "explainer"));
        y = AddFieldRow(content, y, innerW, "Sprache",
            _langBox = NewCombo("de", "en"));

        // Duration row: checkbox toggles the numericupdown on/off
        _useDurationCheck = new CheckBox
        {
            Text = "Zieldauer festlegen (Minuten) — ab 10 min: Doku-Modus",
            Font = FontLabel, ForeColor = TextDark,
            Checked = true,
            Top = y, Left = PadX, Width = innerW - 180, Height = InputH,
            AutoSize = false
        };
        _durationBox = new NumericUpDown
        {
            Minimum = 1, Maximum = 120, Value = 30, Increment = 1,
            Font = FontBase, Width = 160, Height = InputH,
            Top = y, Left = PadX + innerW - 170
        };
        _useDurationCheck.CheckedChanged += (_, _) =>
        {
            _durationBox.Enabled = _useDurationCheck.Checked;
            _settings.UseDuration = _useDurationCheck.Checked;
            _settings.Save();
        };
        content.Controls.Add(_useDurationCheck);
        content.Controls.Add(_durationBox);
        y += InputH + FieldGap;

        // CC-only mode: pure CC video compilation (no AI, no TTS, no Pexels)
        _ccOnlyCheck = new CheckBox
        {
            Text = "Nur Creative-Commons-YouTube-Videos verwenden (auflisten + 1-zu-1 herunterladen, kein AI/TTS)",
            Font = FontLabel,
            ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 28,
            AutoSize = false
        };
        _ccOnlyCheck.CheckedChanged += (_, _) =>
        {
            _settings.CcOnlyMode = _ccOnlyCheck.Checked;
            _settings.Save();
        };
        content.Controls.Add(_ccOnlyCheck);
        y += 32;

        // Optional merge + re-encode after CC download
        _ccMergeCheck = new CheckBox
        {
            Text = "Videos zusammenführen und auf 1080p re-encodieren (nach Download)",
            Font = FontLabel,
            ForeColor = TextMuted,
            Top = y, Left = PadX + 24, Width = innerW - 24, Height = 26,
            AutoSize = false
        };
        _ccMergeCheck.CheckedChanged += (_, _) =>
        {
            _settings.CcMergeAndReencode = _ccMergeCheck.Checked;
            _settings.Save();
        };
        content.Controls.Add(_ccMergeCheck);
        y += 34;

        // Shorts-Modus: sucht CC-Videos ≤60 Sek. und markiert sie als #Shorts
        _ccShortsCheck = new CheckBox
        {
            Text = "Shorts-Modus: nur Videos ≤ 60 Sek. suchen und als #Shorts hochladen",
            Font = FontLabel, ForeColor = TextDark,
            Top = y, Left = PadX + 24, Width = innerW - 24, Height = 26,
            AutoSize = false
        };
        _ccShortsCheck.CheckedChanged += (_, _) => { _settings.CcShortsMode = _ccShortsCheck.Checked; _settings.Save(); };
        content.Controls.Add(_ccShortsCheck);
        y += 34;

        // Auto-upload row (checkbox + visibility dropdown)
        _autoUploadCheck = new CheckBox
        {
            Text = "Nach dem Rendern direkt auf YouTube hochladen",
            Font = FontLabel,
            ForeColor = TextDark,
            Top = y, Left = PadX, Width = 420, Height = 28,
            AutoSize = false
        };
        content.Controls.Add(_autoUploadCheck);

        content.Controls.Add(new Label
        {
            Text = "Sichtbarkeit:", Font = FontLabel, ForeColor = TextDark,
            Top = y + 2, Left = PadX + 440, Width = 90, Height = 24,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        _visibilityBox = NewCombo("private", "unlisted", "public");
        _visibilityBox.Top = y;
        _visibilityBox.Left = PadX + 530;
        _visibilityBox.Width = 140;
        _visibilityBox.Height = InputH;
        content.Controls.Add(_visibilityBox);
        y += 40;

        // ── Playlist-Auswahl (manuell verwaltet) ────────────────────────────
        content.Controls.Add(new Label
        {
            Text = "Playlists (Häkchen = Video wird nach Upload eingefügt):",
            Font = FontLabel, ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 22,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        y += 24;

        _playlistList = new CheckedListBox
        {
            Top = y, Left = PadX, Width = innerW, Height = 110,
            Font = FontBase, BorderStyle = BorderStyle.FixedSingle,
            CheckOnClick = true
        };
        content.Controls.Add(_playlistList);
        y += 116;

        var addPlBtn = NewButton("+ Playlist hinzufügen", 200);
        addPlBtn.Top = y; addPlBtn.Left = PadX;
        addPlBtn.Click += (_, _) => AddPlaylistViaDialog();
        content.Controls.Add(addPlBtn);

        var rmPlBtn = NewButton("− Markierte entfernen", 200);
        rmPlBtn.Top = y; rmPlBtn.Left = PadX + 210;
        rmPlBtn.Click += (_, _) => RemoveSelectedPlaylists();
        content.Controls.Add(rmPlBtn);

        var openPlBtn = NewButton("Datei öffnen", 150);
        openPlBtn.Top = y; openPlBtn.Left = PadX + 420;
        openPlBtn.Click += (_, _) => OpenPlaylistsFile();
        content.Controls.Add(openPlBtn);
        y += ButtonH + 14;

        ReloadPlaylistsUi();

        // Buttons
        int btnX = PadX;
        _generateBtn = NewPrimaryButton("▶  Video erzeugen", 210);
        _generateBtn.Top = y; _generateBtn.Left = btnX;
        _generateBtn.Click += async (_, _) => await RunSinglePipeline();
        content.Controls.Add(_generateBtn);
        btnX += _generateBtn.Width + 10;

        _cancelBtn = NewButton("Abbrechen", 120);
        _cancelBtn.Top = y; _cancelBtn.Left = btnX;
        _cancelBtn.Enabled = false;
        _cancelBtn.Click += (_, _) => _cts?.Cancel();
        content.Controls.Add(_cancelBtn);
        btnX += _cancelBtn.Width + 10;

        _openFolderBtn = NewButton("Ordner öffnen", 150);
        _openFolderBtn.Top = y; _openFolderBtn.Left = btnX;
        _openFolderBtn.Enabled = false;
        _openFolderBtn.Click += (_, _) =>
        {
            if (_lastJob?.OutputVideoPath != null && File.Exists(_lastJob.OutputVideoPath))
                Process.Start("explorer.exe", $"/select,\"{_lastJob.OutputVideoPath}\"");
            else if (_lastJob?.WorkDir != null && Directory.Exists(_lastJob.WorkDir))
                Process.Start("explorer.exe", $"\"{_lastJob.WorkDir}\"");
        };
        content.Controls.Add(_openFolderBtn);
        btnX += _openFolderBtn.Width + 10;

        _uploadBtn = NewPrimaryButton("▲  Auf YouTube hochladen", 240);
        _uploadBtn.Top = y; _uploadBtn.Left = btnX;
        _uploadBtn.BackColor = AccentRed;
        _uploadBtn.Enabled = false;
        _uploadBtn.Click += async (_, _) => await UploadToYouTube();
        content.Controls.Add(_uploadBtn);

        y += ButtonH + 18;

        _statusLabel = new Label
        {
            Text = "Bereit.",
            Font = FontBold, ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 24,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };
        content.Controls.Add(_statusLabel);
        y += 26;

        _progressBar = new ProgressBar
        {
            Top = y, Left = PadX, Width = innerW, Height = 10,
            Style = ProgressBarStyle.Continuous, MarqueeAnimationSpeed = 30
        };
        content.Controls.Add(_progressBar);
        y += 22;

        var logBg = NewCard(y, innerW, 240, out var logTitle, "Verlauf");
        content.Controls.Add(logBg);
        _logBox = new TextBox
        {
            Top = 40, Left = 16, Width = innerW - 32, Height = 240 - 54,
            Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true,
            BorderStyle = BorderStyle.None, BackColor = Color.White, Font = FontMono
        };
        logBg.Controls.Add(_logBox);
        y += 240 + 16;

        content.Height = y;
    }

    // ═══ Auto-Modus tab ═════════════════════════════════════════════════════

    private void BuildAutoTab(TabPage tab)
    {
        var content = BuildScrollableTab(tab, 1340);
        int y = 24;
        int innerW = ContentWidth - PadX * 2;

        content.Controls.Add(new Label
        {
            Text = "Auto-Modus", Font = FontTitle, ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 32,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        y += 40;

        content.Controls.Add(new Label
        {
            Text = "Themen sammeln, automatisch abarbeiten und nach Zeitplan hochladen.",
            Font = FontLabel, ForeColor = TextMuted,
            Top = y, Left = PadX, Width = innerW, Height = 24,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        y += 32;

        // ── Section 1: Themen-Generator ──
        y = AddSection(content, y, innerW, "1. Themen-Generator (Claude)", section =>
        {
            int sy = 16;

            _channelTheme = NewTextBox("z.B. interessante historische Ereignisse");
            sy = AddSettingsRow(section, sy, "Kanal-Schwerpunkt / Nische",
                "Worum geht's auf Ihrem Kanal? Claude schlägt darauf basierend Themen vor.",
                _channelTheme);

            content.Controls.Add(new Label { Text = "", Width = 0, Height = 0 });

            // Topic count + button on same row
            var topicCountLbl = new Label
            {
                Text = "Wie viele Themen?", Font = FontLabel, ForeColor = TextDark,
                Top = 36 + sy, Left = 24, Width = 200, Height = LabelH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
            section.Controls.Add(topicCountLbl);

            _topicCount = new NumericUpDown
            {
                Minimum = 1, Maximum = 50, Value = 10, Increment = 1,
                Font = FontBase, Width = 100, Height = InputH,
                Top = 36 + sy + LabelH + 3, Left = 24
            };
            section.Controls.Add(_topicCount);

            _genTopicsBtn = NewPrimaryButton("✨  Themen generieren", 240);
            _genTopicsBtn.Top = 36 + sy + LabelH + 3 - 5;
            _genTopicsBtn.Left = 24 + 120;
            _genTopicsBtn.Click += async (_, _) => await GenerateTopics();
            section.Controls.Add(_genTopicsBtn);

            sy += LabelH + 3 + InputH + FieldGap;
            return sy;
        });

        // ── Section 2: Warteschlange ──
        y = AddSection(content, y, innerW, "2. Themen-Warteschlange", section =>
        {
            int sy = 16;

            var lbl = new Label
            {
                Text = "Ein Thema pro Zeile. Wird von oben nach unten abgearbeitet.",
                Font = FontLabel, ForeColor = TextDark,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = LabelH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
            section.Controls.Add(lbl);
            sy += LabelH + 4;

            _queueBox = new TextBox
            {
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 180,
                Multiline = true, ScrollBars = ScrollBars.Vertical,
                Font = FontMono, BorderStyle = BorderStyle.FixedSingle
            };
            _queueBox.TextChanged += (_, _) => UpdateQueueCount();
            section.Controls.Add(_queueBox);
            sy += 188;

            _queueCountLabel = new Label
            {
                Font = FontBold, ForeColor = TextDark,
                Top = 36 + sy, Left = 24, Width = 300, Height = 24,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
            section.Controls.Add(_queueCountLabel);

            _saveQueueBtn = NewButton("Speichern", 140);
            _saveQueueBtn.Top = 36 + sy - 6; _saveQueueBtn.Left = 24 + section.Width - 48 - 140 - 150;
            _saveQueueBtn.Click += (_, _) => { SaveQueueFromBox(); MessageBox.Show(this, "Warteschlange gespeichert.", "OK"); };
            section.Controls.Add(_saveQueueBtn);

            _clearQueueBtn = NewButton("Leeren", 140);
            _clearQueueBtn.Top = 36 + sy - 6; _clearQueueBtn.Left = 24 + section.Width - 48 - 140;
            _clearQueueBtn.Click += (_, _) =>
            {
                if (MessageBox.Show(this, "Warteschlange wirklich leeren?", "Bestätigen",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _queueBox.Text = "";
                    SaveQueueFromBox();
                }
            };
            section.Controls.Add(_clearQueueBtn);

            sy += 40;
            return sy;
        });

        // ── Section 3: Verarbeitung ──
        y = AddSection(content, y, innerW, "3. Warteschlange ausführen", section =>
        {
            int sy = 16;

            _processOneBtn = NewPrimaryButton("▶  Nächstes Video erzeugen", 250);
            _processOneBtn.Top = 36 + sy; _processOneBtn.Left = 24;
            _processOneBtn.Click += async (_, _) => await ProcessQueue(allOfThem: false);
            section.Controls.Add(_processOneBtn);

            _processAllBtn = NewPrimaryButton("▶▶  Alle abarbeiten", 220);
            _processAllBtn.Top = 36 + sy; _processAllBtn.Left = 24 + 260;
            _processAllBtn.Click += async (_, _) => await ProcessQueue(allOfThem: true);
            section.Controls.Add(_processAllBtn);

            _stopQueueBtn = NewButton("Stop", 120);
            _stopQueueBtn.Top = 36 + sy; _stopQueueBtn.Left = 24 + 260 + 230;
            _stopQueueBtn.Enabled = false;
            _stopQueueBtn.Click += (_, _) => _queueCts?.Cancel();
            section.Controls.Add(_stopQueueBtn);

            sy += ButtonH + 10;
            return sy;
        });

        // ── Section 4: Scheduler ──
        y = AddSection(content, y, innerW, "4. Scheduler (vollautomatischer Modus)", section =>
        {
            int sy = 16;

            _schedulerCheck = new CheckBox
            {
                Text = "Aktiviert — Tool arbeitet im Hintergrund die Warteschlange ab",
                Font = FontLabel, ForeColor = TextDark,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 28,
                AutoSize = false
            };
            _schedulerCheck.CheckedChanged += (_, _) =>
            {
                _settings.SchedulerEnabled = _schedulerCheck.Checked;
                _settings.Save();
                UpdateNextRunLabel();
            };
            section.Controls.Add(_schedulerCheck);
            sy += 36;

            var ivLbl = new Label
            {
                Text = "Intervall (Stunden zwischen Videos):",
                Font = FontLabel, ForeColor = TextDark,
                Top = 36 + sy, Left = 24, Width = 320, Height = LabelH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
            section.Controls.Add(ivLbl);

            _intervalHours = new NumericUpDown
            {
                Minimum = 1, Maximum = 168, Value = 24, Increment = 1,
                Font = FontBase, Width = 120, Height = InputH,
                Top = 36 + sy - 4, Left = 24 + 340
            };
            _intervalHours.ValueChanged += (_, _) =>
            {
                _settings.SchedulerIntervalHours = (int)_intervalHours.Value;
                _settings.Save();
                UpdateNextRunLabel();
            };
            section.Controls.Add(_intervalHours);
            sy += LabelH + 12;

            _nextRunLabel = new Label
            {
                Font = FontBold, ForeColor = TextMuted,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
            section.Controls.Add(_nextRunLabel);
            sy += 32;

            var hint = new Label
            {
                Text = "→ Wenn aktiviert: Beim Schließen geht das Fenster ins Tray. Doppelklick aufs Icon holt es zurück.",
                Font = FontHint, ForeColor = HintColor,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 20,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
            section.Controls.Add(hint);
            sy += 24;

            return sy;
        });

        // ── Konkurrenz-Tracker ──
        var compBg = NewCard(y, innerW, 320, out var compInner, "Konkurrenz-Tracker (kostenlos, ohne API-Key)");
        content.Controls.Add(compBg);

        var compHint = new Label
        {
            Text = "Verfolgt andere YouTube-Kanäle via RSS. Neue Uploads erscheinen unten. Auto-Modus prüft alle X Stunden.",
            Font = FontHint, ForeColor = HintColor,
            Top = 38, Left = 16, Width = innerW - 32, Height = 18, AutoSize = false
        };
        compBg.Controls.Add(compHint);

        _competitorEnabledCheck = new CheckBox
        {
            Text = "Tracker aktiv (regelmäßig RSS prüfen)",
            Font = FontLabel, ForeColor = TextDark,
            Top = 60, Left = 16, Width = 320, Height = 24, AutoSize = false
        };
        compBg.Controls.Add(_competitorEnabledCheck);

        compBg.Controls.Add(new Label
        {
            Text = "Intervall (Stunden):", Font = FontLabel, ForeColor = TextDark,
            Top = 60, Left = 350, Width = 130, Height = 24,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        _competitorIntervalBox = new NumericUpDown
        {
            Minimum = 1, Maximum = 168, Value = 6, Font = FontBase,
            Width = 70, Height = InputH, Top = 60, Left = 485
        };
        compBg.Controls.Add(_competitorIntervalBox);

        var addCompBtn = NewButton("+ Kanal hinzufügen", 180);
        addCompBtn.Top = 92; addCompBtn.Left = 16;
        addCompBtn.Click += async (_, _) => await AddCompetitorViaDialog();
        compBg.Controls.Add(addCompBtn);

        var rmCompBtn = NewButton("− Markierten entfernen", 180);
        rmCompBtn.Top = 92; rmCompBtn.Left = 200;
        rmCompBtn.Click += (_, _) => RemoveSelectedCompetitor();
        compBg.Controls.Add(rmCompBtn);

        var checkNowBtn = NewPrimaryButton("📡  Jetzt prüfen", 180);
        checkNowBtn.Top = 92; checkNowBtn.Left = 384;
        checkNowBtn.Click += async (_, _) => await RunCompetitorCheck(showPopup: true);
        compBg.Controls.Add(checkNowBtn);

        var checkLicBtn = NewButton("🔍  Lizenzen prüfen", 180);
        checkLicBtn.Top = 92; checkLicBtn.Left = 568;
        checkLicBtn.Click += async (_, _) => await CheckCompetitorLicensesAsync();
        compBg.Controls.Add(checkLicBtn);

        var dlCompBtn = NewButton("⬇  Markierte herunterladen", 220);
        dlCompBtn.Top = 92; dlCompBtn.Left = 752;
        dlCompBtn.Click += async (_, _) => await DownloadCheckedCompetitorVideosAsync();
        compBg.Controls.Add(dlCompBtn);

        _competitorList = new ListBox
        {
            Top = 130, Left = 16, Width = 220, Height = 320 - 130 - 12,
            Font = FontBase, BorderStyle = BorderStyle.FixedSingle
        };
        _competitorList.SelectedIndexChanged += (_, _) => { };
        compBg.Controls.Add(_competitorList);

        _competitorFeed = new ListView
        {
            Top = 130, Left = 250, Width = innerW - 250 - 20, Height = 320 - 130 - 12,
            Font = FontBase, View = View.Details, FullRowSelect = true,
            BorderStyle = BorderStyle.FixedSingle, GridLines = false,
            CheckBoxes = true, ShowItemToolTips = true
        };
        _competitorFeed.Columns.Add("Kanal", 120);
        _competitorFeed.Columns.Add("Titel", 290);
        _competitorFeed.Columns.Add("Datum", 80);
        _competitorFeed.Columns.Add("Lizenz", 110);
        _competitorFeed.DoubleClick += (_, _) =>
        {
            if (_competitorFeed.SelectedItems.Count == 0) return;
            var vid = _competitorFeed.SelectedItems[0].Tag as string;
            if (!string.IsNullOrEmpty(vid))
                Process.Start(new ProcessStartInfo
                { FileName = $"https://www.youtube.com/watch?v={vid}", UseShellExecute = true });
        };
        compBg.Controls.Add(_competitorFeed);
        y += 320 + 24;

        ReloadCompetitorsUi();

        // ── Saisonale Themen-Vorschläge ───────────────────────────────────
        var seasBg = NewCard(y, innerW, 200, out var seasInner, "Saisonale Themen-Vorschläge (Was läuft wann?)");
        content.Controls.Add(seasBg);

        var seasHint = new Label
        {
            Text = "Tool checkt Jahrestage + saisonale Trends. Heutige Treffer kommen in den Log + Tray-Tip.",
            Font = FontHint, ForeColor = HintColor,
            Top = 38, Left = 16, Width = innerW - 32, Height = 18, AutoSize = false
        };
        seasBg.Controls.Add(seasHint);

        _seasonalAutoQueueCheck = new CheckBox
        {
            Text = "Treffer-Themen automatisch in die Auto-Queue einreihen",
            Font = FontLabel, ForeColor = TextDark,
            Top = 60, Left = 16, Width = innerW - 32, Height = 24, AutoSize = false
        };
        _seasonalAutoQueueCheck.CheckedChanged += (_, _) =>
            { _settings.SeasonalAutoAddToQueue = _seasonalAutoQueueCheck.Checked; _settings.Save(); };
        seasBg.Controls.Add(_seasonalAutoQueueCheck);

        var seasCheckBtn = NewPrimaryButton("📅  Heute prüfen", 180);
        seasCheckBtn.Top = 90; seasCheckBtn.Left = 16;
        seasCheckBtn.Click += (_, _) => RunSeasonalCheck(showPopup: true);
        seasBg.Controls.Add(seasCheckBtn);

        var seasOpenBtn = NewButton("Datei öffnen", 140);
        seasOpenBtn.Top = 90; seasOpenBtn.Left = 200;
        seasOpenBtn.Click += (_, _) =>
        {
            if (!File.Exists(AppSettings.SeasonalPath))
                AppSettings.SaveSeasonal(SeasonalEntry.Defaults());
            Process.Start(new ProcessStartInfo { FileName = AppSettings.SeasonalPath, UseShellExecute = true });
        };
        seasBg.Controls.Add(seasOpenBtn);

        _seasonalLabel = new Label
        {
            Top = 124, Left = 16, Width = innerW - 32, Height = 60,
            Font = FontBase, ForeColor = TextDark,
            TextAlign = ContentAlignment.TopLeft, AutoSize = false
        };
        seasBg.Controls.Add(_seasonalLabel);
        y += 200 + 24;

        ReloadSeasonalLabel();

        // ── Log ──
        var logBg = NewCard(y, innerW, 200, out _, "Auto-Modus Log");
        content.Controls.Add(logBg);
        _autoLogBox = new TextBox
        {
            Top = 40, Left = 16, Width = innerW - 32, Height = 200 - 54,
            Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true,
            BorderStyle = BorderStyle.None, BackColor = Color.White, Font = FontMono
        };
        logBg.Controls.Add(_autoLogBox);
        y += 200 + 24;

        content.Height = y;
    }

    private void AppendAutoLog(string s)
    {
        RunLog.Write(s);
        if (_autoLogBox.InvokeRequired)
        {
            _autoLogBox.BeginInvoke(() => _autoLogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}\r\n"));
        }
        else
        {
            _autoLogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}\r\n");
        }
    }

    private void UpdateQueueCount()
    {
        var count = _queueBox.Lines.Count(l => !string.IsNullOrWhiteSpace(l));
        _queueCountLabel.Text = $"{count} Themen in der Warteschlange";
    }

    private void SaveQueueFromBox()
    {
        _settings.QueuedTopics = _queueBox.Lines
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
        _settings.Save();
        UpdateQueueCount();
        UpdateNextRunLabel();
    }

    private void RefreshQueueDisplay()
    {
        if (_queueBox.InvokeRequired)
        {
            _queueBox.BeginInvoke(() => RefreshQueueDisplay());
            return;
        }
        _queueBox.Text = string.Join(Environment.NewLine, _settings.QueuedTopics);
        UpdateQueueCount();
    }

    private async Task GenerateTopics()
    {
        SaveCreateFormToSettings();
        SaveQueueFromBox();
        _settings.ChannelTheme = _channelTheme.Text.Trim();
        _settings.Save();

        if (string.IsNullOrWhiteSpace(_settings.ChannelTheme))
        {
            MessageBox.Show(this, "Bitte einen Kanal-Schwerpunkt angeben.", "Fehlt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _genTopicsBtn.Enabled = false;
        try
        {
            var ai = AIServiceFactory.Create(_settings);
            var progress = new Progress<string>(s => { AppendAutoLog(s); _statusLabel.Text = s; });
            var topics = await ai.GenerateTopicSuggestionsAsync(
                _settings.ChannelTheme, (int)_topicCount.Value, _settings.DefaultLanguage, progress, CancellationToken.None);

            // Append to existing queue
            _settings.QueuedTopics.AddRange(topics);
            _settings.Save();
            RefreshQueueDisplay();
            UpdateNextRunLabel();
            AppendAutoLog($"✓ {topics.Count} Themen hinzugefügt.");
        }
        catch (Exception ex)
        {
            AppendAutoLog($"Fehler: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _genTopicsBtn.Enabled = true; }
    }

    private async Task ProcessQueue(bool allOfThem)
    {
        SaveQueueFromBox();
        SaveCreateFormToSettings();

        if (_settings.QueuedTopics.Count == 0)
        {
            MessageBox.Show(this, "Warteschlange ist leer.", "Fehlt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _processOneBtn.Enabled = false;
        _processAllBtn.Enabled = false;
        _stopQueueBtn.Enabled = true;
        _queueCts = new CancellationTokenSource();

        var progress = new Progress<string>(s => { AppendAutoLog(s); _statusLabel.Text = s; });
        try
        {
            if (allOfThem)
                await _queueProcessor!.ProcessAllAsync(progress, _queueCts.Token);
            else
                await _queueProcessor!.ProcessOneAsync(progress, _queueCts.Token);
            RefreshQueueDisplay();
            UpdateNextRunLabel();
        }
        finally
        {
            _processOneBtn.Enabled = true;
            _processAllBtn.Enabled = true;
            _stopQueueBtn.Enabled = false;
        }
    }

    // ═══ Wish tab ══════════════════════════════════════════════════════════

    private ListView _wishList = null!;
    private Label _wishStatusLabel = null!;
    private System.Windows.Forms.Timer? _wishPollTimer;

    private void BuildWishTab(TabPage tab)
    {
        var content = BuildScrollableTab(tab, 720);
        int innerW = ContentWidth - 2 * PadX;
        int y = 24;

        var head = new Label
        {
            Text = "Discord-Wunschliste",
            Font = new Font(FontBase.FontFamily, 18, FontStyle.Bold),
            ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 36, AutoSize = false
        };
        content.Controls.Add(head); y += 40;

        var hint = new Label
        {
            Text = "Bot liest Wünsche aus deinem Discord-Channel und sucht sie auf YouTube. " +
                   "Treffer werden hier gelistet. Setup in Settings: Bot-Token + Channel-ID.",
            Font = FontHint, ForeColor = HintColor,
            Top = y, Left = PadX, Width = innerW, Height = 36, AutoSize = false
        };
        content.Controls.Add(hint); y += 38;

        var pollBtn = NewPrimaryButton("🔄  Wünsche von Discord abholen", 280);
        pollBtn.Top = y; pollBtn.Left = PadX;
        pollBtn.Click += async (_, _) => await PollWishesAsync(showPopup: true);
        content.Controls.Add(pollBtn);

        var clearBtn = NewButton("🗑  Liste leeren", 160);
        clearBtn.Top = y; clearBtn.Left = PadX + 290;
        clearBtn.Click += (_, _) =>
        {
            if (MessageBox.Show(this, "Komplette Wunschliste löschen?",
                    "Leeren", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            AppSettings.SaveWishes(new());
            ReloadWishesUi();
        };
        content.Controls.Add(clearBtn);

        var openFileBtn = NewButton("Datei öffnen", 140);
        openFileBtn.Top = y; openFileBtn.Left = PadX + 460;
        openFileBtn.Click += (_, _) =>
        {
            if (!File.Exists(AppSettings.WishesPath)) AppSettings.SaveWishes(new());
            Process.Start(new ProcessStartInfo { FileName = AppSettings.WishesPath, UseShellExecute = true });
        };
        content.Controls.Add(openFileBtn);
        y += ButtonH + 16;

        _wishStatusLabel = new Label
        {
            Font = FontBold, ForeColor = TextMuted,
            Top = y, Left = PadX, Width = innerW, Height = 22, AutoSize = false
        };
        content.Controls.Add(_wishStatusLabel);
        y += 26;

        _wishList = new ListView
        {
            Top = y, Left = PadX, Width = innerW, Height = 480,
            Font = FontBase, View = View.Details, FullRowSelect = true,
            BorderStyle = BorderStyle.FixedSingle, GridLines = false,
            ShowItemToolTips = true
        };
        _wishList.Columns.Add("User", 110);
        _wishList.Columns.Add("Wunsch", 320);
        _wishList.Columns.Add("Treffer", 70);
        _wishList.Columns.Add("Datum", 110);
        _wishList.DoubleClick += (_, _) => OpenWishDetail();
        content.Controls.Add(_wishList);
        y += 490;

        var detailBtn = NewButton("Details / Videos öffnen", 220);
        detailBtn.Top = y; detailBtn.Left = PadX;
        detailBtn.Click += (_, _) => OpenWishDetail();
        content.Controls.Add(detailBtn);

        ReloadWishesUi();

        // Background poll
        _wishPollTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _wishPollTimer.Tick += async (_, _) =>
        {
            if (!_settings.WishPollEnabled) return;
            var hours = Math.Max(1, _settings.WishPollIntervalHours);
            var last = _settings.LastWishPollAt;
            if (last.HasValue && DateTime.Now - last.Value < TimeSpan.FromHours(hours)) return;
            await PollWishesAsync(showPopup: false);
        };
        _wishPollTimer.Start();
    }

    private void ReloadWishesUi()
    {
        var wishes = AppSettings.LoadWishes()
            .OrderByDescending(w => w.FoundAt).ToList();
        _wishList.BeginUpdate();
        _wishList.Items.Clear();
        foreach (var w in wishes)
        {
            var it = new ListViewItem(w.User);
            it.SubItems.Add(w.Wish);
            it.SubItems.Add(w.Results.Count.ToString());
            it.SubItems.Add(w.FoundAt.ToString("dd.MM.yyyy HH:mm"));
            it.Tag = w;
            it.ToolTipText =
                $"Wunsch von {w.User}\n\n{w.Wish}\n\nGefundene Videos:\n" +
                string.Join("\n", w.Results.Take(5).Select(r => $"• {r.Title}"));
            _wishList.Items.Add(it);
        }
        _wishList.EndUpdate();
        _wishStatusLabel.Text = $"{wishes.Count} Wünsche gespeichert.";
    }

    private void OpenWishDetail()
    {
        if (_wishList.SelectedItems.Count == 0) return;
        var wish = _wishList.SelectedItems[0].Tag as WishEntry;
        if (wish == null) return;

        using var dlg = new Form
        {
            Text = $"Wunsch von {wish.User}",
            Width = 720, Height = 460, StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.Sizable, Font = FontBase
        };
        var lbl = new Label
        {
            Text = wish.Wish, Top = 12, Left = 16, Width = 680, Height = 40,
            Font = FontBold, ForeColor = TextDark, AutoSize = false
        };
        var lv = new ListView
        {
            Top = 60, Left = 16, Width = 680, Height = 300,
            Font = FontBase, View = View.Details, FullRowSelect = true, GridLines = false
        };
        lv.Columns.Add("Titel", 520);
        lv.Columns.Add("Dauer", 80);
        lv.Columns.Add("Video-ID", 70);
        foreach (var r in wish.Results)
        {
            var it = new ListViewItem(r.Title);
            it.SubItems.Add(FormatDuration(r.DurationSeconds));
            it.SubItems.Add(r.VideoId);
            it.Tag = r.VideoId;
            lv.Items.Add(it);
        }

        var openBtn = new Button { Text = "Auf YouTube öffnen", Top = 370, Left = 16, Width = 200 };
        openBtn.Click += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0) return;
            var vid = lv.SelectedItems[0].Tag as string;
            if (!string.IsNullOrEmpty(vid))
                Process.Start(new ProcessStartInfo { FileName = $"https://www.youtube.com/watch?v={vid}", UseShellExecute = true });
        };

        var topicBtn = new Button { Text = "In Auto-Queue einreihen", Top = 370, Left = 226, Width = 220 };
        topicBtn.Click += (_, _) =>
        {
            _settings.QueuedTopics.Add(wish.Wish);
            _settings.Save();
            MessageBox.Show(dlg, "In Auto-Modus-Queue eingereiht.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var closeBtn = new Button { Text = "Schließen", Top = 370, Left = 596, Width = 100, DialogResult = DialogResult.OK };

        dlg.Controls.AddRange([lbl, lv, openBtn, topicBtn, closeBtn]);
        dlg.AcceptButton = closeBtn;
        dlg.ShowDialog(this);
    }

    private async Task PollWishesAsync(bool showPopup)
    {
        if (string.IsNullOrWhiteSpace(_settings.DiscordBotToken) ||
            string.IsNullOrWhiteSpace(_settings.DiscordWishChannelId))
        {
            if (showPopup)
                MessageBox.Show(this, "Bitte in Settings Discord Bot-Token und Channel-ID eintragen.",
                    "Fehlt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _wishStatusLabel.Text = "Wünsche werden abgeholt...";
        try
        {
            var svc = new DiscordWishService(_settings);
            var log = (IProgress<string>)new Progress<string>(s => AppendAutoLog(s));
            var lang = _settings.DefaultLanguage ?? "de";
            var added = await svc.PollAndSearchAsync(lang, log, CancellationToken.None);
            ReloadWishesUi();
            if (added.Count > 0)
            {
                _trayIcon?.ShowBalloonTip(7000, "Neue Wünsche",
                    $"{added.Count} neue(r) Wunsch/Wünsche gefunden.", ToolTipIcon.Info);
            }
            if (showPopup)
                MessageBox.Show(this,
                    $"{added.Count} neue Wunsch-Treffer hinzugefügt.\nGesamt jetzt: {AppSettings.LoadWishes().Count}",
                    "Fertig", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _wishStatusLabel.Text = $"Fehler: {ex.Message}";
            if (showPopup) MessageBox.Show(this, ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ═══ Backup tab ════════════════════════════════════════════════════════

    private TextBox _backupFolderBox = null!;
    private CheckBox _autoBackupCheck = null!;
    private NumericUpDown _autoBackupIntervalBox = null!;
    private NumericUpDown _autoBackupKeepBox = null!;
    private ListView _backupList = null!;
    private Label _backupStatusLabel = null!;
    private System.Windows.Forms.Timer? _backupTimer;

    private void BuildBackupTab(TabPage tab)
    {
        var content = BuildScrollableTab(tab, 760);
        int innerW = ContentWidth - 2 * PadX;
        int y = 24;

        var head = new Label
        {
            Text = "Backup & Wiederherstellen",
            Font = new Font(FontBase.FontFamily, 18, FontStyle.Bold),
            ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 36, AutoSize = false
        };
        content.Controls.Add(head); y += 40;

        var hint = new Label
        {
            Text = "Sichert: settings.json, playlists.json, competitors.json, wishes.json. " +
                   "Restore überschreibt die aktuellen Dateien — App danach neu starten.",
            Font = FontHint, ForeColor = HintColor,
            Top = y, Left = PadX, Width = innerW, Height = 36, AutoSize = false
        };
        content.Controls.Add(hint); y += 40;

        // Backup-Ordner Pfad
        content.Controls.Add(new Label
        {
            Text = "Backup-Ordner (leer = AppData/CreatorDesktop/backups):",
            Font = FontLabel, ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 22, AutoSize = false
        });
        y += 24;

        _backupFolderBox = new TextBox
        {
            Font = FontBase, BorderStyle = BorderStyle.FixedSingle,
            Top = y, Left = PadX, Width = innerW - 130, Height = InputH
        };
        content.Controls.Add(_backupFolderBox);

        var browseBtn = NewButton("Durchsuchen", 120);
        browseBtn.Top = y; browseBtn.Left = PadX + innerW - 120;
        browseBtn.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Backup-Ordner wählen",
                ShowNewFolderButton = true,
                SelectedPath = !string.IsNullOrWhiteSpace(_backupFolderBox.Text)
                    ? _backupFolderBox.Text : DefaultBackupFolder()
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
                _backupFolderBox.Text = fbd.SelectedPath;
        };
        content.Controls.Add(browseBtn);
        y += InputH + 14;

        // Hauptbuttons
        var saveBtn = NewPrimaryButton("💾  Jetzt Backup erstellen", 240);
        saveBtn.Top = y; saveBtn.Left = PadX;
        saveBtn.Click += (_, _) => CreateBackupNow();
        content.Controls.Add(saveBtn);

        var loadBtn = NewButton("📂  Backup wiederherstellen", 230);
        loadBtn.Top = y; loadBtn.Left = PadX + 250;
        loadBtn.Click += (_, _) => RestoreBackup();
        content.Controls.Add(loadBtn);

        var openFolderBtn = NewButton("Ordner öffnen", 130);
        openFolderBtn.Top = y; openFolderBtn.Left = PadX + 490;
        openFolderBtn.Click += (_, _) =>
        {
            var folder = GetEffectiveBackupFolder();
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        };
        content.Controls.Add(openFolderBtn);
        y += ButtonH + 18;

        // Auto-Backup-Sektion
        _autoBackupCheck = new CheckBox
        {
            Text = "Automatisches Backup aktivieren (im Hintergrund)",
            Font = FontLabel, ForeColor = TextDark, Checked = false,
            Top = y, Left = PadX, Width = innerW, Height = 26, AutoSize = false
        };
        _autoBackupCheck.CheckedChanged += (_, _) => SaveBackupSettings();
        content.Controls.Add(_autoBackupCheck);
        y += 30;

        content.Controls.Add(new Label
        {
            Text = "Alle X Stunden:", Font = FontLabel, ForeColor = TextDark,
            Top = y, Left = PadX + 24, Width = 140, Height = InputH,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        _autoBackupIntervalBox = new NumericUpDown
        {
            Minimum = 1, Maximum = 168, Value = 24, Font = FontBase,
            Width = 70, Height = InputH, Top = y, Left = PadX + 168
        };
        _autoBackupIntervalBox.ValueChanged += (_, _) => SaveBackupSettings();
        content.Controls.Add(_autoBackupIntervalBox);

        content.Controls.Add(new Label
        {
            Text = "Alte Backups löschen nach X Tagen:", Font = FontLabel, ForeColor = TextDark,
            Top = y, Left = PadX + 260, Width = 250, Height = InputH,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        _autoBackupKeepBox = new NumericUpDown
        {
            Minimum = 1, Maximum = 365, Value = 30, Font = FontBase,
            Width = 70, Height = InputH, Top = y, Left = PadX + 514
        };
        _autoBackupKeepBox.ValueChanged += (_, _) => SaveBackupSettings();
        content.Controls.Add(_autoBackupKeepBox);
        y += InputH + 16;

        _backupStatusLabel = new Label
        {
            Font = FontBold, ForeColor = TextMuted,
            Top = y, Left = PadX, Width = innerW, Height = 22, AutoSize = false
        };
        content.Controls.Add(_backupStatusLabel);
        y += 28;

        // Liste vorhandener Backups
        content.Controls.Add(new Label
        {
            Text = "Vorhandene Backups (Doppelklick = wiederherstellen):",
            Font = FontLabel, ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 22, AutoSize = false
        });
        y += 24;

        _backupList = new ListView
        {
            Top = y, Left = PadX, Width = innerW, Height = 320,
            Font = FontBase, View = View.Details, FullRowSelect = true,
            BorderStyle = BorderStyle.FixedSingle, GridLines = false
        };
        _backupList.Columns.Add("Datum", 180);
        _backupList.Columns.Add("Datei", 380);
        _backupList.Columns.Add("Größe", 100);
        _backupList.DoubleClick += (_, _) =>
        {
            if (_backupList.SelectedItems.Count == 0) return;
            var path = _backupList.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(path)) return;
            if (MessageBox.Show(this,
                    $"Backup vom {_backupList.SelectedItems[0].Text} jetzt wiederherstellen?\n\n" +
                    "Aktuelle settings.json + playlists.json + etc. werden überschrieben.\n" +
                    "App muss danach neu gestartet werden.",
                    "Backup wiederherstellen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                != DialogResult.Yes) return;
            RestoreFromPath(path);
        };
        content.Controls.Add(_backupList);

        ReloadBackupList();
        InitBackupTimer();
    }

    private string DefaultBackupFolder() =>
        Path.Combine(AppSettings.DefaultDataDir, "backups");

    private string GetEffectiveBackupFolder() =>
        string.IsNullOrWhiteSpace(_settings.BackupFolder)
            ? DefaultBackupFolder() : _settings.BackupFolder;

    private void SaveBackupSettings()
    {
        _settings.BackupFolder = _backupFolderBox.Text.Trim();
        _settings.AutoBackupEnabled = _autoBackupCheck.Checked;
        _settings.AutoBackupIntervalHours = (int)_autoBackupIntervalBox.Value;
        _settings.AutoBackupKeepDays = (int)_autoBackupKeepBox.Value;
        _settings.Save();
    }

    private void CreateBackupNow()
    {
        SaveBackupSettings();
        try
        {
            // Persist current settings to disk first so backup contains latest state
            _settings.Save();
            var folder = GetEffectiveBackupFolder();
            var path = BackupService.CreateBackup(folder);
            _settings.LastBackupAt = DateTime.Now;
            _settings.Save();
            BackupService.PruneOldBackups(folder, _settings.AutoBackupKeepDays);
            ReloadBackupList();
            MessageBox.Show(this, $"Backup erstellt:\n{path}",
                "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Backup-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RestoreBackup()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Backup-Dateien (*.zip)|*.zip",
            Title = "Backup-Datei wählen",
            InitialDirectory = GetEffectiveBackupFolder()
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;
        if (MessageBox.Show(this,
                "Aktuelle settings.json + playlists.json + etc. werden überschrieben.\n" +
                "App muss danach neu gestartet werden. Wirklich fortfahren?",
                "Wiederherstellen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            != DialogResult.Yes) return;
        RestoreFromPath(ofd.FileName);
    }

    private void RestoreFromPath(string zipPath)
    {
        try
        {
            var count = BackupService.RestoreBackup(zipPath);
            MessageBox.Show(this,
                $"{count} Datei(en) wiederhergestellt.\n\nBitte App jetzt schließen und neu starten.",
                "Wiederhergestellt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Restore-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReloadBackupList()
    {
        var folder = GetEffectiveBackupFolder();
        var backups = BackupService.ListBackups(folder);
        _backupList.BeginUpdate();
        _backupList.Items.Clear();
        foreach (var (path, date, size) in backups)
        {
            var it = new ListViewItem(date.ToString("dd.MM.yyyy HH:mm:ss"));
            it.SubItems.Add(Path.GetFileName(path));
            it.SubItems.Add($"{size / 1024.0:0.#} KB");
            it.Tag = path;
            _backupList.Items.Add(it);
        }
        _backupList.EndUpdate();
        _backupStatusLabel.Text = backups.Count == 0
            ? "Noch kein Backup vorhanden."
            : $"{backups.Count} Backup(s) — letztes: {backups[0].Date:dd.MM.yyyy HH:mm:ss}";
    }

    private void InitBackupTimer()
    {
        _backupTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _backupTimer.Tick += (_, _) =>
        {
            if (!_settings.AutoBackupEnabled) return;
            var hours = Math.Max(1, _settings.AutoBackupIntervalHours);
            var last = _settings.LastBackupAt;
            if (last.HasValue && DateTime.Now - last.Value < TimeSpan.FromHours(hours)) return;
            try
            {
                var folder = GetEffectiveBackupFolder();
                BackupService.CreateBackup(folder);
                _settings.LastBackupAt = DateTime.Now;
                _settings.Save();
                BackupService.PruneOldBackups(folder, _settings.AutoBackupKeepDays);
                ReloadBackupList();
                AppendAutoLog($"Auto-Backup erstellt ({folder}).");
            }
            catch (Exception ex) { AppendAutoLog($"Auto-Backup Fehler: {ex.Message}"); }
        };
        _backupTimer.Start();
    }

    // ═══ Archive.org tab ════════════════════════════════════════════════════

    private void BuildArchiveTab(TabPage tab)
    {
        var content = BuildScrollableTab(tab, 720);
        int innerW = ContentWidth - 2 * PadX;
        int y = 24;

        content.Controls.Add(new Label
        {
            Text = "Archive.org — Filme & Dokumentationen",
            Font = new Font(FontBase.FontFamily, 18, FontStyle.Bold),
            ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 36, AutoSize = false
        });
        y += 40;

        content.Controls.Add(new Label
        {
            Text = "Durchsucht archive.org — Public-Domain-Filme, alte Dokus, Wochenschauen. " +
                   "Alles legal nutzbar, keine API-Keys, kein Bot-Check.",
            Font = FontHint, ForeColor = HintColor,
            Top = y, Left = PadX, Width = innerW, Height = 32, AutoSize = false
        });
        y += 36;

        // Topic dropdown — gleicher Inhalt wie Create-Tab
        content.Controls.Add(new Label
        {
            Text = "Thema auswählen (oder unten frei eingeben):",
            Font = FontLabel, ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = LabelH, AutoSize = false
        });
        y += LabelH + 2;

        _archiveTopicBox = NewEditableCombo("", DocumentaryTopics);
        _archiveTopicBox.Top = y; _archiveTopicBox.Left = PadX;
        _archiveTopicBox.Width = innerW - 200;
        _archiveTopicBox.SelectedIndexChanged += async (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_archiveTopicBox.Text))
            {
                _archiveQueryBox.Text = _archiveTopicBox.Text;
                await ArchiveSearchAsync();
            }
        };
        content.Controls.Add(_archiveTopicBox);

        _archiveGermanOnly = new CheckBox
        {
            Text = "🇩🇪 Nur Deutsch",
            Font = FontLabel, ForeColor = TextDark, Checked = false,
            Top = y + 4, Left = PadX + innerW - 180, Width = 180, Height = 26, AutoSize = false
        };
        content.Controls.Add(_archiveGermanOnly);
        y += InputH + 10;

        // Search field
        content.Controls.Add(new Label
        {
            Text = "Suchbegriff (z.B. \"WW2\", \"space\", \"titanic\"):",
            Font = FontLabel, ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = LabelH, AutoSize = false
        });
        y += LabelH + 2;

        _archiveQueryBox = new TextBox
        {
            Font = FontBase, BorderStyle = BorderStyle.FixedSingle,
            Top = y, Left = PadX, Width = innerW, Height = InputH
        };
        content.Controls.Add(_archiveQueryBox);
        y += InputH + 10;

        // Year filter + max + buttons in one row
        content.Controls.Add(new Label
        {
            Text = "Jahr von:", Font = FontLabel, ForeColor = TextDark,
            Top = y + 6, Left = PadX, Width = 80, Height = LabelH, AutoSize = false
        });
        _archiveYearFromBox = new NumericUpDown
        {
            Top = y + 4, Left = PadX + 80, Width = 90, Height = ButtonH,
            Minimum = 1850, Maximum = DateTime.Now.Year, Value = 1900, Font = FontLabel
        };
        content.Controls.Add(_archiveYearFromBox);

        content.Controls.Add(new Label
        {
            Text = "bis:", Font = FontLabel, ForeColor = TextDark,
            Top = y + 6, Left = PadX + 180, Width = 30, Height = LabelH, AutoSize = false
        });
        _archiveYearToBox = new NumericUpDown
        {
            Top = y + 4, Left = PadX + 210, Width = 90, Height = ButtonH,
            Minimum = 1850, Maximum = DateTime.Now.Year, Value = DateTime.Now.Year, Font = FontLabel
        };
        content.Controls.Add(_archiveYearToBox);

        content.Controls.Add(new Label
        {
            Text = "Max:", Font = FontLabel, ForeColor = TextDark,
            Top = y + 6, Left = PadX + 320, Width = 40, Height = LabelH, AutoSize = false
        });
        _archiveMaxBox = new NumericUpDown
        {
            Top = y + 4, Left = PadX + 360, Width = 80, Height = ButtonH,
            Minimum = 25, Maximum = 1000, Increment = 25, Value = 100, Font = FontLabel
        };
        content.Controls.Add(_archiveMaxBox);

        _archiveSearchBtn = NewPrimaryButton("🔍  Suchen", 140);
        _archiveSearchBtn.Top = y; _archiveSearchBtn.Left = PadX + 460;
        _archiveSearchBtn.Click += async (_, _) => await ArchiveSearchAsync();
        content.Controls.Add(_archiveSearchBtn);

        y += ButtonH + 8;

        _archiveStatusLabel = new Label
        {
            Text = "Tipp: Jahr 1850-1928 = garantiert Public Domain. 1929-1955 oft auch.",
            Font = FontHint, ForeColor = TextMuted,
            Top = y, Left = PadX, Width = innerW, Height = HintH, AutoSize = false
        };
        content.Controls.Add(_archiveStatusLabel);
        y += HintH + 4;

        // Result list
        _archiveListView = new ListView
        {
            View = View.Details, CheckBoxes = true, FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable, ShowItemToolTips = true,
            Top = y, Left = PadX, Width = innerW, Height = 320,
            Font = FontLabel, BackColor = CardColor, BorderStyle = BorderStyle.FixedSingle
        };
        _archiveListView.Columns.Add("Titel", innerW - 400);
        _archiveListView.Columns.Add("Jahr", 60);
        _archiveListView.Columns.Add("Dauer", 80);
        _archiveListView.Columns.Add("Lizenz", 160);
        _archiveListView.Columns.Add("ID", 0);
        content.Controls.Add(_archiveListView);
        y += 328;

        // Bottom buttons
        var selectAllBtn = NewButton("☑ Alle", 88);
        selectAllBtn.Top = y; selectAllBtn.Left = PadX;
        selectAllBtn.Click += (_, _) => { foreach (ListViewItem it in _archiveListView.Items) it.Checked = true; };
        content.Controls.Add(selectAllBtn);

        var selectNoneBtn = NewButton("☐ Keine", 88);
        selectNoneBtn.Top = y; selectNoneBtn.Left = PadX + 96;
        selectNoneBtn.Click += (_, _) => { foreach (ListViewItem it in _archiveListView.Items) it.Checked = false; };
        content.Controls.Add(selectNoneBtn);

        _archiveDownloadBtn = NewPrimaryButton("▼  Markierte herunterladen", 260);
        _archiveDownloadBtn.Top = y; _archiveDownloadBtn.Left = PadX + innerW - 260;
        _archiveDownloadBtn.Enabled = false;
        _archiveDownloadBtn.Click += async (_, _) => await ArchiveDownloadAsync();
        content.Controls.Add(_archiveDownloadBtn);
    }

    private async Task ArchiveSearchAsync()
    {
        var query = _archiveQueryBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            MessageBox.Show(this, "Bitte einen Suchbegriff eingeben.", "Leer",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _archiveSearchBtn.Enabled = false;
        _archiveDownloadBtn.Enabled = false;
        _archiveListView.Items.Clear();
        _archiveStatusLabel.Text = "Suche bei archive.org...";

        try
        {
            var svc = new ArchiveOrgService();
            var items = await svc.SearchAsync(query,
                (int)_archiveMaxBox.Value,
                (int)_archiveYearFromBox.Value,
                (int)_archiveYearToBox.Value,
                CancellationToken.None,
                germanOnly: _archiveGermanOnly.Checked);

            var historyIds = new HashSet<string>(_settings.DownloadedVideoIds);
            int green = 0, yellow = 0, redCnt = 0, grey = 0;
            _archiveListView.BeginUpdate();
            foreach (var it in items)
            {
                var row = new ListViewItem(it.Title);
                row.SubItems.Add(it.Year?.ToString() ?? "");
                row.SubItems.Add(it.DurationSeconds > 0 ? FormatDuration(it.DurationSeconds) : "");

                var (label, color) = it.License switch
                {
                    ArchiveOrgService.LicenseClass.PublicDomain    => ("✓ Public Domain",     Color.FromArgb(40, 140, 60)),
                    ArchiveOrgService.LicenseClass.CreativeCommons => ("✓ Creative Commons",  Color.FromArgb(40, 140, 60)),
                    ArchiveOrgService.LicenseClass.NonCommercial   => ("⚠ Nicht-kommerziell", Color.FromArgb(200, 140, 0)),
                    ArchiveOrgService.LicenseClass.Restricted      => ("✗ Geschützt",         Color.FromArgb(180, 60, 60)),
                    _ => ("? Lizenz unbekannt", Color.FromArgb(120, 120, 120))
                };
                row.SubItems.Add(label);
                row.SubItems.Add(it.Identifier);
                row.ForeColor = color;
                row.ToolTipText =
                    $"Lizenz: {(string.IsNullOrEmpty(it.LicenseUrl) ? "nicht angegeben" : it.LicenseUrl)}\n" +
                    $"Sammlung: {it.Collection}\n" +
                    (string.IsNullOrWhiteSpace(it.Description) ? "" : it.Description);

                if (historyIds.Contains("ia:" + it.Identifier))
                {
                    row.ForeColor = Color.FromArgb(180, 60, 60);
                    row.SubItems[0].Text = "✓ " + it.Title;
                }

                switch (it.License)
                {
                    case ArchiveOrgService.LicenseClass.PublicDomain:
                    case ArchiveOrgService.LicenseClass.CreativeCommons: green++; break;
                    case ArchiveOrgService.LicenseClass.NonCommercial: yellow++; break;
                    case ArchiveOrgService.LicenseClass.Restricted: redCnt++; break;
                    default: grey++; break;
                }
                _archiveListView.Items.Add(row);
            }
            _archiveListView.EndUpdate();
            _archiveStatusLabel.Text =
                $"{items.Count} Treffer — 🟢 {green} frei · 🟡 {yellow} nicht-kommerziell · ⚪ {grey} unbekannt · 🔴 {redCnt} geschützt";
            _archiveDownloadBtn.Enabled = items.Count > 0;
        }
        catch (Exception ex)
        {
            _archiveStatusLabel.Text = $"Fehler: {ex.Message}";
        }
        finally { _archiveSearchBtn.Enabled = true; }
    }

    private async Task ArchiveDownloadAsync()
    {
        var checkedRows = _archiveListView.CheckedItems.Cast<ListViewItem>().ToList();
        if (checkedRows.Count == 0)
        {
            MessageBox.Show(this, "Bitte erst Videos ankreuzen.", "Leer",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _archiveDownloadBtn.Enabled = false;
        _archiveSearchBtn.Enabled = false;
        _cts = new CancellationTokenSource();
        _progressBar.Style = ProgressBarStyle.Marquee;
        _logBox.Clear();

        var workDir = Path.Combine(_settings.OutputFolder, $"archive_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(workDir);
        _lastJob = new VideoJob { WorkDir = workDir, Topic = "Archive.org" };

        var log = (IProgress<string>)new Progress<string>(s =>
        {
            _statusLabel.Text = s;
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}\r\n");
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.ScrollToCaret();
        });

        int done = 0, failed = 0;
        try
        {
            for (int i = 0; i < checkedRows.Count; i++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var row = checkedRows[i];
                var id = row.SubItems.Count > 4 ? row.SubItems[4].Text : "";
                var title = row.Text.TrimStart('✓', ' ');
                if (string.IsNullOrEmpty(id)) continue;
                log.Report($"[{i + 1}/{checkedRows.Count}] {title}");

                if (i > 0)
                {
                    log.Report("  Pause 4 Sek....");
                    await Task.Delay(4000, _cts.Token);
                }

                try
                {
                    var path = await ArchiveOrgService.DownloadAsync(id, workDir, log, _cts.Token);
                    _settings.DownloadedVideoIds.Add("ia:" + id);
                    _settings.Save();
                    _lastJob.Scenes.Add(new Scene
                    {
                        Order = done + 1, Title = title, SegmentPath = path,
                        SourceVideoId = "ia:" + id,
                        SourceLicense = "Public Domain (archive.org)"
                    });
                    done++;
                    row.ForeColor = Color.FromArgb(40, 140, 60);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Report($"  Fehler: {ex.Message}");
                    failed++;
                }
            }
            log.Report($"Fertig: {done} heruntergeladen, {failed} Fehler.");
        }
        catch (OperationCanceledException) { log.Report("Abgebrochen."); }
        finally
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Value = 0;
            _archiveDownloadBtn.Enabled = true;
            _archiveSearchBtn.Enabled = true;
            if (done > 0) _uploadBtn.Enabled = true;
        }
    }

    // ═══ Tools tab (Shorts + Thumbnail + Music) ════════════════════════════

    private void BuildToolsTab(TabPage tab)
    {
        var content = BuildScrollableTab(tab, 1100);
        int innerW = ContentWidth - 2 * PadX;
        int y = 24;

        content.Controls.Add(new Label
        {
            Text = "Tools — Shorts, KI-Thumbnails, Musik",
            Font = new Font(FontBase.FontFamily, 18, FontStyle.Bold), ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 36, AutoSize = false
        });
        y += 44;

        // ── Shorts-Cutter ─────────────────────────────────────────────────
        y = AddCard(content, y, innerW, "🎬 Shorts aus letztem Video schneiden",
            "Nimmt das zuletzt erstellte/heruntergeladene Video und schneidet es in N vertikale 9:16-Clips für YouTube Shorts.",
            (panel, py) =>
            {
                panel.Controls.Add(new Label
                {
                    Text = "Anzahl Clips:", Font = FontLabel, ForeColor = TextDark,
                    Top = py + 6, Left = 12, Width = 110, Height = LabelH, AutoSize = false
                });
                _shortsCountBox = new NumericUpDown
                {
                    Top = py + 4, Left = 130, Width = 70, Height = ButtonH,
                    Minimum = 1, Maximum = 30, Value = 8, Font = FontLabel
                };
                panel.Controls.Add(_shortsCountBox);

                panel.Controls.Add(new Label
                {
                    Text = "Sek/Clip:", Font = FontLabel, ForeColor = TextDark,
                    Top = py + 6, Left = 220, Width = 80, Height = LabelH, AutoSize = false
                });
                _shortsDurationBox = new NumericUpDown
                {
                    Top = py + 4, Left = 300, Width = 70, Height = ButtonH,
                    Minimum = 15, Maximum = 180, Value = 50, Font = FontLabel
                };
                panel.Controls.Add(_shortsDurationBox);

                var cutBtn = NewPrimaryButton("Schneiden", 180);
                cutBtn.Top = py; cutBtn.Left = 390;
                cutBtn.Click += async (_, _) => await CutShortsAsync();
                panel.Controls.Add(cutBtn);
                return py + ButtonH + 8;
            });

        // ── KI-Thumbnail ──────────────────────────────────────────────────
        y = AddCard(content, y, innerW, "🎨 KI-Thumbnail generieren (DALL-E 3)",
            "Erstellt ein cinematisches YouTube-Thumbnail aus Titel + Thema. Braucht den OpenAI-API-Key.",
            (panel, py) =>
            {
                panel.Controls.Add(new Label
                {
                    Text = "Titel:", Font = FontLabel, ForeColor = TextDark,
                    Top = py + 6, Left = 12, Width = 60, Height = LabelH, AutoSize = false
                });
                _thumbTitleBox = new TextBox
                {
                    Top = py + 4, Left = 80, Width = panel.Width - 100, Height = InputH,
                    Font = FontBase, BorderStyle = BorderStyle.FixedSingle
                };
                panel.Controls.Add(_thumbTitleBox);
                py += InputH + 8;

                panel.Controls.Add(new Label
                {
                    Text = "Thema:", Font = FontLabel, ForeColor = TextDark,
                    Top = py + 6, Left = 12, Width = 60, Height = LabelH, AutoSize = false
                });
                _thumbTopicBox = new TextBox
                {
                    Top = py + 4, Left = 80, Width = panel.Width - 100, Height = InputH,
                    Font = FontBase, BorderStyle = BorderStyle.FixedSingle
                };
                panel.Controls.Add(_thumbTopicBox);
                py += InputH + 10;

                var genBtn = NewPrimaryButton("🎨 Thumbnail generieren", 240);
                genBtn.Top = py; genBtn.Left = 12;
                genBtn.Click += async (_, _) => await GenerateThumbnailAsync();
                panel.Controls.Add(genBtn);
                return py + ButtonH + 8;
            });

        // ── Musik-Suche ───────────────────────────────────────────────────
        y = AddCard(content, y, innerW, "🎵 Musik-Suche (Pixabay + Archive.org)",
            "Findet gratis Hintergrundmusik — Pixabay-Tracks sind kommerziell nutzbar (auch YT-Monetarisierung). Pixabay-Key in Settings eintragen für mehr Treffer.",
            (panel, py) =>
            {
                _musicQueryBox = new TextBox
                {
                    Top = py + 4, Left = 12, Width = panel.Width - 200, Height = InputH,
                    Font = FontBase, BorderStyle = BorderStyle.FixedSingle,
                    PlaceholderText = "z.B. \"cinematic\", \"epic\", \"sad piano\"..."
                };
                panel.Controls.Add(_musicQueryBox);

                var searchBtn = NewPrimaryButton("Suchen", 160);
                searchBtn.Top = py; searchBtn.Left = panel.Width - 180;
                searchBtn.Click += async (_, _) => await SearchMusicAsync();
                panel.Controls.Add(searchBtn);
                py += InputH + 8;

                _musicListView = new ListView
                {
                    View = View.Details, FullRowSelect = true, GridLines = false,
                    Top = py, Left = 12, Width = panel.Width - 24, Height = 200,
                    Font = FontLabel, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle
                };
                _musicListView.Columns.Add("Titel", 320);
                _musicListView.Columns.Add("Künstler", 180);
                _musicListView.Columns.Add("Dauer", 80);
                _musicListView.Columns.Add("URL", 0);
                _musicListView.DoubleClick += (_, _) => DownloadSelectedMusic();
                panel.Controls.Add(_musicListView);
                py += 208;

                var dlBtn = NewButton("⬇ Markierten Track herunterladen", 280);
                dlBtn.Top = py; dlBtn.Left = 12;
                dlBtn.Click += (_, _) => DownloadSelectedMusic();
                panel.Controls.Add(dlBtn);
                return py + ButtonH + 8;
            });

        // ── Log ───────────────────────────────────────────────────────────
        content.Controls.Add(new Label
        {
            Text = "Log:", Font = FontLabel, ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = LabelH, AutoSize = false
        });
        y += LabelH;
        _toolsLogBox = new TextBox
        {
            Top = y, Left = PadX, Width = innerW, Height = 140,
            Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true,
            Font = new Font("Consolas", 9), BackColor = Color.FromArgb(245, 247, 250)
        };
        content.Controls.Add(_toolsLogBox);
    }

    private int AddCard(Panel content, int y, int innerW, string title, string hint,
        Func<Panel, int, int> build)
    {
        var card = new Panel
        {
            Top = y, Left = PadX, Width = innerW, BackColor = CardColor,
            BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(12)
        };
        card.Controls.Add(new Label
        {
            Text = title, Font = FontBold, ForeColor = TextDark,
            Top = 8, Left = 12, Width = innerW - 32, Height = 24, AutoSize = false
        });
        card.Controls.Add(new Label
        {
            Text = hint, Font = FontHint, ForeColor = HintColor,
            Top = 34, Left = 12, Width = innerW - 32, Height = 32, AutoSize = false
        });
        var pyEnd = build(card, 72);
        card.Height = pyEnd + 12;
        content.Controls.Add(card);
        return y + card.Height + 12;
    }

    private void ToolsLog(string s)
    {
        if (_toolsLogBox.InvokeRequired)
        {
            // Marshal first — this method calls itself, so logging here would double every line.
            _toolsLogBox.Invoke(() => ToolsLog(s));
            return;
        }
        RunLog.Write(s);
        _toolsLogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}\r\n");
        _toolsLogBox.SelectionStart = _toolsLogBox.TextLength;
        _toolsLogBox.ScrollToCaret();
    }

    private async Task CutShortsAsync()
    {
        var src = _lastJob?.OutputVideoPath
                  ?? _lastJob?.Scenes.FirstOrDefault()?.SegmentPath;
        if (src == null || !File.Exists(src))
        {
            MessageBox.Show(this, "Kein Video gefunden — bitte zuerst eine Doku oder einen Download erstellen.",
                "Leer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var outFolder = Path.Combine(_settings.OutputFolder, $"shorts_{DateTime.Now:yyyyMMdd_HHmmss}");
        ToolsLog($"Schneide Shorts aus {Path.GetFileName(src)}...");
        try
        {
            var clips = await ShortsCutterService.CutAsync(src, outFolder,
                (int)_shortsCountBox.Value, (int)_shortsDurationBox.Value,
                new Progress<string>(ToolsLog), CancellationToken.None);
            ToolsLog($"Fertig: {clips.Count} Shorts in {outFolder}");
            Process.Start(new ProcessStartInfo { FileName = outFolder, UseShellExecute = true });
        }
        catch (Exception ex) { ToolsLog($"Fehler: {ex.Message}"); }
    }

    private async Task GenerateThumbnailAsync()
    {
        var title = _thumbTitleBox.Text.Trim();
        var topic = _thumbTopicBox.Text.Trim();
        if (string.IsNullOrEmpty(title)) { MessageBox.Show(this, "Bitte einen Titel eingeben."); return; }
        if (string.IsNullOrEmpty(topic)) topic = title;

        var outFolder = Path.Combine(_settings.OutputFolder, "thumbnails");
        try
        {
            var svc = new AiThumbnailService(_settings);
            var path = await svc.GenerateAsync(title, topic, outFolder,
                new Progress<string>(ToolsLog), CancellationToken.None);
            ToolsLog($"Thumbnail erstellt: {path}");
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex) { ToolsLog($"Fehler: {ex.Message}"); }
    }

    private async Task SearchMusicAsync()
    {
        var q = _musicQueryBox.Text.Trim();
        if (string.IsNullOrEmpty(q)) { MessageBox.Show(this, "Bitte einen Suchbegriff eingeben."); return; }
        _musicListView.Items.Clear();
        ToolsLog($"Suche Musik: {q}");
        try
        {
            var svc = new PixabayMusicService(_settings);
            var tracks = await svc.SearchAsync(q, CancellationToken.None);
            foreach (var t in tracks)
            {
                var row = new ListViewItem(t.Title);
                row.SubItems.Add(t.Artist);
                row.SubItems.Add(t.DurationSec > 0 ? FormatDuration(t.DurationSec) : "");
                row.SubItems.Add(t.DownloadUrl);
                _musicListView.Items.Add(row);
            }
            ToolsLog($"{tracks.Count} Tracks gefunden. Doppelklick zum Download.");
        }
        catch (Exception ex) { ToolsLog($"Fehler: {ex.Message}"); }
    }

    private async void DownloadSelectedMusic()
    {
        if (_musicListView.SelectedItems.Count == 0)
        { MessageBox.Show(this, "Bitte erst einen Track auswählen."); return; }
        var sel = _musicListView.SelectedItems[0];
        var url = sel.SubItems.Count > 3 ? sel.SubItems[3].Text : "";
        if (string.IsNullOrEmpty(url)) return;

        var outFolder = Path.Combine(_settings.OutputFolder, "music");
        var track = new PixabayMusicService.MusicTrack(
            0, sel.Text, sel.SubItems[1].Text, 0, url, url, new());
        try
        {
            var path = await new PixabayMusicService(_settings).DownloadAsync(
                track, outFolder, CancellationToken.None);
            ToolsLog($"Musik gespeichert: {path}");
        }
        catch (Exception ex) { ToolsLog($"Fehler: {ex.Message}"); }
    }

    // ═══ Stats tab ═══════════════════════════════════════════════════════════

    private void BuildStatsTab(TabPage tab)
    {
        var content = BuildScrollableTab(tab, 720);
        int innerW = ContentWidth - 2 * PadX;
        int y = 24;

        content.Controls.Add(new Label
        {
            Text = "Channel-Statistik",
            Font = new Font(FontBase.FontFamily, 18, FontStyle.Bold), ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 36, AutoSize = false
        });
        y += 40;

        content.Controls.Add(new Label
        {
            Text = "Zeigt die letzten 50 hochgeladenen Videos mit Views, Likes, Kommentaren, CTR und Watch-Time.",
            Font = FontHint, ForeColor = HintColor,
            Top = y, Left = PadX, Width = innerW, Height = 32, AutoSize = false
        });
        y += 36;

        var loadBtn = NewPrimaryButton("📊 Statistik laden", 220);
        loadBtn.Top = y; loadBtn.Left = PadX;
        loadBtn.Click += async (_, _) => await LoadStatsAsync();
        content.Controls.Add(loadBtn);

        _statsHeaderLabel = new Label
        {
            Text = "", Font = FontLabel, ForeColor = TextDark,
            Top = y + 6, Left = PadX + 240, Width = innerW - 240, Height = LabelH, AutoSize = false
        };
        content.Controls.Add(_statsHeaderLabel);
        y += ButtonH + 16;

        _statsListView = new ListView
        {
            View = View.Details, FullRowSelect = true, GridLines = false,
            Top = y, Left = PadX, Width = innerW, Height = 460,
            Font = FontLabel, BackColor = CardColor, BorderStyle = BorderStyle.FixedSingle
        };
        _statsListView.Columns.Add("Titel", innerW - 540);
        _statsListView.Columns.Add("Datum", 100);
        _statsListView.Columns.Add("Views", 80);
        _statsListView.Columns.Add("Likes", 70);
        _statsListView.Columns.Add("Komm.", 70);
        _statsListView.Columns.Add("CTR %", 80);
        _statsListView.Columns.Add("Avg. View", 100);
        content.Controls.Add(_statsListView);
    }

    private async Task LoadStatsAsync()
    {
        _statsListView.Items.Clear();
        _statsHeaderLabel.Text = "Lade...";
        try
        {
            var yt = new YouTubeService(_settings);
            var svc = new YouTubeStatsService(yt, _settings);
            var stats = await svc.GetRecentAsync(50,
                new Progress<string>(s => _statsHeaderLabel.Text = s),
                CancellationToken.None);

            long totalViews = 0;
            foreach (var s in stats)
            {
                totalViews += s.Views;
                var row = new ListViewItem(s.Title);
                row.SubItems.Add(s.PublishedAt.ToString("yyyy-MM-dd"));
                row.SubItems.Add(s.Views.ToString("N0"));
                row.SubItems.Add(s.Likes.ToString("N0"));
                row.SubItems.Add(s.Comments.ToString("N0"));
                row.SubItems.Add(s.CtrPct.HasValue ? s.CtrPct.Value.ToString("F2") : "—");
                row.SubItems.Add(s.AvgViewSecs.HasValue
                    ? FormatDuration((int)s.AvgViewSecs.Value) : "—");

                // Color by CTR
                if (s.CtrPct.HasValue)
                {
                    row.ForeColor = s.CtrPct.Value >= 6 ? Color.FromArgb(40, 140, 60)
                        : s.CtrPct.Value >= 3 ? Color.FromArgb(200, 140, 0)
                        : Color.FromArgb(180, 60, 60);
                }
                _statsListView.Items.Add(row);
            }
            _statsHeaderLabel.Text = $"{stats.Count} Videos · {totalViews:N0} Views gesamt · grün ≥6 % CTR, gelb ≥3 %, rot <3 %";
        }
        catch (Exception ex) { _statsHeaderLabel.Text = $"Fehler: {ex.Message}"; }
    }

    // ═══ Settings tab ═══════════════════════════════════════════════════════

    private void BuildSettingsTab(TabPage tab)
    {
        var content = BuildScrollableTab(tab, 1400);
        int y = 24;
        int innerW = ContentWidth - PadX * 2;

        content.Controls.Add(new Label
        {
            Text = "Einstellungen", Font = FontTitle, ForeColor = TextDark,
            Top = y, Left = PadX, Width = innerW, Height = 32,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        y += 40;

        content.Controls.Add(new Label
        {
            Text = "Tragen Sie hier Ihre API-Schlüssel ein. Unter jedem Feld steht, " +
                   "woher Sie den Schlüssel bekommen und wie er aussieht.",
            Font = FontLabel, ForeColor = TextMuted,
            Top = y, Left = PadX, Width = innerW, Height = 36,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        y += 44;

        // ── YouTube Bot-Check ─────────────────────────────────────────────────
        y = AddSection(content, y, innerW, "0. YouTube Bot-Check / 403 beheben (wichtig!)", section =>
        {
            int sy = 16;
            section.Controls.Add(new Label
            {
                Text = "Wenn yt-dlp den Fehler \"Sign in to confirm you're not a bot\" zeigt:\n" +
                       "1. Edge auf diesem Server öffnen und bei youtube.com einloggen.\n" +
                       "2. Unten \"Edge\" auswählen und Einstellungen speichern — fertig.\n" +
                       "   (Alternativ \"Keiner\" wenn kein Browser installiert ist.)\n" +
                       "Bei \"HTTP Error 403: Forbidden\" ist meist yt-dlp veraltet — unten aktualisieren.",
                Font = FontHint, ForeColor = HintColor,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 82,
                TextAlign = ContentAlignment.TopLeft, AutoSize = false
            });
            sy += 88;
            _cookiesBrowserBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = FontLabel, ForeColor = TextDark,
                Top = 36 + sy, Left = 24, Width = 200
            };
            _cookiesBrowserBox.Items.AddRange(["Keiner", "edge", "chrome", "firefox", "chromium"]);
            _cookiesBrowserBox.SelectedIndex = 0;
            section.Controls.Add(_cookiesBrowserBox);
            section.Controls.Add(new Label
            {
                Text = "Browser für yt-dlp Cookies",
                Font = FontLabel, ForeColor = TextDark,
                Top = 36 + sy + 3, Left = 232, Width = section.Width - 264, Height = LabelH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            sy += LabelH + 8;

            _ytDlpAutoUpdateCheck = new CheckBox
            {
                Text = "yt-dlp automatisch aktuell halten (empfohlen)",
                Font = FontLabel, ForeColor = TextDark, Checked = true,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_ytDlpAutoUpdateCheck);
            sy += 30;

            _ytDlpUpdateBtn = NewButton("yt-dlp jetzt aktualisieren", 230);
            _ytDlpUpdateBtn.Top = 36 + sy; _ytDlpUpdateBtn.Left = 24;
            _ytDlpUpdateBtn.Click += async (_, _) => await UpdateYtDlpNow();
            section.Controls.Add(_ytDlpUpdateBtn);

            var openLogBtn = NewButton("Log-Ordner öffnen", 180);
            openLogBtn.Top = 36 + sy; openLogBtn.Left = 24 + 230 + 10;
            openLogBtn.Click += (_, _) => OpenLogFolder();
            section.Controls.Add(openLogBtn);
            sy += ButtonH + 8;

            section.Controls.Add(new Label
            {
                Text = "Jeder Lauf wird mitgeschrieben (eine Datei pro Tag, 14 Tage) — inklusive " +
                       "der kompletten yt-dlp-Ausgabe, die oben im Log nicht auftaucht.",
                Font = FontHint, ForeColor = HintColor,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 34,
                TextAlign = ContentAlignment.TopLeft, AutoSize = false
            });
            sy += 40;
            return sy;
        });

        // Provider chooser
        y = AddSection(content, y, innerW, "1. KI-Anbieter auswählen", section =>
        {
            int sy = 16;
            section.Controls.Add(new Label
            {
                Text = "Welcher Anbieter soll Skripte und Themen generieren?",
                Font = FontLabel, ForeColor = TextDark,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = LabelH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            _aiProviderBox = NewCombo("anthropic", "openai", "gemini");
            _aiProviderBox.Top = 36 + sy + LabelH + 3;
            _aiProviderBox.Left = 24;
            _aiProviderBox.Width = 300;
            _aiProviderBox.Height = InputH;
            _aiProviderBox.SelectedIndexChanged += (_, _) =>
            {
                _settings.AiProvider = _aiProviderBox.SelectedItem?.ToString() ?? "anthropic";
                _settings.Save();
            };
            section.Controls.Add(_aiProviderBox);
            sy += LabelH + 3 + InputH + 6;
            section.Controls.Add(new Label
            {
                Text = "→ Sie müssen nur den API-Key für den gewählten Anbieter ausfüllen. " +
                       "Die anderen können leer bleiben.",
                Font = FontHint, ForeColor = HintColor,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = HintH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            sy += HintH + 4 + FieldGap;
            return sy;
        });

        // Anthropic Claude
        y = AddSection(content, y, innerW, "1a. Anthropic Claude", section =>
        {
            int sy = 16;
            _anthropicKey = NewPassword();
            sy = AddSettingsRow(section, sy, "Anthropic API Key",
                "Bei https://console.anthropic.com → Settings → Billing zuerst Credits aufladen. Dann API Keys → Create Key. Beginnt mit \"sk-ant-…\"",
                _anthropicKey);
            _anthropicModel = NewEditableCombo("claude-sonnet-4-6", AnthropicModels);
            sy = AddSettingsRow(section, sy, "Modell",
                "Empfohlen: \"claude-sonnet-4-6\" (gut + günstig). Liste klappt auf — oder eigenes eintippen.",
                _anthropicModel);
            return sy;
        });

        // OpenAI ChatGPT
        y = AddSection(content, y, innerW, "1b. OpenAI ChatGPT", section =>
        {
            int sy = 16;
            _openAiKey = NewPassword();
            sy = AddSettingsRow(section, sy, "OpenAI API Key",
                "Bei https://platform.openai.com/api-keys einloggen → Create new secret key. Beginnt mit \"sk-…\"",
                _openAiKey);
            _openAiModel = NewEditableCombo("gpt-4o", OpenAIModels);
            sy = AddSettingsRow(section, sy, "Modell",
                "Empfohlen: \"gpt-4o\". \"gpt-4o-mini\" ist viel günstiger. Liste klappt auf.",
                _openAiModel);
            return sy;
        });

        // Google Gemini
        y = AddSection(content, y, innerW, "1c. Google Gemini", section =>
        {
            int sy = 16;
            _geminiKey = NewPassword();
            sy = AddSettingsRow(section, sy, "Gemini API Key",
                "Bei https://aistudio.google.com/apikey kostenlos generieren. Beginnt mit \"AIza…\"",
                _geminiKey);
            _geminiModel = NewEditableCombo("gemini-2.0-flash", GeminiModels);
            sy = AddSettingsRow(section, sy, "Modell",
                "\"gemini-2.0-flash\" ist kostenlos (Rate-Limit 60/min). Liste klappt auf.",
                _geminiModel);
            return sy;
        });

        y = AddSection(content, y, innerW, "2. Voice (ElevenLabs — die KI-Stimme)", section =>
        {
            int sy = 16;
            _elevenKey = NewPassword();
            sy = AddSettingsRow(section, sy, "ElevenLabs API Key",
                "Bei https://elevenlabs.io einloggen → oben rechts auf Profil → \"API Keys\" → Schlüssel kopieren.",
                _elevenKey);
            _elevenVoice = NewTextBox("21m00Tcm4TlvDq8ikWAM");
            sy = AddSettingsRow(section, sy, "Voice ID",
                "ID einer Stimme aus elevenlabs.io/voice-library. Vorgabe = englische Stimme \"Rachel\".",
                _elevenVoice);
            return sy;
        });

        y = AddSection(content, y, innerW, "3. B-Roll (Pexels — die Hintergrund-Videos)", section =>
        {
            int sy = 16;
            _pexelsKey = NewPassword();
            sy = AddSettingsRow(section, sy, "Pexels API Key",
                "Bei https://www.pexels.com/api/ kostenlos registrieren → \"Your API Key\" kopieren.",
                _pexelsKey);
            return sy;
        });

        y = AddSection(content, y, innerW, "4. YouTube Upload (optional)", section =>
        {
            int sy = 16;
            section.Controls.Add(new Label
            {
                Text = "Für den Upload brauchen Sie OAuth-Zugangsdaten von Google:\n" +
                       "  1. https://console.cloud.google.com öffnen → Projekt anlegen\n" +
                       "  2. \"APIs & Services\" → \"YouTube Data API v3\" aktivieren\n" +
                       "  3. \"Credentials\" → \"Create Credentials\" → \"OAuth client ID\" → Anwendungstyp \"Desktop\"",
                Font = FontHint, ForeColor = HintColor,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 78,
                TextAlign = ContentAlignment.TopLeft, AutoSize = false
            });
            sy += 88;
            _ytClientId = NewTextBox("");
            sy = AddSettingsRow(section, sy, "OAuth Client ID",
                "Sieht so aus: \"123456789-abc...apps.googleusercontent.com\"", _ytClientId);
            _ytClientSecret = NewPassword();
            sy = AddSettingsRow(section, sy, "OAuth Client Secret",
                "Sieht so aus: \"GOCSPX-...\" (geheim halten!)", _ytClientSecret);

            _ytConnectBtn = NewButton("Mit YouTube verbinden", 230);
            _ytConnectBtn.BackColor = AccentRed;
            _ytConnectBtn.ForeColor = Color.White;
            _ytConnectBtn.Font = FontBold;
            _ytConnectBtn.FlatAppearance.BorderSize = 0;
            _ytConnectBtn.Top = 36 + sy; _ytConnectBtn.Left = 24;
            _ytConnectBtn.Click += async (_, _) => await ConnectYouTube();
            section.Controls.Add(_ytConnectBtn);

            _ytDisconnectBtn = NewButton("Trennen", 120);
            _ytDisconnectBtn.Top = 36 + sy; _ytDisconnectBtn.Left = 24 + 230 + 10;
            _ytDisconnectBtn.Click += (_, _) => { _settings.YouTubeRefreshToken = null; _settings.Save(); UpdateYtStatus(); };
            section.Controls.Add(_ytDisconnectBtn);
            sy += ButtonH + 12;

            _ytStatus = new Label
            {
                Font = FontBold,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 22,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
            section.Controls.Add(_ytStatus);
            sy += 32;

            // Auto-category
            _autoCategoryCheck = new CheckBox
            {
                Text = "Kategorie automatisch erkennen (aus Titel/Thema)",
                Font = FontLabel, ForeColor = TextDark, Checked = true,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_autoCategoryCheck);
            sy += 30;

            // Auto-playlist removed — playlists are managed manually

            // Auto first comment
            _autoCommentCheck = new CheckBox
            {
                Text = "Automatisch ersten Kommentar nach Upload posten",
                Font = FontLabel, ForeColor = TextDark, Checked = true,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_autoCommentCheck);
            sy += 28;

            _autoCommentText = new TextBox
            {
                Font = FontBase, BorderStyle = BorderStyle.FixedSingle,
                Top = 36 + sy, Left = 40, Width = section.Width - 64, Height = InputH
            };
            section.Controls.Add(_autoCommentText);
            sy += InputH + 8;

            // Auto-optimize (24h CTR/views check)
            _autoOptimizeCheck = new CheckBox
            {
                Text = "Schwache Videos nach 24h erkennen und melden",
                Font = FontLabel, ForeColor = TextDark, Checked = true,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_autoOptimizeCheck);
            sy += 30;

            // Auto-reply to comments
            _autoReplyCheck = new CheckBox
            {
                Text = "Automatisch auf neue Kommentare antworten",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_autoReplyCheck);
            sy += 28;

            _autoReplyText = new TextBox
            {
                Font = FontBase, BorderStyle = BorderStyle.FixedSingle,
                Top = 36 + sy, Left = 40, Width = section.Width - 64, Height = InputH
            };
            section.Controls.Add(_autoReplyText);
            sy += InputH + 8;

            // ── Reddit Cross-Posting ────────────────────────────────────────
            _redditAutoPostCheck = new CheckBox
            {
                Text = "Nach Upload automatisch auf Reddit cross-posten",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_redditAutoPostCheck);
            sy += 28;

            sy += AddInlineLabel(section, sy, 40, "Reddit Client-ID:", _redditClientId = NewText());
            sy += AddInlineLabel(section, sy, 40, "Reddit Client-Secret:", _redditClientSecret = NewPassword());
            sy += AddInlineLabel(section, sy, 40, "Reddit Username:", _redditUsername = NewText());
            sy += AddInlineLabel(section, sy, 40, "Reddit Passwort (oder App-PW):", _redditPassword = NewPassword());
            sy += AddInlineLabel(section, sy, 40, "Subreddits (Komma-getrennt, ohne r/):", _redditSubreddits = NewText());

            // ── TikTok Auto-Export ──────────────────────────────────────────
            _tiktokAutoExportCheck = new CheckBox
            {
                Text = "Nach Upload TikTok-Version (1080x1920 vertikal) exportieren",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_tiktokAutoExportCheck);
            sy += 28;

            _tiktokAutoOpenCheck = new CheckBox
            {
                Text = "TikTok-Upload-Seite im Browser öffnen",
                Font = FontLabel, ForeColor = TextDark, Checked = true,
                Top = 36 + sy, Left = 40, Width = section.Width - 64, Height = 26, AutoSize = false
            };
            section.Controls.Add(_tiktokAutoOpenCheck);
            sy += 30;

            sy += AddInlineLabel(section, sy, 40, "TikTok-Export Ordner (leer = Ausgabeordner/tiktok):",
                _tiktokExportFolder = NewText());

            // ── Telegram Bot ────────────────────────────────────────────────
            _telegramAutoPostCheck = new CheckBox
            {
                Text = "Nach Upload Link in Telegram-Kanal posten",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_telegramAutoPostCheck);
            sy += 28;

            sy += AddInlineLabel(section, sy, 40, "Telegram Bot-Token (von @BotFather):", _telegramBotToken = NewPassword());
            sy += AddInlineLabel(section, sy, 40, "Telegram Chat-ID (z.B. -1001234567890):", _telegramChatId = NewText());
            sy += AddInlineLabel(section, sy, 40, "Nachrichten-Template ({title} und {url} werden ersetzt):",
                _telegramTemplate = NewText());

            // ── Discord (Wish-Channel + Invite-Link) ────────────────────────
            sy += AddInlineLabel(section, sy, 40, "Discord Bot-Token (für Wish-Channel):",
                _discordBotToken = NewPassword());
            sy += AddInlineLabel(section, sy, 40, "Discord Wish-Channel-ID (Rechtsklick Channel → Copy ID):",
                _discordWishChannelId = NewText());
            sy += AddInlineLabel(section, sy, 40, "Discord-Invite-Link (kommt unter jedes hochgeladene Video):",
                _discordInviteLink = NewText());

            _wishPollEnabledCheck = new CheckBox
            {
                Text = "Wünsche automatisch alle paar Stunden abholen",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 40, Width = section.Width - 64, Height = 26, AutoSize = false
            };
            section.Controls.Add(_wishPollEnabledCheck);
            sy += 30;

            section.Controls.Add(new Label
            {
                Text = "Intervall (Stunden):", Font = FontLabel, ForeColor = TextMuted,
                Top = 36 + sy, Left = 40, Width = 160, Height = InputH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            _wishPollIntervalBox = new NumericUpDown
            {
                Minimum = 1, Maximum = 168, Value = 2, Font = FontBase,
                Width = 70, Height = InputH, Top = 36 + sy, Left = 210
            };
            section.Controls.Add(_wishPollIntervalBox);
            sy += InputH + 10;

            // ── Mehrsprachige Titel ─────────────────────────────────────────
            _multilingualCheck = new CheckBox
            {
                Text = "Mehrsprachige Titel + Beschreibungen automatisch generieren (per AI)",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_multilingualCheck);
            sy += 30;

            sy += AddInlineLabel(section, sy, 40,
                "Zielsprachen (Komma, ISO-Codes: en, es, fr, tr, ru, pt, it, pl, ja, ko, ar):",
                _multilingualTargets = NewText());

            section.Controls.Add(new Label
            {
                Text = "→ Video bleibt deutsch. Ausländische Zuschauer sehen den Titel in ihrer Sprache " +
                       "und können Auto-Untertitel aktivieren.",
                Font = FontHint, ForeColor = HintColor,
                Top = 36 + sy, Left = 40, Width = section.Width - 64, Height = 32, AutoSize = false
            });
            sy += 36;

            // ── Discord-Webhook bei Upload ───────────────────────────────
            _discordWebhookCheck = new CheckBox
            {
                Text = "Discord-Webhook: nach Upload Embed im Channel posten",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_discordWebhookCheck);
            sy += 28;

            sy += AddInlineLabel(section, sy, 40,
                "Webhook-URL (Channel → Bearbeiten → Integrationen → Webhook):",
                _discordWebhookUrl = NewPassword());

            // ── AI-Antworten auf Kommentare ──────────────────────────────
            _aiReplyCheck = new CheckBox
            {
                Text = "AI-generierte, kontextbezogene Antworten auf Kommentare (statt Template)",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_aiReplyCheck);
            sy += 30;

            // ── Trending-Detector ────────────────────────────────────────
            _trendingEnabledCheck = new CheckBox
            {
                Text = "Trending-Detector: meldet trendige Themen passend zu deinen Keywords",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_trendingEnabledCheck);
            sy += 28;

            _trendingAutoQueueCheck = new CheckBox
            {
                Text = "Trending-Themen automatisch in Auto-Queue einreihen",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 40, Width = section.Width - 64, Height = 26, AutoSize = false
            };
            section.Controls.Add(_trendingAutoQueueCheck);
            sy += 28;

            section.Controls.Add(new Label
            {
                Text = "Intervall (Stunden):", Font = FontLabel, ForeColor = TextMuted,
                Top = 36 + sy, Left = 40, Width = 160, Height = InputH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            _trendingIntervalBox = new NumericUpDown
            {
                Minimum = 1, Maximum = 168, Value = 12, Font = FontBase,
                Width = 70, Height = InputH, Top = 36 + sy, Left = 210
            };
            section.Controls.Add(_trendingIntervalBox);
            sy += InputH + 10;

            // Schedule publish
            _scheduleUploadCheck = new CheckBox
            {
                Text = "Veröffentlichung planen (anstatt sofort öffentlich)",
                Font = FontLabel, ForeColor = TextDark, Checked = false,
                Top = 36 + sy, Left = 24, Width = section.Width - 48, Height = 26, AutoSize = false
            };
            section.Controls.Add(_scheduleUploadCheck);
            sy += 34;

            // Days + hour pickers (indented)
            section.Controls.Add(new Label
            {
                Text = "Tage ab heute:", Font = FontLabel, ForeColor = TextMuted,
                Top = 36 + sy, Left = 40, Width = 120, Height = InputH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            _scheduleDaysBox = new NumericUpDown
            {
                Minimum = 0, Maximum = 30, Value = 1, Font = FontBase,
                Width = 70, Height = InputH, Top = 36 + sy, Left = 165
            };
            section.Controls.Add(_scheduleDaysBox);

            section.Controls.Add(new Label
            {
                Text = "Uhrzeit (Stunde):", Font = FontLabel, ForeColor = TextMuted,
                Top = 36 + sy, Left = 260, Width = 140, Height = InputH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            _scheduleHourBox = new NumericUpDown
            {
                Minimum = 0, Maximum = 23, Value = 15, Font = FontBase,
                Width = 70, Height = InputH, Top = 36 + sy, Left = 405
            };
            section.Controls.Add(_scheduleHourBox);
            section.Controls.Add(new Label
            {
                Text = ":00 Uhr",
                Font = FontLabel, ForeColor = TextMuted,
                Top = 36 + sy, Left = 480, Width = 60, Height = InputH,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            sy += InputH + 10;

            // Enable/disable day/hour pickers based on checkbox
            void UpdateScheduleControls()
            {
                _scheduleDaysBox.Enabled = _scheduleUploadCheck.Checked;
                _scheduleHourBox.Enabled = _scheduleUploadCheck.Checked;
            }
            _scheduleUploadCheck.CheckedChanged += (_, _) => UpdateScheduleControls();
            UpdateScheduleControls();

            return sy;
        });

        y = AddSection(content, y, innerW, "5. Ausgabe", section =>
        {
            int sy = 16;
            _outputFolder = NewTextBox("");
            sy = AddSettingsRow(section, sy, "Ordner für erstellte Videos",
                "Vorgabe: \"…\\Videos\\CreatorDesktop\". Leer lassen für Standard.",
                _outputFolder);
            return sy;
        });

        _saveSettingsBtn = NewPrimaryButton("✓  Einstellungen speichern", 280);
        _saveSettingsBtn.Top = y; _saveSettingsBtn.Left = PadX;
        _saveSettingsBtn.Click += (_, _) => SaveSettingsFromForm();
        content.Controls.Add(_saveSettingsBtn);
        y += ButtonH + 24;
        content.Height = y;
    }

    // ═══ Layout helpers ═════════════════════════════════════════════════════

    private int AddFieldRow(Control parent, int y, int innerW, string labelText, Control field,
        bool fillWidth = true)
    {
        parent.Controls.Add(new Label
        {
            Text = labelText, Font = FontLabel, ForeColor = TextMuted,
            Top = y, Left = PadX, Width = innerW, Height = LabelH,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });

        field.Top = y + LabelH + 4;
        field.Left = PadX;
        if (fillWidth)
        {
            field.Width = innerW;
            field.Height = InputH;
        }
        parent.Controls.Add(field);
        return y + LabelH + 4 + InputH + FieldGap;
    }

    private int AddSection(Control parent, int y, int innerW, string title, Func<Panel, int> build)
    {
        var section = new Panel
        {
            Top = y, Left = PadX, Width = innerW,
            Height = 200, BackColor = CardColor
        };
        section.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, section.Width - 1, section.Height - 1);
        };
        parent.Controls.Add(section);

        section.Controls.Add(new Label
        {
            Text = title, Font = FontSection, ForeColor = TextDark,
            Top = 10, Left = 24, Width = section.Width - 48, Height = 26,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });

        int innerEnd = build(section);
        section.Height = 36 + innerEnd + 8;
        return y + section.Height + 18;
    }

    private int AddSettingsRow(Panel section, int y, string labelText, string hint, Control field)
    {
        int innerW = section.Width - 48;
        section.Controls.Add(new Label
        {
            Text = labelText, Font = FontLabel, ForeColor = TextDark,
            Top = 36 + y, Left = 24, Width = innerW, Height = LabelH,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        field.Top = 36 + y + LabelH + 3;
        field.Left = 24;
        field.Width = innerW;
        field.Height = InputH;
        section.Controls.Add(field);
        section.Controls.Add(new Label
        {
            Text = "→ " + hint, Font = FontHint, ForeColor = HintColor,
            Top = 36 + y + LabelH + 3 + InputH + 2,
            Left = 24, Width = innerW, Height = HintH,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        return y + LabelH + 3 + InputH + HintH + 4 + FieldGap;
    }

    private Panel NewCard(int top, int width, int height, out Label titleLabel, string title)
    {
        var bg = new Panel
        {
            Top = top, Left = PadX, Width = width, Height = height,
            BackColor = CardColor
        };
        bg.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, bg.Width - 1, bg.Height - 1);
        };
        titleLabel = new Label
        {
            Text = title, Font = FontBold, ForeColor = TextDark,
            Top = 12, Left = 16, Width = width - 32, Height = 22,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };
        bg.Controls.Add(titleLabel);
        return bg;
    }

    // ═══ Control factories ══════════════════════════════════════════════════

    private static TextBox NewTextBox(string placeholder)
        => new() { Font = FontBase, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = placeholder };

    private static TextBox NewText()
        => new() { Font = FontBase, BorderStyle = BorderStyle.FixedSingle };

    private static TextBox NewPassword()
        => new() { Font = FontBase, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };

    /// <summary>Adds "Label \n Input" row inside a Settings section. Returns Y-advance used.</summary>
    private int AddInlineLabel(Control parent, int sy, int left, string label, TextBox input)
    {
        parent.Controls.Add(new Label
        {
            Text = label, Font = FontLabel, ForeColor = TextDark,
            Top = 36 + sy, Left = left, Width = parent.Width - left - 24, Height = 22,
            TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        });
        input.Top = 36 + sy + 22;
        input.Left = left;
        input.Width = parent.Width - left - 24;
        input.Height = InputH;
        parent.Controls.Add(input);
        return 22 + InputH + 6;
    }

    private static ComboBox NewCombo(params object[] items)
    {
        var c = new ComboBox
        {
            Font = FontBase,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat
        };
        c.Items.AddRange(items);
        c.SelectedIndex = 0;
        return c;
    }

    /// <summary>
    /// Editable ComboBox — user can pick from the list OR type a custom value.
    /// Used for model selection so new models can be entered when providers release them.
    /// FlatStyle.Standard ensures the dropdown arrow is always visible on Windows 11.
    /// </summary>
    private static ComboBox NewEditableCombo(string defaultValue, params string[] options)
    {
        var c = new ComboBox
        {
            Font = FontBase,
            DropDownStyle = ComboBoxStyle.DropDown,
            FlatStyle = FlatStyle.Standard,
            IntegralHeight = false,
            DropDownHeight = 240,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };
        foreach (var item in options)
            c.Items.Add(item);
        c.Text = defaultValue;
        return c;
    }

    // ─── Curated model lists ────────────────────────────────────────────────
    // Editable — users can pick or type their own.

    private static readonly string[] AnthropicModels =
    {
        "claude-opus-4-8",
        "claude-opus-4-7",
        "claude-opus-4-6",
        "claude-sonnet-4-6",
        "claude-haiku-4-5"
    };

    private static readonly string[] OpenAIModels =
    {
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-4-turbo",
        "gpt-4",
        "gpt-3.5-turbo",
        "o1-mini",
        "o1-preview"
    };

    private static readonly string[] GeminiModels =
    {
        "gemini-2.0-flash",
        "gemini-2.5-flash-preview-05-20",
        "gemini-2.5-pro-preview-05-06",
        "gemini-1.5-pro-latest",
        "gemini-1.5-flash-latest",
        "gemini-1.5-pro",
        "gemini-1.5-flash"
    };

    private static readonly string[] DocumentaryTopics =
    [
        "Ägypten Pyramiden",
        "Ägyptische Götter",
        "Aliens",
        "Alligator",
        "Antike Kulturen",
        "Area 51",
        "Ausbrecher",
        "Ausgestorbene Tiere",
        "Bären",
        "Bermuda Dreieck",
        "Berühmte Schlachten",
        "Biohacking",
        "Chemieunfälle",
        "Delfine",
        "Diktatoren",
        "Dinosaurier",
        "Drogenkartelle",
        "Erdbeben",
        "Extremsport",
        "Fails",
        "Feuerwehr Einsätze",
        "Freeclimbing",
        "Gangster",
        "Gangs",
        "Gefahren im Weltall",
        "Gefährliche Tiere",
        "Gefängnisse",
        "Geister",
        "Geschichte",
        "Goldsucher",
        "Gorillas",
        "Große Brände",
        "Größte Explosionen",
        "Größte Räuber der Geschichte",
        "Hacker",
        "Haie",
        "Hiroshima Nagasaki",
        "Hitler",
        "Holocaust",
        "ISS Raumstation",
        "Jack the Ripper",
        "Jeffrey Dahmer",
        "Kampfjets",
        "Kampfsport",
        "Killer",
        "Klettern",
        "Kochen",
        "Kometen und Asteroiden",
        "Krokodile",
        "Künstliche Intelligenz",
        "Lost Places",
        "Löwen",
        "Lustige Videos",
        "Mafia",
        "Mars",
        "Militär Operationen",
        "Mörder",
        "Motorräder",
        "Mysterien",
        "Naturkatastrophen",
        "Ninjas",
        "Oktopus",
        "Orangen Utan",
        "Piraten",
        "Planeten",
        "Polizei Verfolgungsjagden",
        "Raumfahrt",
        "Reisen",
        "Ritter",
        "Roboter",
        "Römer",
        "Schatzsuche",
        "Schlangen",
        "Schwarze Löcher",
        "Serienmörder",
        "Stunts",
        "Supercars",
        "Surfen",
        "Ted Bundy",
        "Tiefsee Monster",
        "Tiere",
        "Tiger",
        "Titanic",
        "Tschernobyl",
        "Tsunamis",
        "U-Boote",
        "UFOs",
        "Ungelöste Morde",
        "Unterwasserwelt",
        "Verschwörungen",
        "Vulkane",
        "Wahre Verbrechen",
        "Wale",
        "Weltall",
        "Wikinger",
        "Wilde Tiere Afrika",
        "Wölfe",
        "Zauberei",
        "Zweiter Weltkrieg",
        "Züge",
    ];

    private static Button NewButton(string text, int width)
        => new()
        {
            Text = text, Width = width, Height = ButtonH, Font = FontBase,
            FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = Cursors.Hand,
            FlatAppearance = { BorderColor = Color.FromArgb(195, 205, 220), BorderSize = 1 }
        };

    private static Button NewPrimaryButton(string text, int width)
    {
        var b = NewButton(text, width);
        b.BackColor = PrimaryColor;
        b.ForeColor = Color.White;
        b.Font = FontBold;
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    // ═══ State <-> form ═════════════════════════════════════════════════════

    private void LoadSettingsToForm()
    {
        // Settings tab
        _aiProviderBox.SelectedItem = _settings.AiProvider;
        _anthropicKey.Text = _settings.AnthropicApiKey;
        _anthropicModel.Text = _settings.AnthropicModel;
        _openAiKey.Text = _settings.OpenAiApiKey;
        _openAiModel.Text = _settings.OpenAiModel;
        _geminiKey.Text = _settings.GeminiApiKey;
        _geminiModel.Text = _settings.GeminiModel;
        _elevenKey.Text = _settings.ElevenLabsApiKey;
        _elevenVoice.Text = _settings.ElevenLabsVoiceId;
        _pexelsKey.Text = _settings.PexelsApiKey;
        _ytClientId.Text = _settings.YouTubeClientId;
        _ytClientSecret.Text = _settings.YouTubeClientSecret;
        _autoCategoryCheck.Checked = _settings.AutoCategory;
        _autoCommentCheck.Checked = _settings.AutoPostComment;
        _autoCommentText.Text = _settings.AutoCommentTemplate;
        _autoOptimizeCheck.Checked = _settings.AutoOptimize;
        _autoReplyCheck.Checked = _settings.AutoReplyComments;
        _autoReplyText.Text = _settings.AutoReplyTemplate;
        _redditAutoPostCheck.Checked = _settings.AutoPostReddit;
        _redditClientId.Text = _settings.RedditClientId;
        _redditClientSecret.Text = _settings.RedditClientSecret;
        _redditUsername.Text = _settings.RedditUsername;
        _redditPassword.Text = _settings.RedditPassword;
        _redditSubreddits.Text = string.Join(", ", _settings.RedditSubreddits);
        _tiktokAutoExportCheck.Checked = _settings.TikTokAutoExport;
        _tiktokAutoOpenCheck.Checked = _settings.TikTokAutoOpen;
        _tiktokExportFolder.Text = _settings.TikTokExportFolder;
        _telegramAutoPostCheck.Checked = _settings.AutoPostTelegram;
        _telegramBotToken.Text = _settings.TelegramBotToken;
        _telegramChatId.Text = _settings.TelegramChatId;
        _telegramTemplate.Text = _settings.TelegramTemplate;
        _discordBotToken.Text = _settings.DiscordBotToken;
        _discordWishChannelId.Text = _settings.DiscordWishChannelId;
        _discordInviteLink.Text = _settings.DiscordInviteLink;
        _wishPollEnabledCheck.Checked = _settings.WishPollEnabled;
        _wishPollIntervalBox.Value = Math.Clamp(_settings.WishPollIntervalHours, 1, 168);
        _multilingualCheck.Checked = _settings.AutoMultilingual;
        _multilingualTargets.Text = string.Join(", ", _settings.MultilingualTargets);
        _discordWebhookCheck.Checked = _settings.AutoPostDiscordWebhook;
        _discordWebhookUrl.Text = _settings.DiscordWebhookUrl;
        _aiReplyCheck.Checked = _settings.AutoReplyUseAI;
        _trendingEnabledCheck.Checked = _settings.TrendingDetectorEnabled;
        _trendingAutoQueueCheck.Checked = _settings.TrendingAutoAddToQueue;
        _trendingIntervalBox.Value = Math.Clamp(_settings.TrendingCheckIntervalHours, 1, 168);
        _seasonalAutoQueueCheck.Checked = _settings.SeasonalAutoAddToQueue;
        _backupFolderBox.Text = _settings.BackupFolder;
        _autoBackupCheck.Checked = _settings.AutoBackupEnabled;
        _autoBackupIntervalBox.Value = Math.Clamp(_settings.AutoBackupIntervalHours, 1, 168);
        _autoBackupKeepBox.Value = Math.Clamp(_settings.AutoBackupKeepDays, 1, 365);
        _scheduleUploadCheck.Checked = _settings.ScheduleUpload;
        _scheduleDaysBox.Value = Math.Clamp(_settings.ScheduleDaysFromNow, 0, 30);
        _scheduleHourBox.Value = Math.Clamp(_settings.ScheduleHour, 0, 23);
        _scheduleDaysBox.Enabled = _settings.ScheduleUpload;
        _scheduleHourBox.Enabled = _settings.ScheduleUpload;
        _outputFolder.Text = _settings.OutputFolder;
        // Cookies browser dropdown
        var browserVal = string.IsNullOrWhiteSpace(_settings.YtDlpCookiesBrowser) ? "Keiner" : _settings.YtDlpCookiesBrowser;
        _cookiesBrowserBox.SelectedItem = browserVal;
        if (_cookiesBrowserBox.SelectedIndex < 0) _cookiesBrowserBox.SelectedIndex = 0;
        _ytDlpAutoUpdateCheck.Checked = _settings.YtDlpAutoUpdate;
        UpdateYtStatus();

        // Create tab
        _styleBox.SelectedItem = _settings.DefaultStyle;
        _langBox.SelectedItem = _settings.DefaultLanguage;
        // Settings store seconds for backwards-compat; UI shows minutes.
        var minutes = Math.Max(1, _settings.DefaultDurationSeconds / 60);
        _durationBox.Value = Math.Clamp(minutes, (int)_durationBox.Minimum, (int)_durationBox.Maximum);
        _useDurationCheck.Checked = _settings.UseDuration;
        _durationBox.Enabled = _settings.UseDuration;
        _autoUploadCheck.Checked = _settings.AutoUploadAfterRender;
        _ccOnlyCheck.Checked = _settings.CcOnlyMode;
        _ccMergeCheck.Checked = _settings.CcMergeAndReencode;
        _ccShortsCheck.Checked = _settings.CcShortsMode;
        _visibilityBox.SelectedItem = _settings.UploadVisibility;

        // Auto tab
        _channelTheme.Text = _settings.ChannelTheme;
        _queueBox.Text = string.Join(Environment.NewLine, _settings.QueuedTopics);
        _schedulerCheck.Checked = _settings.SchedulerEnabled;
        _intervalHours.Value = Math.Clamp(_settings.SchedulerIntervalHours, 1, 168);
        _competitorEnabledCheck.Checked = _settings.CompetitorTrackerEnabled;
        _competitorIntervalBox.Value = Math.Clamp(_settings.CompetitorCheckIntervalHours, 1, 168);
        _competitorEnabledCheck.CheckedChanged += (_, _) =>
            { _settings.CompetitorTrackerEnabled = _competitorEnabledCheck.Checked; _settings.Save(); };
        _competitorIntervalBox.ValueChanged += (_, _) =>
            { _settings.CompetitorCheckIntervalHours = (int)_competitorIntervalBox.Value; _settings.Save(); };
        UpdateQueueCount();
        UpdateNextRunLabel();
    }

    /// <summary>Persists the Create-tab choices (style/lang/duration/auto-upload) into settings.</summary>
    private void SaveCreateFormToSettings()
    {
        _settings.DefaultStyle = _styleBox.SelectedItem?.ToString() ?? "documentary";
        _settings.DefaultLanguage = _langBox.SelectedItem?.ToString() ?? "de";
        _settings.DefaultDurationSeconds = (int)_durationBox.Value * 60;
        _settings.UseDuration = _useDurationCheck.Checked;
        _settings.AutoUploadAfterRender = _autoUploadCheck.Checked;
        _settings.CcOnlyMode = _ccOnlyCheck.Checked;
        _settings.CcMergeAndReencode = _ccMergeCheck.Checked;
        _settings.CcShortsMode = _ccShortsCheck.Checked;
        _settings.UploadVisibility = _visibilityBox.SelectedItem?.ToString() ?? "private";
        _settings.Save();
    }

    private void SaveSettingsFromForm()
    {
        _settings.AiProvider = _aiProviderBox.SelectedItem?.ToString() ?? "anthropic";
        _settings.AnthropicApiKey = _anthropicKey.Text.Trim();
        _settings.AnthropicModel = string.IsNullOrWhiteSpace(_anthropicModel.Text) ? "claude-sonnet-4-6" : _anthropicModel.Text.Trim();
        _settings.OpenAiApiKey = _openAiKey.Text.Trim();
        _settings.OpenAiModel = string.IsNullOrWhiteSpace(_openAiModel.Text) ? "gpt-4o" : _openAiModel.Text.Trim();
        _settings.GeminiApiKey = _geminiKey.Text.Trim();
        _settings.GeminiModel = string.IsNullOrWhiteSpace(_geminiModel.Text) ? "gemini-2.0-flash" : _geminiModel.Text.Trim();
        _settings.ElevenLabsApiKey = _elevenKey.Text.Trim();
        _settings.ElevenLabsVoiceId = string.IsNullOrWhiteSpace(_elevenVoice.Text) ? "21m00Tcm4TlvDq8ikWAM" : _elevenVoice.Text.Trim();
        _settings.PexelsApiKey = _pexelsKey.Text.Trim();
        _settings.YouTubeClientId = _ytClientId.Text.Trim();
        _settings.YouTubeClientSecret = _ytClientSecret.Text.Trim();
        _settings.AutoCategory = _autoCategoryCheck.Checked;
        _settings.AutoPostComment = _autoCommentCheck.Checked;
        _settings.AutoCommentTemplate = _autoCommentText.Text.Trim();
        _settings.AutoOptimize = _autoOptimizeCheck.Checked;
        _settings.AutoReplyComments = _autoReplyCheck.Checked;
        _settings.AutoReplyTemplate = _autoReplyText.Text.Trim();
        _settings.AutoPostReddit = _redditAutoPostCheck.Checked;
        _settings.RedditClientId = _redditClientId.Text.Trim();
        _settings.RedditClientSecret = _redditClientSecret.Text.Trim();
        _settings.RedditUsername = _redditUsername.Text.Trim();
        _settings.RedditPassword = _redditPassword.Text;
        _settings.RedditSubreddits = _redditSubreddits.Text
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        _settings.TikTokAutoExport = _tiktokAutoExportCheck.Checked;
        _settings.TikTokAutoOpen = _tiktokAutoOpenCheck.Checked;
        _settings.TikTokExportFolder = _tiktokExportFolder.Text.Trim();
        _settings.AutoPostTelegram = _telegramAutoPostCheck.Checked;
        _settings.TelegramBotToken = _telegramBotToken.Text.Trim();
        _settings.TelegramChatId = _telegramChatId.Text.Trim();
        _settings.TelegramTemplate = _telegramTemplate.Text;
        _settings.DiscordBotToken = _discordBotToken.Text.Trim();
        _settings.DiscordWishChannelId = _discordWishChannelId.Text.Trim();
        _settings.DiscordInviteLink = _discordInviteLink.Text.Trim();
        _settings.WishPollEnabled = _wishPollEnabledCheck.Checked;
        _settings.WishPollIntervalHours = (int)_wishPollIntervalBox.Value;
        _settings.AutoMultilingual = _multilingualCheck.Checked;
        _settings.MultilingualTargets = _multilingualTargets.Text
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        _settings.AutoPostDiscordWebhook = _discordWebhookCheck.Checked;
        _settings.DiscordWebhookUrl = _discordWebhookUrl.Text.Trim();
        _settings.AutoReplyUseAI = _aiReplyCheck.Checked;
        _settings.TrendingDetectorEnabled = _trendingEnabledCheck.Checked;
        _settings.TrendingAutoAddToQueue = _trendingAutoQueueCheck.Checked;
        _settings.TrendingCheckIntervalHours = (int)_trendingIntervalBox.Value;
        _settings.ScheduleUpload = _scheduleUploadCheck.Checked;
        _settings.ScheduleDaysFromNow = (int)_scheduleDaysBox.Value;
        _settings.ScheduleHour = (int)_scheduleHourBox.Value;
        var selectedBrowser = _cookiesBrowserBox.SelectedItem?.ToString() ?? "Keiner";
        _settings.YtDlpCookiesBrowser = selectedBrowser == "Keiner" ? "" : selectedBrowser;
        _settings.YtDlpAutoUpdate = _ytDlpAutoUpdateCheck.Checked;
        if (!string.IsNullOrWhiteSpace(_outputFolder.Text))
        {
            _settings.OutputFolder = _outputFolder.Text.Trim();
            Directory.CreateDirectory(_settings.OutputFolder);
        }
        _settings.Save();
        MessageBox.Show(this, "Einstellungen gespeichert.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>Opens the log folder in Explorer, selecting today's file if it exists.</summary>
    private void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(RunLog.Dir);
            var today = RunLog.TodayPath;
            var psi = File.Exists(today)
                ? new ProcessStartInfo("explorer.exe", $"/select,\"{today}\"")
                : new ProcessStartInfo("explorer.exe", $"\"{RunLog.Dir}\"");
            psi.UseShellExecute = true;
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Log-Ordner: {RunLog.Dir}\r\n\r\n{ex.Message}",
                "Ordner konnte nicht geöffnet werden", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Forces a yt-dlp update from the settings tab. YouTube's player API changes every few
    /// weeks — an outdated yt-dlp.exe is the usual cause of "HTTP Error 403: Forbidden".
    /// </summary>
    private async Task UpdateYtDlpNow()
    {
        _ytDlpUpdateBtn.Enabled = false;
        var oldText = _ytDlpUpdateBtn.Text;
        _ytDlpUpdateBtn.Text = "Aktualisiere...";
        var log = (IProgress<string>)new Progress<string>(AppendAutoLog);
        try
        {
            await YtDlpDownloader.EnsureAsync(log, CancellationToken.None);
            var ok = await YtDlpDownloader.UpdateAsync(log, CancellationToken.None);
            MessageBox.Show(this,
                ok ? "yt-dlp ist jetzt aktuell. Details stehen im Auto-Log."
                   : "Update fehlgeschlagen — Details stehen im Auto-Log.",
                "yt-dlp", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            AppendAutoLog($"yt-dlp-Update fehlgeschlagen: {ex.Message}");
            MessageBox.Show(this, ex.Message, "yt-dlp", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _ytDlpUpdateBtn.Text = oldText;
            _ytDlpUpdateBtn.Enabled = true;
        }
    }

    private void UpdateYtStatus()
    {
        if (!string.IsNullOrEmpty(_settings.YouTubeRefreshToken))
        {
            _ytStatus.Text = "● Verbunden";
            _ytStatus.ForeColor = AccentGreen;
        }
        else
        {
            _ytStatus.Text = "○ Nicht verbunden";
            _ytStatus.ForeColor = TextMuted;
        }
    }

    private async Task ConnectYouTube()
    {
        SaveSettingsFromForm();
        if (string.IsNullOrWhiteSpace(_settings.YouTubeClientId) || string.IsNullOrWhiteSpace(_settings.YouTubeClientSecret))
        {
            MessageBox.Show(this, "Bitte zuerst Client ID und Secret eintragen.", "Fehlt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            _ytConnectBtn.Enabled = false;
            var yt = new YouTubeService(_settings);
            var log = new Progress<string>(s => _statusLabel.Text = s);
            await yt.EnsureAccessTokenAsync(log, CancellationToken.None);
            UpdateYtStatus();
            MessageBox.Show(this, "YouTube verbunden.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _ytConnectBtn.Enabled = true; }
    }

    private async Task RunSinglePipeline()
    {
        if (string.IsNullOrWhiteSpace(_topicBox.Text))
        {
            MessageBox.Show(this, "Bitte ein Topic eingeben.", "Fehlt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        SaveSettingsFromForm();
        SaveCreateFormToSettings();

        _logBox.Clear();
        _generateBtn.Enabled = false;
        _cancelBtn.Enabled = true;
        _openFolderBtn.Enabled = false;
        _uploadBtn.Enabled = false;
        _progressBar.Style = ProgressBarStyle.Marquee;
        _cts = new CancellationTokenSource();

        var progress = new Progress<string>(s =>
        {
            _statusLabel.Text = s;
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}\r\n");
        });

        try
        {
            var pipeline = new Pipeline(_settings);
            var targetSec = _useDurationCheck.Checked ? (int)_durationBox.Value * 60 : int.MaxValue / 2;
            _lastJob = await pipeline.RunAsync(
                _topicBox.Text.Trim(),
                _styleBox.SelectedItem?.ToString() ?? "documentary",
                _langBox.SelectedItem?.ToString() ?? "de",
                targetSec,
                progress, _cts.Token);

            _openFolderBtn.Enabled = true;
            _uploadBtn.Enabled = true;
            _statusLabel.Text = "Render fertig.";

            // Auto-Upload (Feature 1)
            if (_autoUploadCheck.Checked && !string.IsNullOrEmpty(_settings.YouTubeRefreshToken))
            {
                _statusLabel.Text = "Auto-Upload läuft...";
                var yt = new YouTubeService(_settings);
                var url = await yt.UploadVideoAsync(
                    _lastJob, _lastJob.OutputVideoPath!, _lastJob.ThumbnailPath,
                    _settings.UploadVisibility, progress, _cts.Token);
                _logBox.AppendText($"YouTube: {url}\r\n");
                _statusLabel.Text = "Fertig + hochgeladen.";
            }
            else
            {
                _statusLabel.Text = "Fertig.";
            }
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Abgebrochen.";
            _logBox.AppendText("[Abgebrochen]\r\n");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Fehler.";
            _logBox.AppendText($"[FEHLER] {ex.Message}\r\n");
            MessageBox.Show(this, ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _generateBtn.Enabled = true;
            _cancelBtn.Enabled = false;
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Value = 0;
        }
    }

    // ── CC-Videosuche & direkter Download ───────────────────────────────────

    private async Task SearchVideos()
    {
        var topic = _topicBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(topic))
        {
            MessageBox.Show(this, "Bitte zuerst ein Thema eingeben oder auswählen.", "Kein Thema",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _searchVideosBtn.Enabled = false;
        _searchStatusLabel.Text = "Suche läuft — bitte warten...";
        _videoListView.Items.Clear();
        _downloadCheckedBtn.Enabled = false;

        try
        {
            var lang = _langBox.SelectedItem?.ToString() ?? "de";
            var logProgress = new Progress<string>(s => _searchStatusLabel.Text = s);

            var isShorts = _settings.CcShortsMode;
            var max = (int)_maxResultsBox.Value;
            var videos = await YtDlpDownloader.SearchAsync(
                topic + (isShorts ? " shorts" : ""),
                max, ccOnly: true, logProgress, CancellationToken.None, language: lang);

            // Shorts-Modus: nur Videos bis 60 Sek.
            if (isShorts)
                videos = videos.Where(v => v.durationSeconds is > 0 and <= 60).ToList();

            var historyIds = new HashSet<string>(_settings.DownloadedVideoIds);

            _videoListView.BeginUpdate();
            int alreadyDl = 0;
            foreach (var (id, title, dur) in videos)
            {
                var item = new ListViewItem(title);
                item.SubItems.Add(FormatDuration(dur));
                item.SubItems.Add(id);
                if (historyIds.Contains(id))
                {
                    item.ForeColor = Color.FromArgb(180, 60, 60);   // red — already downloaded
                    item.SubItems[0].Text = "✓ " + title;
                    item.ToolTipText = "Bereits heruntergeladen";
                    alreadyDl++;
                }
                else
                {
                    item.ForeColor = Color.FromArgb(80, 80, 80);    // neutral grey — license not yet checked
                }
                _videoListView.Items.Add(item);
            }
            _videoListView.ShowItemToolTips = true;
            _videoListView.EndUpdate();

            var hint = alreadyDl > 0 ? $" ({alreadyDl} bereits heruntergeladen, rot markiert)" : "";
            _searchStatusLabel.Text = videos.Count > 0
                ? $"{videos.Count} Videos gefunden{hint}. Klicke 'Lizenzen prüfen' für Farbcodierung (grün/gelb/rot)."
                : $"Keine Videos gefunden. Anderes Thema versuchen.";
            _downloadCheckedBtn.Enabled = videos.Count > 0;
            _checkLicensesBtn.Enabled = videos.Count > 0;
        }
        catch (Exception ex)
        {
            _searchStatusLabel.Text = $"Fehler: {ex.Message}";
        }
        finally
        {
            _searchVideosBtn.Enabled = true;
        }
    }

    /// <summary>
    /// Checks every visible (non-history) video's license via yt-dlp and colours rows:
    /// green = Creative Commons (safe to re-upload), yellow = Standard YouTube licence
    /// (download OK, re-upload risky), red = blocked / unavailable.
    /// </summary>
    private async Task CheckLicensesAsync()
    {
        var checkedItems = _videoListView.CheckedItems.Cast<ListViewItem>().ToList();
        if (checkedItems.Count == 0)
        {
            MessageBox.Show(this, "Bitte erst Videos ankreuzen, die geprüft werden sollen.",
                "Keine Auswahl", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _checkLicensesBtn.Enabled = false;
        var ct = CancellationToken.None;
        var checker = new CompetitorTrackerService();
        var historyIds = new HashSet<string>(_settings.DownloadedVideoIds);

        int total = checkedItems.Count, done = 0;
        foreach (var item in checkedItems)
        {
            var id = item.SubItems.Count > 2 ? item.SubItems[2].Text : "";
            if (string.IsNullOrEmpty(id)) continue;
            if (historyIds.Contains(id)) { done++; continue; } // keep red for downloaded

            _searchStatusLabel.Text = $"Prüfe Lizenz {done + 1}/{total}: {item.Text}";
            try
            {
                var r = await checker.CheckVideoAsync(id, null, ct);
                Color col = r.Status switch
                {
                    CompetitorTrackerService.LicenseStatus.CreativeCommons => Color.FromArgb(40, 140, 60),    // green
                    CompetitorTrackerService.LicenseStatus.StandardYouTube => Color.FromArgb(200, 140, 0),   // yellow/orange
                    CompetitorTrackerService.LicenseStatus.RegionRestricted => Color.FromArgb(200, 140, 0),  // yellow
                    CompetitorTrackerService.LicenseStatus.Blocked => Color.FromArgb(180, 60, 60),           // red
                    _ => Color.FromArgb(80, 80, 80)
                };
                item.ForeColor = col;
                item.ToolTipText = $"{r.Status} — {r.Reason} ({r.License})";
            }
            catch (Exception ex) { item.ToolTipText = $"Prüfung fehlgeschlagen: {ex.Message}"; }
            done++;
        }
        _searchStatusLabel.Text = $"Lizenzen geprüft. Grün = CC (safe), Gelb = Standard, Rot = gesperrt/bereits geladen.";
        _checkLicensesBtn.Enabled = true;
    }

    private async Task DownloadCheckedVideos()
    {
        var checkedItems = _videoListView.CheckedItems.Cast<ListViewItem>().ToList();
        if (checkedItems.Count == 0)
        {
            MessageBox.Show(this, "Kein Video markiert.", "Kein Video",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _downloadCheckedBtn.Enabled = false;
        _generateBtn.Enabled = false;
        _cancelBtn.Enabled = true;
        _openFolderBtn.Enabled = false;
        _cts = new CancellationTokenSource();
        _progressBar.Style = ProgressBarStyle.Marquee;
        _logBox.Clear();

        var workDir = Path.Combine(_settings.OutputFolder, $"cc_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(workDir);
        _lastJob = new VideoJob { WorkDir = workDir };

        var log = (IProgress<string>)new Progress<string>(s =>
        {
            _statusLabel.Text = s;
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}\r\n");
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.ScrollToCaret();
        });

        // Download ffmpeg once before the loop so yt-dlp can merge audio+video
        // and thumbnail creation works for all videos.
        string? ffmpegExeForSession = null;
        try
        {
            log.Report("Lade ffmpeg (einmalig falls nötig)...");
            ffmpegExeForSession = await FFmpegDownloader.EnsureAsync(_settings, log, _cts.Token);
            log.Report("ffmpeg bereit.");
        }
        catch (Exception fex) { log.Report($"ffmpeg Download fehlgeschlagen: {fex.Message} — lade pre-muxed."); }

        int done = 0, failed = 0, skipped = 0;
        var history = new HashSet<string>(_settings.DownloadedVideoIds);
        try
        {
            for (int i = 0; i < checkedItems.Count; i++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var item = checkedItems[i];
                var id = item.SubItems.Count > 2 ? item.SubItems[2].Text : "";
                var title = item.Text.TrimStart('✓', ' ');
                log.Report($"[{i + 1}/{checkedItems.Count}] {title}");

                if (history.Contains(id))
                {
                    log.Report($"  Übersprungen — schon früher heruntergeladen ({id}).");
                    skipped++;
                    continue;
                }

                // Pause between downloads so YouTube doesn't rate-limit the IP
                if (i > 0)
                {
                    log.Report("  Pause 8 Sek. (YouTube Rate-Limit vermeiden)...");
                    await Task.Delay(8000, _cts.Token);
                }

                try
                {
                    var path = await YtDlpDownloader.DownloadVideoAsync(id, workDir, log, _cts.Token);
                    history.Add(id);
                    _settings.DownloadedVideoIds.Add(id);
                    _settings.Save();

                    // Fetch uploader info for attribution in description (best-effort, never blocks)
                    var info = await YtDlpDownloader.GetVideoInfoAsync(id, null, _cts.Token);
                    if (info != null)
                        log.Report($"  Kanal: {info.Uploader}");

                    var scene = new Scene
                    {
                        Order = i + 1, Title = title, SegmentPath = path, SourceVideoId = id,
                        SourceUploader = info?.Uploader ?? "",
                        SourceUploaderUrl = info?.UploaderUrl ?? "",
                        SourceLicense = info?.License ?? "Creative Commons Attribution (CC-BY)"
                    };

                    // Generate thumbnail from the downloaded video
                    var ffmpegExe = ffmpegExeForSession;
                    if (ffmpegExe != null && File.Exists(ffmpegExe))
                    {
                        var thumbPath = Path.Combine(workDir, $"thumb_{i + 1}.jpg");
                        try
                        {
                            log.Report("  Erstelle Thumbnail...");
                            var ff = new FFmpegService(ffmpegExe);
                            await ff.GenerateThumbnailAsync(path, title, thumbPath, _cts.Token);
                            scene.ThumbnailPath = thumbPath;
                            log.Report("  Thumbnail erstellt.");
                        }
                        catch (Exception tex)
                        {
                            log.Report($"  Thumbnail Fehler: {tex.Message}");
                        }
                    }
                    else
                    {
                        log.Report("  Kein Thumbnail — ffmpeg noch nicht heruntergeladen (Auto-Modus Tab → ffmpeg holen).");
                    }

                    _lastJob.Scenes.Add(scene);
                    done++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Report($"  Fehler: {ex.Message}");
                    failed++;
                }
            }
            var summary = $"{done} heruntergeladen";
            if (skipped > 0) summary += $", {skipped} übersprungen (schon geladen)";
            if (failed > 0) summary += $", {failed} Fehler";
            log.Report($"Fertig: {summary}.");
            log.Report($"Ausgabeordner: {workDir}");
            _statusLabel.Text = $"{done}/{checkedItems.Count} Videos heruntergeladen.";
            _openFolderBtn.Enabled = true;
            _uploadBtn.Enabled = done > 0;
        }
        catch (OperationCanceledException)
        {
            log.Report("Download abgebrochen.");
            _statusLabel.Text = "Abgebrochen.";
        }
        finally
        {
            _generateBtn.Enabled = true;
            _cancelBtn.Enabled = false;
            _downloadCheckedBtn.Enabled = true;
            _progressBar.Style = ProgressBarStyle.Blocks;
            _progressBar.Value = 0;
        }
    }

    private static string FormatDuration(int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0
            ? $"{ts.Hours}:{ts.Minutes:00}:{ts.Seconds:00}"
            : $"{ts.Minutes}:{ts.Seconds:00}";
    }

    private async Task UploadToYouTube()
    {
        if (_lastJob == null)
        {
            MessageBox.Show(this, "Kein Video zum Hochladen.", "Fehlt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Collect what we can upload:
        // Case A — single merged pipeline output
        // Case B — individually downloaded CC videos stored in Scenes
        var uploads = new List<(string path, string title, string? thumb, Scene? scene)>();

        if (_lastJob.OutputVideoPath != null && File.Exists(_lastJob.OutputVideoPath))
        {
            uploads.Add((_lastJob.OutputVideoPath, _lastJob.Title, _lastJob.ThumbnailPath, null));
        }
        else
        {
            foreach (var s in _lastJob.Scenes)
                if (s.SegmentPath != null && File.Exists(s.SegmentPath))
                    uploads.Add((s.SegmentPath, s.Title, s.ThumbnailPath, s));
        }

        if (uploads.Count == 0)
        {
            MessageBox.Show(this, "Keine Videodatei gefunden. Bitte zuerst ein Video herunterladen oder rendern.",
                "Fehlt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _uploadBtn.Enabled = false;
        try
        {
            var yt = new YouTubeService(_settings);
            var progress = (IProgress<string>)new Progress<string>(s =>
            {
                _statusLabel.Text = s;
                _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}\r\n");
                _logBox.SelectionStart = _logBox.TextLength;
                _logBox.ScrollToCaret();
            });

            // Ensure access token once for the whole batch (playlist + upload)
            var accessToken = await yt.EnsureAccessTokenAsync(progress, CancellationToken.None);

            // Compute optional scheduled publish time
            DateTime? publishAt = null;
            if (_settings.ScheduleUpload)
            {
                var target = DateTime.Now.Date
                    .AddDays(_settings.ScheduleDaysFromNow)
                    .AddHours(_settings.ScheduleHour);
                if (target <= DateTime.Now) target = target.AddDays(1);
                publishAt = target;
                progress.Report($"Veröffentlichung geplant: {target:dd.MM.yyyy} um {target:HH:mm} Uhr");
            }


            var uploaded = new List<string>();
            for (int i = 0; i < uploads.Count; i++)
            {
                var (path, title, thumb, scene) = uploads[i];

                // Shorts: append #Shorts so YouTube routes them to the Shorts feed
                var uploadTitle = _settings.CcShortsMode && !title.Contains("#Shorts", StringComparison.OrdinalIgnoreCase)
                    ? title + " #Shorts"
                    : title;
                progress.Report($"[{i + 1}/{uploads.Count}] Lade hoch: {uploadTitle}");

                // Auto-detect category from title + topic
                var categoryId = _settings.AutoCategory
                    ? YouTubeService.DetectCategory(_lastJob?.Topic ?? "", uploadTitle)
                    : "24";
                progress.Report($"  Kategorie: {CategoryName(categoryId)}");

                // Re-use pipeline-generated description when present; otherwise build one
                // with topic blurb, hashtags and (for CC clips) a Creative Commons attribution.
                var topic = _lastJob?.Topic ?? "";
                var description =
                    (_lastJob?.OutputVideoPath != null && !string.IsNullOrWhiteSpace(_lastJob.Description))
                        ? AppendDiscordLink(_lastJob.Description)
                        : BuildVideoDescription(uploadTitle, topic, scene);

                var stub = new VideoJob
                {
                    Title = uploadTitle,
                    Description = description,
                    Topic = topic,
                    Tags = BuildTags(topic, uploadTitle)
                };
                // Generate AI translations of title + short description for foreign viewers
                Dictionary<string, (string Title, string Description)>? localizations = null;
                if (_settings.AutoMultilingual && _settings.MultilingualTargets.Count > 0)
                {
                    try
                    {
                        progress.Report($"  Übersetze Titel in {_settings.MultilingualTargets.Count} Sprachen...");
                        localizations = await TranslateMultilingualAsync(uploadTitle, description,
                            _settings.MultilingualTargets, CancellationToken.None);
                    }
                    catch (Exception tex)
                    {
                        progress.Report($"  Übersetzung übersprungen: {tex.Message}");
                    }
                }

                var url = await yt.UploadVideoAsync(stub, path, thumb,
                    _settings.UploadVisibility, progress, CancellationToken.None,
                    publishAt, categoryId, localizations);
                _logBox.AppendText($"YouTube: {url}\r\n");
                uploaded.Add(url);

                // Track the uploaded video ID so we can auto-delete it if it gets blocked
                var uploadedVideoId = url.Contains("v=") ? url.Split("v=")[1] : "";
                if (!string.IsNullOrEmpty(uploadedVideoId))
                {
                    _settings.UploadedVideoIds.Add(uploadedVideoId);
                    _settings.Save();

                    // Add to every checked playlist from the Create-tab list
                    var checkedPlaylists = SelectedPlaylistIds();
                    foreach (var plId in checkedPlaylists)
                    {
                        try
                        {
                            await yt.AddToPlaylistAsync(plId, uploadedVideoId, accessToken, CancellationToken.None);
                            var plName = _playlists.FirstOrDefault(p => p.Id == plId)?.Name ?? plId;
                            progress.Report($"  ✓ In Playlist \"{plName}\" eingefügt.");
                        }
                        catch (Exception pex)
                        {
                            progress.Report($"  Playlist-Fehler ({plId}): {pex.Message}");
                        }
                    }

                    // Auto-post the first comment to seed engagement
                    if (_settings.AutoPostComment && !string.IsNullOrWhiteSpace(_settings.AutoCommentTemplate))
                    {
                        try
                        {
                            await yt.PostCommentAsync(uploadedVideoId, _settings.AutoCommentTemplate,
                                accessToken, CancellationToken.None);
                            progress.Report("  ✓ Erster Kommentar gepostet.");
                        }
                        catch (Exception cex)
                        {
                            progress.Report($"  Kommentar fehlgeschlagen: {cex.Message}");
                        }
                    }

                    // Reddit cross-posting
                    if (_settings.AutoPostReddit)
                    {
                        try
                        {
                            progress.Report("  Reddit: Cross-Post läuft...");
                            var reddit = new RedditService(_settings);
                            await reddit.CrossPostAsync(uploadTitle, url, progress, CancellationToken.None);
                        }
                        catch (Exception rex) { progress.Report($"  Reddit-Fehler: {rex.Message}"); }
                    }

                    // Telegram channel post
                    if (_settings.AutoPostTelegram)
                    {
                        try
                        {
                            var tg = new TelegramService(_settings);
                            await tg.SendMessageAsync(uploadTitle, url, progress, CancellationToken.None);
                        }
                        catch (Exception tex) { progress.Report($"  Telegram-Fehler: {tex.Message}"); }
                    }

                    // Discord webhook
                    if (_settings.AutoPostDiscordWebhook)
                    {
                        try
                        {
                            var hook = new DiscordWebhookService(_settings);
                            await hook.PostUploadAsync(uploadTitle, url, uploadedVideoId,
                                progress, CancellationToken.None);
                        }
                        catch (Exception dex) { progress.Report($"  Discord-Webhook-Fehler: {dex.Message}"); }
                    }
                }

                // TikTok export (needs the local file → before deletion)
                if (_settings.TikTokAutoExport)
                {
                    var tiktokFfmpeg = Path.Combine(AppSettings.DefaultDataDir, "ffmpeg", "ffmpeg.exe");
                    if (File.Exists(tiktokFfmpeg))
                    {
                        try
                        {
                            var folder = string.IsNullOrWhiteSpace(_settings.TikTokExportFolder)
                                ? Path.Combine(_settings.OutputFolder, "tiktok")
                                : _settings.TikTokExportFolder;
                            var tiktok = new TikTokExportService(tiktokFfmpeg);
                            await tiktok.ExportVerticalAsync(path, uploadTitle, description, folder,
                                progress, CancellationToken.None);
                            if (i == 0 && _settings.TikTokAutoOpen)
                                TikTokExportService.OpenUploadPage();
                        }
                        catch (Exception tex) { progress.Report($"  TikTok-Export Fehler: {tex.Message}"); }
                    }
                    else
                    {
                        progress.Report("  TikTok-Export uebersprungen (ffmpeg nicht gefunden).");
                    }
                }

                // Delete local files after successful upload to keep the folder clean
                try { File.Delete(path); } catch { }
                if (thumb != null) try { File.Delete(thumb); } catch { }
                progress.Report($"  Lokale Datei gelöscht: {Path.GetFileName(path)}");
            }

            var msg = uploads.Count == 1
                ? $"Hochgeladen:\n{uploaded[0]}\n\nIm Browser öffnen?"
                : $"{uploaded.Count} Videos hochgeladen.\n\nErstes im Browser öffnen?";
            if (MessageBox.Show(this, msg, "Erfolg", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo { FileName = uploaded[0], UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Upload-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _uploadBtn.Enabled = true; }
    }

    /// <summary>
    /// Builds a YouTube description: short topic-aware blurb, hashtags, plus a
    /// Creative Commons source attribution when the clip came from another channel.
    /// YouTube's CC-BY license requires us to credit the original creator.
    /// </summary>
    /// <summary>
    /// Uses the configured AI provider to translate the German title + first paragraph
    /// of the description into every requested target language code. Returns a dict
    /// keyed by language code that YouTube uses in the "localizations" upload field.
    /// </summary>
    private async Task<Dictionary<string, (string Title, string Description)>> TranslateMultilingualAsync(
        string germanTitle, string germanDescription, List<string> targetCodes, CancellationToken ct)
    {
        var ai = AIServiceFactory.Create(_settings);
        var result = new Dictionary<string, (string, string)>();

        // Keep the description we translate small — only the first paragraph is enough
        var shortDesc = germanDescription.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)[0];
        if (shortDesc.Length > 500) shortDesc = shortDesc[..500];

        foreach (var code in targetCodes)
        {
            var langName = code switch
            {
                "en" => "English", "es" => "Spanish", "fr" => "French",
                "tr" => "Turkish", "ru" => "Russian", "pt" => "Portuguese",
                "it" => "Italian", "pl" => "Polish", "nl" => "Dutch",
                "ja" => "Japanese", "ko" => "Korean", "zh" => "Chinese",
                "ar" => "Arabic", _ => code
            };

            var prompt =
                $"Translate the following German YouTube title and description into {langName}. " +
                "Keep it punchy and click-worthy — match YouTube tone, do NOT translate it literally. " +
                "Add \"[German Audio]\" at the end of the title so foreign viewers know the video is in German. " +
                "Reply with ONLY two lines, no labels, no commentary:\n" +
                "Line 1: translated title\n" +
                "Line 2: translated description\n\n" +
                $"Title: {germanTitle}\n" +
                $"Description: {shortDesc}";

            try
            {
                var reply = await ai.GenerateTextAsync(prompt, 400, ct);
                var lines = reply.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 2)
                {
                    var t = lines[0].TrimStart('-', '*', ' ');
                    var d = string.Join(" ", lines.Skip(1)).Trim();
                    if (t.Length > 100) t = t[..100];
                    result[code] = (t, d);
                }
            }
            catch { /* skip this language on failure */ }
        }
        return result;
    }

    /// <summary>
    /// Asks the configured AI provider for a short, friendly, context-aware German
    /// reply to a viewer comment. Falls back to the template on any failure.
    /// </summary>
    private async Task<string> GenerateAiReplyAsync(string author, string commentText, CancellationToken ct)
    {
        var ai = AIServiceFactory.Create(_settings);
        var prompt =
            "Du bist freundlicher deutscher YouTube-Kanal-Betreiber. Antworte auf den folgenden " +
            "Zuschauer-Kommentar kurz (max 200 Zeichen), auf Deutsch, freundlich, themenbezogen. " +
            "Verwende KEINE Anführungszeichen, KEINE Anrede 'Hallo XYZ', schreib einfach drauf los. " +
            "Falls der Kommentar eine Frage enthält, beantworte sie wenn möglich.\n\n" +
            $"Kommentar von {author}: {commentText}\n\n" +
            "Antwort:";
        var reply = await ai.GenerateTextAsync(prompt, 200, ct);
        reply = reply.Trim().Trim('"');
        if (reply.Length > 500) reply = reply[..500];
        return string.IsNullOrWhiteSpace(reply) ? _settings.AutoReplyTemplate : reply;
    }

    private string AppendDiscordLink(string description)
    {
        if (string.IsNullOrWhiteSpace(_settings.DiscordInviteLink)) return description;
        return description.TrimEnd() +
               "\n\n💬 Tritt unserem Discord bei und stimme über neue Themen ab:\n" +
               _settings.DiscordInviteLink;
    }

    private string BuildVideoDescription(string title, string topic, Scene? scene)
    {
        var sb = new System.Text.StringBuilder();

        // Auto-chapters at the very top — YouTube only detects them if they start
        // at line 1 with 00:00.
        if (_lastJob != null && _lastJob.Scenes.Count >= 2)
        {
            var chapters = ChapterBuilderService.Build(_lastJob);
            if (!string.IsNullOrWhiteSpace(chapters))
            {
                sb.AppendLine(chapters);
                sb.AppendLine();
            }
        }

        sb.AppendLine(title);
        sb.AppendLine();
        sb.AppendLine(TopicBlurb(topic));
        sb.AppendLine();

        var tags = BuildTags(topic, title);
        if (tags.Count > 0)
        {
            sb.AppendLine(string.Join(" ", tags.Take(8).Select(t => "#" + t.Replace(" ", ""))));
            sb.AppendLine();
        }

        if (scene != null && !string.IsNullOrWhiteSpace(scene.SourceVideoId))
        {
            sb.AppendLine("— Quelle / Source —");
            if (!string.IsNullOrWhiteSpace(scene.SourceUploader))
                sb.AppendLine($"Original-Kanal: {scene.SourceUploader}");
            if (!string.IsNullOrWhiteSpace(scene.SourceUploaderUrl))
                sb.AppendLine($"Kanal-Link: {scene.SourceUploaderUrl}");
            sb.AppendLine($"Originalvideo: https://www.youtube.com/watch?v={scene.SourceVideoId}");
            var lic = string.IsNullOrWhiteSpace(scene.SourceLicense)
                ? "Creative Commons Attribution (CC-BY)"
                : scene.SourceLicense;
            sb.AppendLine($"Lizenz: {lic}");
            sb.AppendLine("Verwendung gemäß CC-BY-Lizenz mit Namensnennung des Urhebers.");
            sb.AppendLine("Alle Rechte am Originalmaterial verbleiben beim ursprünglichen Ersteller.");
        }

        // Discord-Link unter jedes Video
        if (!string.IsNullOrWhiteSpace(_settings.DiscordInviteLink))
        {
            sb.AppendLine();
            sb.AppendLine("💬 Tritt unserem Discord bei und stimme über neue Themen ab:");
            sb.AppendLine(_settings.DiscordInviteLink);
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Short German blurb keyed off the topic — falls back to a generic line.</summary>
    private static string TopicBlurb(string topic)
    {
        var t = topic.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(t))
            return "Spannendes Videomaterial — schau rein und lass ein Abo da!";
        if (t.Contains("weltall") || t.Contains("planet") || t.Contains("mars") || t.Contains("raum"))
            return "Eindrucksvolle Aufnahmen aus dem Weltall — Planeten, Sterne und die Weiten des Universums.";
        if (t.Contains("tier") || t.Contains("hai") || t.Contains("löwe") || t.Contains("wolf") || t.Contains("wal"))
            return "Beeindruckende Tieraufnahmen aus aller Welt — Natur pur in atemberaubenden Bildern.";
        if (t.Contains("krieg") || t.Contains("hitler") || t.Contains("ww2") || t.Contains("weltkrieg"))
            return "Historisches Material — Ereignisse, die die Welt für immer verändert haben.";
        if (t.Contains("mörder") || t.Contains("killer") || t.Contains("verbrechen") || t.Contains("mafia"))
            return "True-Crime-Material — wahre Geschichten von Verbrechen, die niemand vergessen sollte.";
        if (t.Contains("ufo") || t.Contains("alien") || t.Contains("verschwörung") || t.Contains("mysteri"))
            return "Mysteriöse Aufnahmen und ungeklärte Phänomene — was steckt wirklich dahinter?";
        if (t.Contains("katastroph") || t.Contains("vulkan") || t.Contains("tsunami") || t.Contains("erdbeben"))
            return "Naturgewalten in voller Wucht — Katastrophen, die ganze Regionen verändert haben.";
        if (t.Contains("sport") || t.Contains("klettern") || t.Contains("surf") || t.Contains("extrem"))
            return "Extreme Action — Sportler an der Grenze des Machbaren.";
        if (t.Contains("auto") || t.Contains("car") || t.Contains("supercar") || t.Contains("motorrad"))
            return "Schnelle Maschinen, spektakuläre Aufnahmen — für Fans von PS und Adrenalin.";
        return $"Spannendes Material zum Thema {topic} — hier siehst du die besten Aufnahmen dazu.";
    }

    private static List<string> BuildTags(string topic, string title)
    {
        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(topic)) tags.Add(topic);

        // Extract candidate words from title
        foreach (var word in title.Split([' ', ',', '-', '|', ':', '#'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (word.Length < 4) continue;
            if (tags.Contains(word, StringComparer.OrdinalIgnoreCase)) continue;
            tags.Add(word);
            if (tags.Count >= 12) break;
        }

        // Topic-specific keyword expansion — boosts discoverability
        var t = (topic + " " + title).ToLowerInvariant();
        var extra = new List<string>();
        if (t.Contains("weltall") || t.Contains("planet") || t.Contains("mars") || t.Contains("raum"))
            extra.AddRange(["Universum", "Astronomie", "NASA", "Galaxie", "Sterne"]);
        if (t.Contains("tier") || t.Contains("hai") || t.Contains("löwe") || t.Contains("wolf"))
            extra.AddRange(["Natur", "Wildlife", "Tierwelt", "Tierdoku", "Wilde Tiere"]);
        if (t.Contains("krieg") || t.Contains("hitler") || t.Contains("weltkrieg") || t.Contains("nazi"))
            extra.AddRange(["Geschichte", "WW2", "Historisch", "Weltkrieg", "Militaer"]);
        if (t.Contains("mörder") || t.Contains("killer") || t.Contains("verbrechen") || t.Contains("mafia"))
            extra.AddRange(["True Crime", "Verbrechen", "Krimi", "Wahre Geschichten", "Polizei"]);
        if (t.Contains("ufo") || t.Contains("alien") || t.Contains("verschwörung") || t.Contains("mysteri"))
            extra.AddRange(["Mystery", "Unerklaerlich", "Paranormal", "Geheimnis", "Phaenomen"]);
        if (t.Contains("katastroph") || t.Contains("vulkan") || t.Contains("tsunami"))
            extra.AddRange(["Naturkatastrophe", "Erdbeben", "Sturm", "Klima", "Umwelt"]);

        foreach (var e in extra)
            if (!tags.Contains(e, StringComparer.OrdinalIgnoreCase)) tags.Add(e);

        // Universal Doku/Recommendations tags
        tags.AddRange(["Dokumentation", "Doku", "Deutsch", "HD", "Erklärt", "Fakten", "Wissen"]);

        // YouTube cap: tags total ≤ 500 chars
        var result = new List<string>();
        int totalChars = 0;
        foreach (var tg in tags.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            totalChars += tg.Length + 2;
            if (totalChars > 480) break;
            result.Add(tg);
        }
        return result;
    }

    private void ReloadPlaylistsUi()
    {
        _playlists = AppSettings.LoadPlaylists();
        _playlistList.Items.Clear();
        foreach (var p in _playlists)
            _playlistList.Items.Add($"{p.Name}  ({p.Id})", false);
        if (_playlists.Count == 0)
            _playlistList.Items.Add("(Noch keine Playlists — auf '+ Playlist hinzufügen' klicken)");
    }

    private void AddPlaylistViaDialog()
    {
        using var dlg = new Form
        {
            Text = "Playlist hinzufügen",
            Width = 460, Height = 220,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false, MaximizeBox = false, Font = FontBase
        };
        var lblName = new Label { Text = "Name (z.B. Filme):", Top = 16, Left = 20, Width = 400, Height = 22 };
        var tbName = new TextBox { Top = 40, Left = 20, Width = 400, Font = FontBase };
        var lblId = new Label { Text = "Playlist-ID (aus URL nach 'list=', beginnt mit PL...):", Top = 78, Left = 20, Width = 400, Height = 22 };
        var tbId = new TextBox { Top = 102, Left = 20, Width = 400, Font = FontBase };
        var ok = new Button { Text = "Hinzufügen", Top = 140, Left = 240, Width = 90, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Abbrechen", Top = 140, Left = 335, Width = 85, DialogResult = DialogResult.Cancel };
        dlg.Controls.AddRange([lblName, tbName, lblId, tbId, ok, cancel]);
        dlg.AcceptButton = ok; dlg.CancelButton = cancel;

        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var name = tbName.Text.Trim();
        var id = tbId.Text.Trim();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
        {
            MessageBox.Show(this, "Name und ID sind Pflicht.", "Fehlt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _playlists.Add(new PlaylistEntry { Name = name, Id = id });
        AppSettings.SavePlaylists(_playlists);
        ReloadPlaylistsUi();
    }

    private void RemoveSelectedPlaylists()
    {
        var toRemove = new List<PlaylistEntry>();
        for (int i = 0; i < _playlistList.Items.Count; i++)
            if (_playlistList.GetItemChecked(i) && i < _playlists.Count) toRemove.Add(_playlists[i]);
        if (toRemove.Count == 0) return;
        if (MessageBox.Show(this, $"{toRemove.Count} Playlist(s) aus der Liste entfernen?",
                "Entfernen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        foreach (var p in toRemove) _playlists.Remove(p);
        AppSettings.SavePlaylists(_playlists);
        ReloadPlaylistsUi();
    }

    private void OpenPlaylistsFile()
    {
        if (!File.Exists(AppSettings.PlaylistsPath))
            AppSettings.SavePlaylists(_playlists);
        Process.Start(new ProcessStartInfo { FileName = AppSettings.PlaylistsPath, UseShellExecute = true });
        MessageBox.Show(this,
            "Datei gespeichert? Dann auf OK klicken — die Playlisten werden neu geladen.",
            "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
        ReloadPlaylistsUi();
    }

    private List<string> SelectedPlaylistIds()
    {
        var ids = new List<string>();
        for (int i = 0; i < _playlistList.Items.Count; i++)
            if (_playlistList.GetItemChecked(i) && i < _playlists.Count) ids.Add(_playlists[i].Id);
        return ids;
    }

    private void ReloadCompetitorsUi()
    {
        _competitors = AppSettings.LoadCompetitors();
        _competitorList.Items.Clear();
        foreach (var c in _competitors) _competitorList.Items.Add($"{c.Name}");
        if (_competitors.Count == 0) _competitorList.Items.Add("(noch keine — '+ Kanal' klicken)");
    }

    private async Task AddCompetitorViaDialog()
    {
        using var dlg = new Form
        {
            Text = "Konkurrenz-Kanal hinzufügen",
            Width = 520, Height = 240, StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false, MaximizeBox = false, Font = FontBase
        };
        var lblHint = new Label
        {
            Text = "URL oder Kanal-ID einfügen (z.B. https://youtube.com/@kurzgesagt oder UCsXVk37bltHxD1rDPwtNM8Q):",
            Top = 16, Left = 20, Width = 470, Height = 40
        };
        var tbUrl = new TextBox { Top = 64, Left = 20, Width = 470, Font = FontBase };
        var ok = new Button { Text = "Hinzufügen", Top = 110, Left = 290, Width = 95, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Abbrechen", Top = 110, Left = 395, Width = 95, DialogResult = DialogResult.Cancel };
        dlg.Controls.AddRange([lblHint, tbUrl, ok, cancel]);
        dlg.AcceptButton = ok; dlg.CancelButton = cancel;

        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var input = tbUrl.Text.Trim();
        if (string.IsNullOrEmpty(input)) return;

        try
        {
            Cursor = Cursors.WaitCursor;
            var tracker = new CompetitorTrackerService();
            var id = await tracker.ResolveChannelIdAsync(input, CancellationToken.None);
            if (id == null)
            {
                MessageBox.Show(this, "Kanal-ID konnte nicht ermittelt werden. Pruefe die URL.",
                    "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Get channel name by pulling one RSS request
            var vids = await tracker.FetchChannelAsync(id, CancellationToken.None);
            var name = vids.Count > 0 ? vids[0].ChannelName : id;

            _competitors.Add(new CompetitorChannel { Name = name, ChannelId = id });
            AppSettings.SaveCompetitors(_competitors);
            ReloadCompetitorsUi();
            AppendAutoLog($"Konkurrenz hinzugefügt: {name} ({id})");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { Cursor = Cursors.Default; }
    }

    private void RemoveSelectedCompetitor()
    {
        var idx = _competitorList.SelectedIndex;
        if (idx < 0 || idx >= _competitors.Count) return;
        var ch = _competitors[idx];
        if (MessageBox.Show(this, $"{ch.Name} entfernen?", "Entfernen",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _competitors.RemoveAt(idx);
        AppSettings.SaveCompetitors(_competitors);
        ReloadCompetitorsUi();
    }

    /// <summary>Pulls every channel's RSS feed and shows the newest videos.</summary>
    private async Task RunCompetitorCheck(bool showPopup)
    {
        if (_competitors.Count == 0)
        {
            if (showPopup)
                MessageBox.Show(this, "Erst Konkurrenz-Kanäle hinzufügen.", "Keine Kanäle",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var log = (IProgress<string>)new Progress<string>(s => AppendAutoLog(s));
            log.Report("Konkurrenz-Check läuft...");

            var tracker = new CompetitorTrackerService();
            var all = await tracker.FetchAllAsync(_competitors, log, CancellationToken.None);

            // Filter to last 14 days
            var cutoff = DateTime.UtcNow.AddDays(-14);
            var recent = all.Where(v => v.Published.ToUniversalTime() >= cutoff).ToList();

            // Detect new uploads (since last check)
            var newOnes = new List<CompetitorTrackerService.CompetitorVideo>();
            foreach (var v in recent)
            {
                if (!_settings.CompetitorSeenVideos.TryGetValue(v.ChannelId, out var seen))
                    seen = _settings.CompetitorSeenVideos[v.ChannelId] = new List<string>();
                if (!seen.Contains(v.VideoId))
                {
                    newOnes.Add(v);
                    seen.Add(v.VideoId);
                    if (seen.Count > 50) seen.RemoveRange(0, seen.Count - 50);
                }
            }
            _settings.LastCompetitorCheckAt = DateTime.Now;
            _settings.Save();

            // Populate ListView
            _competitorFeed.BeginUpdate();
            _competitorFeed.Items.Clear();
            foreach (var v in recent)
            {
                var it = new ListViewItem(v.ChannelName);
                it.SubItems.Add(v.Title);
                it.SubItems.Add(v.Published.ToLocalTime().ToString("dd.MM HH:mm"));
                it.SubItems.Add("—");           // Lizenz-Spalte (gefüllt nach "Lizenzen prüfen")
                it.Tag = v.VideoId;

                // Tooltip: Titel + Kanal + Beschreibung (gekürzt)
                var desc = v.Description.Length > 400 ? v.Description[..400] + "…" : v.Description;
                it.ToolTipText =
                    $"{v.Title}\n" +
                    $"Kanal: {v.ChannelName}\n" +
                    $"Hochgeladen: {v.Published.ToLocalTime():dd.MM.yyyy HH:mm}\n" +
                    $"Video-ID: {v.VideoId}\n" +
                    (string.IsNullOrWhiteSpace(desc) ? "" : $"\n{desc}");

                if (newOnes.Any(n => n.VideoId == v.VideoId))
                    it.Font = new Font(_competitorFeed.Font, FontStyle.Bold); // neu = fett
                _competitorFeed.Items.Add(it);
            }
            _competitorFeed.EndUpdate();

            log.Report($"Konkurrenz-Check fertig: {recent.Count} Videos (14 Tage), {newOnes.Count} neu seit letztem Check.");

            if (newOnes.Count > 0)
            {
                _trayIcon?.ShowBalloonTip(7000,
                    "Neue Konkurrenz-Videos",
                    $"{newOnes.Count} neue Video(s) gefunden:\n" +
                    string.Join("\n", newOnes.Take(5).Select(n => $"• {n.ChannelName}: {n.Title}")),
                    ToolTipIcon.Info);
            }
            if (showPopup)
                MessageBox.Show(this, $"{recent.Count} Videos geladen, {newOnes.Count} neu seit letztem Check.",
                    "Fertig", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppendAutoLog($"Konkurrenz-Check Fehler: {ex.Message}");
            if (showPopup) MessageBox.Show(this, ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// For every video in the competitor feed, fetch its license + region restrictions
    /// via yt-dlp (free, no API quota) and color-code the row:
    /// green = CC + worldwide, yellow = Standard/DE-only, red = blocked.
    /// </summary>
    private async Task CheckCompetitorLicensesAsync()
    {
        if (_competitorFeed.Items.Count == 0)
        {
            MessageBox.Show(this, "Erst Konkurrenz-Check ausführen, damit Videos in der Liste stehen.",
                "Leere Liste", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var tracker = new CompetitorTrackerService();
        var log = (IProgress<string>)new Progress<string>(s => AppendAutoLog(s));
        log.Report($"Prüfe {_competitorFeed.Items.Count} Videos auf Lizenz + Sperrungen...");

        var items = _competitorFeed.Items.Cast<ListViewItem>().ToList();
        int counter = 0;
        foreach (var it in items)
        {
            counter++;
            var vid = it.Tag as string;
            if (string.IsNullOrEmpty(vid)) continue;

            // UI feedback inkremental
            it.SubItems[3].Text = "prüfe...";
            it.BackColor = Color.FromArgb(240, 240, 240);
            Application.DoEvents();

            try
            {
                var result = await tracker.CheckVideoAsync(vid, null, CancellationToken.None);
                string statusLine;
                switch (result.Status)
                {
                    case CompetitorTrackerService.LicenseStatus.CreativeCommons:
                        it.BackColor = Color.FromArgb(210, 245, 210); // grün
                        it.SubItems[3].Text = "✓ CC";
                        statusLine = "🟢 Creative Commons — Download + Re-Upload erlaubt (mit Quellenangabe)";
                        break;
                    case CompetitorTrackerService.LicenseStatus.StandardYouTube:
                        it.BackColor = Color.FromArgb(255, 245, 200); // gelb
                        it.SubItems[3].Text = "© Standard";
                        statusLine = "🟡 Standard YouTube-Lizenz — Download OK, Re-Upload urheberrechtlich riskant";
                        break;
                    case CompetitorTrackerService.LicenseStatus.RegionRestricted:
                        it.BackColor = Color.FromArgb(255, 245, 200); // gelb
                        it.SubItems[3].Text = "DE ✓ andere ✗";
                        statusLine = $"🟡 In DE erlaubt, anderswo gesperrt ({result.Reason}) — Download möglich";
                        break;
                    case CompetitorTrackerService.LicenseStatus.Blocked:
                        it.BackColor = Color.FromArgb(255, 215, 215); // rot
                        it.SubItems[3].Text = $"✗ {result.Reason}";
                        statusLine = $"🔴 Gesperrt: {result.Reason} — Download nicht möglich";
                        break;
                    default:
                        it.BackColor = Color.White;
                        it.SubItems[3].Text = "?";
                        statusLine = "❓ Lizenz unbekannt";
                        break;
                }
                // Tooltip um Lizenz-Info ergänzen
                it.ToolTipText = statusLine + "\n\n" + it.ToolTipText;
                if (counter % 5 == 0) log.Report($"  {counter}/{items.Count} geprüft...");
            }
            catch (Exception ex)
            {
                it.BackColor = Color.White;
                it.SubItems[3].Text = "Fehler";
                log.Report($"  {vid}: {ex.Message}");
            }
            Application.DoEvents();
        }
        log.Report("Lizenz-Check fertig. Grün = CC (download empfohlen), Gelb = nur DE/Standard, Rot = gesperrt.");
    }

    /// <summary>
    /// Downloads every CHECKED row whose license is CC or Standard or DE-allowed.
    /// Only rows confirmed as BLOCKED (red, status starts with "✗") are skipped —
    /// rest including yellow Standard-YouTube and DE-only get downloaded.
    /// </summary>
    private async Task DownloadCheckedCompetitorVideosAsync()
    {
        var checkedRows = _competitorFeed.CheckedItems.Cast<ListViewItem>().ToList();
        if (checkedRows.Count == 0)
        {
            MessageBox.Show(this, "Kein Video in der Liste markiert.",
                "Nichts markiert", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Allow: green CC + yellow Standard + yellow DE-only. Skip only red ("✗").
        var ok = checkedRows.Where(r => !r.SubItems[3].Text.StartsWith("✗")).ToList();
        var skipped = checkedRows.Count - ok.Count;
        if (ok.Count == 0)
        {
            MessageBox.Show(this,
                "Alle markierten Videos sind gesperrt (rot). Nicht herunterladbar.",
                "Nur gesperrte Videos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Warn if user is downloading yellow (Standard YT) videos so they understand the copyright risk
        var standardCount = ok.Count(r => r.SubItems[3].Text == "© Standard");
        if (standardCount > 0)
        {
            var result = MessageBox.Show(this,
                $"{standardCount} Video(s) haben Standard YouTube-Lizenz (gelb).\n\n" +
                "Download ist OK, aber das **Hochladen** auf deinen eigenen Kanal verletzt das Urheberrecht.\n\n" +
                "Trotzdem herunterladen?",
                "Achtung Standard-Lizenz", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;
        }

        _cts = new CancellationTokenSource();
        var workDir = Path.Combine(_settings.OutputFolder, $"competitor_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(workDir);
        _lastJob = new VideoJob { WorkDir = workDir, Topic = "Konkurrenz" };

        var log = (IProgress<string>)new Progress<string>(s => AppendAutoLog(s));
        log.Report($"Lade {ok.Count} Videos herunter ({skipped} übersprungen weil gesperrt)...");

        int done = 0, failed = 0;
        foreach (var row in ok)
        {
            var vid = row.Tag as string;
            if (string.IsNullOrEmpty(vid)) continue;
            var title = row.SubItems[1].Text;
            try
            {
                if (done > 0)
                    await Task.Delay(8000, _cts.Token);
                log.Report($"  Lade: {title}");
                var path = await YtDlpDownloader.DownloadVideoAsync(vid, workDir, log, _cts.Token);
                _settings.DownloadedVideoIds.Add(vid);
                _settings.Save();
                _lastJob.Scenes.Add(new Scene
                {
                    Order = done + 1, Title = title,
                    SegmentPath = path, SourceVideoId = vid
                });
                done++;
            }
            catch (Exception ex)
            {
                log.Report($"  Fehler: {ex.Message}");
                failed++;
            }
        }

        log.Report($"Fertig: {done} heruntergeladen, {failed} Fehler, {skipped} gesperrt.");
        _uploadBtn.Enabled = done > 0;
        if (done > 0)
            MessageBox.Show(this,
                $"{done} Videos heruntergeladen.\n\n" +
                $"Ordner: {workDir}\n\n" +
                "Im Create-Tab auf 'Upload' klicken um sie auf deinen Kanal zu posten\n" +
                "(nur bei CC-Videos empfohlen!).",
                "Fertig", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string CategoryName(string id) => id switch
    {
        "10" => "Musik",
        "15" => "Tiere & Haustiere",
        "17" => "Sport",
        "19" => "Reisen & Events",
        "20" => "Gaming",
        "22" => "Menschen & Blogs",
        "23" => "Comedy",
        "24" => "Entertainment",
        "25" => "Nachrichten & Politik",
        "26" => "Ratgeber & Style",
        "27" => "Bildung",
        "28" => "Wissenschaft & Technik",
        _ => $"ID {id}"
    };
}
