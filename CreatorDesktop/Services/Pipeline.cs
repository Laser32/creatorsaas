using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

public class Pipeline
{
    private readonly AppSettings _settings;

    /// <summary>
    /// Videos with target length >= this many seconds use the documentary
    /// pipeline (YouTube research + outline + per-chapter narration + 1 B-roll per chapter).
    /// </summary>
    public const int DocumentaryThresholdSeconds = 600;

    public Pipeline(AppSettings settings) => _settings = settings;

    public Task<VideoJob> RunAsync(string topic, string style, string language, int targetSeconds,
        IProgress<string> log, CancellationToken ct)
    {
        if (_settings.CcOnlyMode)
            return RunCcOnlyAsync(topic, language, targetSeconds, log, ct);
        if (targetSeconds >= DocumentaryThresholdSeconds)
            return RunDocumentaryAsync(topic, language, targetSeconds, log, ct);
        return RunShortAsync(topic, style, language, targetSeconds, log, ct);
    }

    /// <summary>
    /// CC-Only pipeline: searches Creative Commons YouTube videos for the topic,
    /// lists them, and downloads each via yt-dlp.
    ///
    /// Default (CcMergeAndReencode = false): keeps every downloaded file as-is so
    /// the user can upload each video individually to YouTube.
    ///
    /// Optional (CcMergeAndReencode = true): re-encodes each clip to 1080p and
    /// concatenates everything into one output file.
    /// </summary>
    private async Task<VideoJob> RunCcOnlyAsync(string topic, string language, int targetSeconds,
        IProgress<string> log, CancellationToken ct)
    {
        bool doMerge = _settings.CcMergeAndReencode;
        int totalSteps = doMerge ? 3 : 2;

        log.Report("CC-Modus aktiv — keine AI, kein ElevenLabs, kein Pexels.");
        log.Report($"Schritt 1/{totalSteps} — Suche Creative-Commons-Videos zu '{topic}'");

        // Try the YouTube Data API first (requires OAuth); fall back to yt-dlp's
        // built-in search which works without any API key or login.
        var researcher = new YouTubeResearchService(_settings);
        var allVideos = await researcher.ListCCVideosAsync(topic, maxResults: 25, ct);
        if (allVideos.Count == 0)
        {
            log.Report("  YouTube-API leer/nicht verbunden — nutze yt-dlp-Suche.");
            allVideos = await YtDlpDownloader.SearchAsync(topic, maxResults: 25, ccOnly: true, log, ct);
        }
        if (allVideos.Count == 0)
        {
            log.Report("  Keine CC-Videos — wiederhole Suche ohne CC-Filter.");
            allVideos = await YtDlpDownloader.SearchAsync(topic, maxResults: 25, ccOnly: false, log, ct);
        }
        if (allVideos.Count == 0)
            throw new InvalidOperationException(
                "Keine Videos gefunden. Anderes Thema versuchen oder Internet pruefen.");

        log.Report($"  {allVideos.Count} CC-Videos gefunden:");
        foreach (var v in allVideos)
            log.Report($"   - [{FormatDur(v.durationSeconds)}] {v.title}  ({v.id})");

        // Select enough videos to cover the target duration
        var selected = new List<(string id, string title, int durationSeconds)>();
        int total = 0;
        foreach (var v in allVideos)
        {
            if (v.durationSeconds <= 0) continue;
            selected.Add(v);
            total += v.durationSeconds;
            if (total >= targetSeconds) break;
        }
        if (selected.Count == 0) selected.AddRange(allVideos);

        log.Report($"  → {selected.Count} Videos ausgewaehlt ({FormatDur(total)} Gesamtdauer).");

        var workDir = Path.Combine(_settings.OutputFolder, $"job_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(workDir);

        var job = new VideoJob
        {
            Topic = topic,
            Style = "cc-only",
            Language = language,
            TargetDurationSeconds = targetSeconds,
            Title = $"{topic} — CC Compilation",
            Description = $"Zusammenstellung aus {selected.Count} Creative-Commons-YouTube-Videos zum Thema '{topic}'.",
            WorkDir = workDir
        };

        log.Report($"Schritt 2/{totalSteps} — Lade {selected.Count} Videos herunter");
        for (int i = 0; i < selected.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var v = selected[i];
            log.Report($"  [{i + 1}/{selected.Count}] {v.title}");

            try
            {
                var rawPath = await YtDlpDownloader.DownloadVideoAsync(v.id, workDir, log, ct);
                log.Report($"    → gespeichert: {Path.GetFileName(rawPath)}");

                job.Scenes.Add(new Scene
                {
                    Order = i + 1,
                    Title = v.title,
                    DurationSeconds = v.durationSeconds,
                    SegmentPath = rawPath
                });
            }
            catch (Exception ex)
            {
                log.Report($"  Video {v.id} uebersprungen: {ex.Message}");
            }
        }

        if (job.Scenes.Count == 0)
            throw new InvalidOperationException("Kein einziges Video konnte heruntergeladen werden.");

        // ── Download-only mode (default) ────────────────────────────────────
        if (!doMerge)
        {
            log.Report($"FERTIG: {job.Scenes.Count} Video(s) heruntergeladen.");
            log.Report($"Ausgabeordner: {workDir}");
            log.Report("Tipp: 'Ordner oeffnen' zeigt die Dateien — direkt auf YouTube hochladbar.");
            return job;
        }

        // ── Merge + re-encode ────────────────────────────────────────────────
        var ffmpegPath = await FFmpegDownloader.EnsureAsync(_settings, log, ct);
        var ff = new FFmpegService(ffmpegPath);

        log.Report($"Schritt 3/{totalSteps} — Re-encodiere auf 1080p und fuege zusammen");
        for (int i = 0; i < job.Scenes.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var scene = job.Scenes[i];
            var segPath = Path.Combine(workDir, $"cc_{i + 1:00}.mp4");
            try
            {
                await ff.ReencodeToStandardAsync(scene.SegmentPath!, segPath, log, ct);
                TryDelete(scene.SegmentPath);
                scene.SegmentPath = segPath;
            }
            catch (Exception ex)
            {
                log.Report($"  Re-encode uebersprungen ({scene.Title}): {ex.Message}");
            }
        }

        var finalPath = Path.Combine(workDir, $"{Sanitize(job.Title)}.mp4");
        await ff.ConcatenateAsync(job.Scenes, finalPath, log, ct);
        job.OutputVideoPath = finalPath;

        var thumbPath = Path.Combine(workDir, "thumbnail.jpg");
        try
        {
            await ff.GenerateThumbnailAsync(finalPath, job.Title, thumbPath, ct);
            job.ThumbnailPath = thumbPath;
        }
        catch (Exception ex) { log.Report($"Thumbnail uebersprungen: {ex.Message}"); }

        foreach (var s in job.Scenes)
            if (s.SegmentPath != finalPath) TryDelete(s.SegmentPath);

        log.Report($"FERTIG: {finalPath}");
        return job;
    }

    private static string FormatDur(int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0 ? $"{ts.Hours}:{ts.Minutes:00}:{ts.Seconds:00}" : $"{ts.Minutes}:{ts.Seconds:00}";
    }

    private async Task<VideoJob> RunShortAsync(string topic, string style, string language, int targetSeconds,
        IProgress<string> log, CancellationToken ct)
    {
        var ffmpegPath = await FFmpegDownloader.EnsureAsync(_settings, log, ct);

        log.Report($"Schritt 1/5 — Skript generieren ({_settings.AiProvider})");
        var ai = AIServiceFactory.Create(_settings);
        var job = await ai.GenerateScriptAsync(topic, style, language, targetSeconds, log, ct);

        var workDir = Path.Combine(_settings.OutputFolder, $"job_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(workDir);
        job.WorkDir = workDir;

        log.Report($"Titel: {job.Title}");
        log.Report($"{job.Scenes.Count} Szenen werden erzeugt.");

        log.Report("Schritt 2/5 — Voice (ElevenLabs)");
        var voice = new ElevenLabsService(_settings);
        foreach (var scene in job.Scenes)
        {
            ct.ThrowIfCancellationRequested();
            var audioPath = Path.Combine(workDir, $"audio_{scene.Order}.mp3");
            await voice.SynthesizeAsync(scene, audioPath, log, ct);
        }

        log.Report("Schritt 3/5 — B-Roll (Pexels)");
        var pexels = new PexelsService(_settings);
        var ff = new FFmpegService(ffmpegPath);
        foreach (var scene in job.Scenes)
        {
            ct.ThrowIfCancellationRequested();
            var brollPath = Path.Combine(workDir, $"broll_{scene.Order}.mp4");
            try
            {
                await pexels.DownloadBRollAsync(scene, brollPath, log, ct);
            }
            catch (Exception ex)
            {
                log.Report($"  Pexels Fehler ({ex.Message}) — erzeuge Platzhalter-Video.");
                await ff.GeneratePlaceholderAsync(scene, brollPath, log, ct);
            }
        }

        log.Report("Schritt 4/5 — Video rendern");
        foreach (var scene in job.Scenes)
        {
            ct.ThrowIfCancellationRequested();
            var segPath = Path.Combine(workDir, $"scene_{scene.Order}.mp4");
            await ff.RenderSceneAsync(scene, segPath, log, ct);
        }

        var finalPath = Path.Combine(workDir, $"{Sanitize(job.Title)}.mp4");
        await ff.ConcatenateAsync(job.Scenes, finalPath, log, ct);
        job.OutputVideoPath = finalPath;

        log.Report("Schritt 5/5 — Thumbnail");
        var thumbPath = Path.Combine(workDir, "thumbnail.jpg");
        try
        {
            await ff.GenerateThumbnailAsync(finalPath, job.Title, thumbPath, ct);
            job.ThumbnailPath = thumbPath;
        }
        catch (Exception ex)
        {
            log.Report($"Thumbnail uebersprungen: {ex.Message}");
        }

        foreach (var scene in job.Scenes)
        {
            TryDelete(scene.AudioPath);
            TryDelete(scene.BRollPath);
            TryDelete(scene.SegmentPath);
        }

        log.Report($"FERTIG: {finalPath}");
        return job;
    }

    /// <summary>
    /// Long-form documentary pipeline.
    ///
    /// For each chapter the pipeline tries, in order:
    ///   1. Creative Commons YouTube video → download with yt-dlp → re-encode to 1080p.
    ///      Original audio is kept; ElevenLabs and Pexels are skipped for that chapter.
    ///   2. AI narration (ElevenLabs / Windows TTS) + Pexels B-roll → FFmpeg render.
    /// </summary>
    private async Task<VideoJob> RunDocumentaryAsync(string topic, string language, int targetSeconds,
        IProgress<string> log, CancellationToken ct)
    {
        var ffmpegPath = await FFmpegDownloader.EnsureAsync(_settings, log, ct);
        var ff = new FFmpegService(ffmpegPath);

        // ── Step 1: Research ────────────────────────────────────────────────
        log.Report("Schritt 1/6 — YouTube-Recherche (Creative Commons Videos)");
        var researcher = new YouTubeResearchService(_settings);
        var research = "";
        try
        {
            research = await researcher.ResearchAsync(topic, language, maxVideos: 6, log, ct);
        }
        catch (Exception ex)
        {
            log.Report($"Recherche uebersprungen: {ex.Message}");
        }

        // ── Step 2: Outline ─────────────────────────────────────────────────
        log.Report($"Schritt 2/6 — Gliederung ({_settings.AiProvider})");
        var ai = AIServiceFactory.Create(_settings);
        var outlineText = await ai.GenerateTextAsync(
            AIPrompts.DocumentaryOutlinePrompt(topic, language, targetSeconds, research),
            maxTokens: 4096, ct);
        var outlineJson = AIPrompts.ExtractJson(outlineText);
        var job = AIPrompts.ParseOutline(outlineJson, topic, language, targetSeconds);

        var workDir = Path.Combine(_settings.OutputFolder, $"job_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(workDir);
        job.WorkDir = workDir;

        log.Report($"Titel: {job.Title}");
        log.Report($"{job.Scenes.Count} Kapitel werden erzeugt.");

        var totalChapters = job.Scenes.Count;
        var perChapterSeconds = totalChapters > 0 ? targetSeconds / totalChapters : 300;
        var wordsPerChapter = Math.Max(120, perChapterSeconds * 150 / 60);

        // ── Step 3: Try CC video download per chapter ───────────────────────
        log.Report("Schritt 3/6 — CC-YouTube-Videos suchen & herunterladen");
        string? ytDlpPath = null;
        try { ytDlpPath = await YtDlpDownloader.EnsureAsync(log, ct); }
        catch (Exception ex) { log.Report($"  yt-dlp nicht verfuegbar: {ex.Message} — Fallback auf ElevenLabs."); }

        for (int i = 0; i < job.Scenes.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var ch = job.Scenes[i];
            if (ytDlpPath == null) break;  // yt-dlp not available; all chapters use TTS

            log.Report($"  Kapitel {i + 1}/{totalChapters}: suche CC-Video '{ch.BRollQuery}'...");
            var videoId = await researcher.FindCCVideoAsync(ch.BRollQuery, ct);
            if (videoId == null)
            {
                log.Report($"  Kein CC-Video gefunden — Kapitel {i + 1} bekommt ElevenLabs-Narration.");
                continue;
            }

            log.Report($"  CC-Video {videoId} gefunden — lade herunter...");
            try
            {
                var rawPath = await YtDlpDownloader.DownloadVideoAsync(videoId, workDir, log, ct);
                var segPath = Path.Combine(workDir, $"chapter_{ch.Order}.mp4");
                await ff.ReencodeToStandardAsync(rawPath, segPath, log, ct);
                TryDelete(rawPath);
                ch.SegmentPath = segPath;  // Mark as done — skip ElevenLabs + Pexels for this chapter
                log.Report($"  ✓ Kapitel {i + 1} aus CC-YouTube-Video fertig.");
            }
            catch (Exception ex)
            {
                log.Report($"  Download/Reencode fehlgeschlagen ({ex.Message}) — Fallback auf ElevenLabs.");
            }
        }

        // ── Step 4: AI narration for chapters that have no CC video ─────────
        var chaptersNeedingTts = job.Scenes.Where(s => s.SegmentPath == null).ToList();
        if (chaptersNeedingTts.Count > 0)
        {
            log.Report($"Schritt 4/6 — Skripte fuer {chaptersNeedingTts.Count} Kapitel ohne CC-Video");
            for (int i = 0; i < job.Scenes.Count; i++)
            {
                var ch = job.Scenes[i];
                if (ch.SegmentPath != null) continue;  // already have CC video
                ct.ThrowIfCancellationRequested();
                log.Report($"  Kapitel {i + 1}/{totalChapters}: {ch.Title}");
                var prompt = AIPrompts.ChapterNarrationPrompt(topic, language, ch.Title, ch.Narration,
                    i + 1, totalChapters, wordsPerChapter, research);
                var narration = await ai.GenerateTextAsync(prompt, Math.Min(8192, wordsPerChapter * 6), ct);
                ch.Narration = narration.Trim();
                ch.DurationSeconds = perChapterSeconds;
            }

            // ── Step 5: Voice synthesis (ElevenLabs or Windows TTS) ─────────
            log.Report("Schritt 5/6 — Voice-Synthese");
            var voice = new ElevenLabsService(_settings);
            var pexels = new PexelsService(_settings);
            foreach (var scene in job.Scenes)
            {
                if (scene.SegmentPath != null) continue;
                ct.ThrowIfCancellationRequested();

                var audioPath = Path.Combine(workDir, $"audio_{scene.Order}.mp3");
                await voice.SynthesizeAsync(scene, audioPath, log, ct);

                var brollPath = Path.Combine(workDir, $"broll_{scene.Order}.mp4");
                try
                {
                    await pexels.DownloadBRollAsync(scene, brollPath, log, ct);
                }
                catch (Exception ex)
                {
                    log.Report($"  Pexels Fehler ({ex.Message}) — erzeuge Platzhalter-Video.");
                    await ff.GeneratePlaceholderAsync(scene, brollPath, log, ct);
                }

                var segPath = Path.Combine(workDir, $"chapter_{scene.Order}.mp4");
                await ff.RenderSceneAsync(scene, segPath, log, ct);
            }
        }
        else
        {
            log.Report("Schritt 4-5/6 — uebersprungen (alle Kapitel aus CC-YouTube-Videos)");
        }

        // ── Step 6: Concat ───────────────────────────────────────────────────
        log.Report("Schritt 6/6 — Zusammenschnitt");
        var finalPath = Path.Combine(workDir, $"{Sanitize(job.Title)}.mp4");
        await ff.ConcatenateAsync(job.Scenes, finalPath, log, ct);
        job.OutputVideoPath = finalPath;

        var thumbPath = Path.Combine(workDir, "thumbnail.jpg");
        try
        {
            await ff.GenerateThumbnailAsync(finalPath, job.Title, thumbPath, ct);
            job.ThumbnailPath = thumbPath;
        }
        catch (Exception ex) { log.Report($"Thumbnail uebersprungen: {ex.Message}"); }

        foreach (var scene in job.Scenes)
        {
            TryDelete(scene.AudioPath);
            TryDelete(scene.BRollPath);
            if (scene.SegmentPath != finalPath)
                TryDelete(scene.SegmentPath);
        }

        log.Report($"FERTIG: {finalPath}");
        return job;
    }

    private static void TryDelete(string? p)
    {
        if (string.IsNullOrEmpty(p)) return;
        try { File.Delete(p); } catch { }
    }

    private static string Sanitize(string s)
    {
        var bad = Path.GetInvalidFileNameChars();
        var clean = new string(s.Select(c => bad.Contains(c) ? '_' : c).ToArray());
        return clean.Length > 60 ? clean.Substring(0, 60) : clean;
    }
}
