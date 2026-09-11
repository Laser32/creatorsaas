using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Pops topics from the AppSettings queue and runs them through the full pipeline,
/// optionally uploading to YouTube. Used both by the manual "process queue" button
/// and by the scheduler.
/// </summary>
public class QueueProcessor
{
    private readonly AppSettings _settings;
    private int _running;

    public QueueProcessor(AppSettings settings) => _settings = settings;

    public bool IsRunning => Interlocked.CompareExchange(ref _running, 0, 0) == 1;

    /// <summary>Process exactly one topic from the head of the queue.</summary>
    public async Task<bool> ProcessOneAsync(IProgress<string> log, CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) == 1)
        {
            log.Report("Warteschlange läuft bereits.");
            return false;
        }

        try
        {
            if (_settings.QueuedTopics.Count == 0)
            {
                log.Report("Warteschlange ist leer.");
                return false;
            }

            var topic = _settings.QueuedTopics[0];
            log.Report($"▶ Bearbeite: {topic}");

            var pipeline = new Pipeline(_settings);
            var job = await pipeline.RunAsync(
                topic,
                _settings.DefaultStyle,
                _settings.DefaultLanguage,
                _settings.DefaultDurationSeconds,
                log,
                ct);

            if (_settings.AutoUploadAfterRender
                && !string.IsNullOrEmpty(_settings.YouTubeRefreshToken)
                && !string.IsNullOrEmpty(job.OutputVideoPath))
            {
                log.Report("Auto-Upload aktiviert → lade hoch...");
                var yt = new YouTubeService(_settings);
                var url = await yt.UploadVideoAsync(
                    job, job.OutputVideoPath, job.ThumbnailPath,
                    _settings.UploadVisibility, log, ct);
                log.Report($"✓ Auf YouTube: {url}");

                await yt.AutoAddToTopicPlaylistAsync(url, topic, null, log, ct);
            }

            // Remove processed topic from queue
            _settings.QueuedTopics.RemoveAt(0);
            _settings.LastAutoRunAt = DateTime.UtcNow;
            _settings.Save();
            log.Report($"✓ Erledigt. {_settings.QueuedTopics.Count} Themen verbleiben in der Warteschlange.");
            return true;
        }
        catch (Exception ex)
        {
            log.Report($"✗ Fehler: {ex.Message}");
            return false;
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    /// <summary>Process every topic in the queue, one after the other.</summary>
    public async Task ProcessAllAsync(IProgress<string> log, CancellationToken ct)
    {
        while (_settings.QueuedTopics.Count > 0 && !ct.IsCancellationRequested)
        {
            var ok = await ProcessOneAsync(log, ct);
            if (!ok) break;
        }
        log.Report("Warteschlange abgearbeitet.");
    }
}
