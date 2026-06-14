using System.Text.Json;
using CreatorSaaS.Core.Enums;
using CreatorSaaS.Core.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace CreatorSaaS.Workers.Analytics;

public interface IAnalyticsService
{
    Task FetchYouTubeAnalyticsAsync(CancellationToken ct);
    Task FetchVideoAnalyticsAsync(Guid videoJobId, CancellationToken ct);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _uow;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(IUnitOfWork uow, HttpClient httpClient, ILogger<AnalyticsService> logger)
    {
        _uow = uow;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task FetchYouTubeAnalyticsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting batch YouTube analytics fetch");

        // Fetch videos that have been uploaded and haven't been checked in 6 hours
        var videos = (await _uow.VideoJobs.FindAsync(
            j => j.Status == VideoJobStatus.Completed
              && j.YouTubeVideoId != null
              && (j.AnalyticsLastFetchedAt == null || j.AnalyticsLastFetchedAt < DateTime.UtcNow.AddHours(-6)),
            ct
        )).ToList();

        _logger.LogInformation("Fetching analytics for {Count} videos", videos.Count);

        foreach (var video in videos)
        {
            try
            {
                await FetchVideoAnalyticsAsync(video.Id, ct);
                await Task.Delay(200, ct); // Rate limiting
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Analytics fetch failed for video {VideoId}", video.Id);
            }
        }

        _logger.LogInformation("Analytics batch completed");
    }

    public async Task FetchVideoAnalyticsAsync(Guid videoJobId, CancellationToken ct)
    {
        var job = await _uow.VideoJobs.GetByIdAsync(videoJobId, ct);
        if (job == null || string.IsNullOrEmpty(job.YouTubeVideoId)) return;

        var channel = job.ChannelId.HasValue
            ? await _uow.Channels.GetByIdAsync(job.ChannelId.Value, ct)
            : null;

        if (channel == null || string.IsNullOrEmpty(channel.YouTubeAccessToken)) return;

        try
        {
            var url = $"https://www.googleapis.com/youtube/v3/videos"
                + $"?part=statistics"
                + $"&id={job.YouTubeVideoId}";

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", channel.YouTubeAccessToken);

            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            var items = json.GetProperty("items").EnumerateArray().ToList();

            if (!items.Any()) return;

            var stats = items[0].GetProperty("statistics");

            job.Views = ParseLong(stats, "viewCount");
            job.Likes = ParseLong(stats, "likeCount");
            job.Comments = ParseLong(stats, "commentCount");
            job.AnalyticsLastFetchedAt = DateTime.UtcNow;

            await _uow.VideoJobs.UpdateAsync(job, ct);
            await _uow.SaveChangesAsync(ct);

            // Update channel totals
            var allChannelVideos = await _uow.VideoJobs.FindAsync(
                j => j.ChannelId == channel.Id && j.Views.HasValue, ct);
            channel.TotalViews = allChannelVideos.Sum(v => v.Views ?? 0);
            channel.TotalVideos = allChannelVideos.Count();
            await _uow.Channels.UpdateAsync(channel, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Analytics updated for video {VideoId}: {Views} views", 
                job.YouTubeVideoId, job.Views);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Analytics fetch failed for video {VideoId}", job.YouTubeVideoId);
        }
    }

    private static long? ParseLong(JsonElement element, string property)
    {
        try
        {
            if (element.TryGetProperty(property, out var prop))
                return long.Parse(prop.GetString() ?? "0");
        }
        catch { }
        return null;
    }
}

/// <summary>
/// Scheduled job runner – called by Hangfire RecurringJob
/// </summary>
public class AnalyticsScheduler
{
    private readonly IAnalyticsService _analytics;
    private readonly ILogger<AnalyticsScheduler> _logger;

    public AnalyticsScheduler(IAnalyticsService analytics, ILogger<AnalyticsScheduler> logger)
    {
        _analytics = analytics;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    [AutomaticRetry(Attempts = 1)]
    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[AnalyticsScheduler] Starting periodic analytics sync");
        await _analytics.FetchYouTubeAnalyticsAsync(ct);
    }
}

/// <summary>
/// Register recurring jobs in Program.cs startup
/// </summary>
public static class RecurringJobRegistration
{
    public static void RegisterRecurringJobs()
    {
        // Sync analytics every 6 hours
        RecurringJob.AddOrUpdate<AnalyticsScheduler>(
            "youtube-analytics-sync",
            job => job.RunAsync(CancellationToken.None),
            Cron.HourInterval(6)
        );

        // Reset monthly quotas at midnight on 1st of each month
        RecurringJob.AddOrUpdate<QuotaResetScheduler>(
            "monthly-quota-reset",
            job => job.RunAsync(CancellationToken.None),
            Cron.Monthly(1, 0)
        );
    }
}

public class QuotaResetScheduler
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<QuotaResetScheduler> _logger;

    public QuotaResetScheduler(IUnitOfWork uow, ILogger<QuotaResetScheduler> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[QuotaResetScheduler] Resetting monthly video quotas");

        var tenants = (await _uow.Tenants.FindAsync(t => t.IsActive && t.QuotaResetAt <= DateTime.UtcNow, ct)).ToList();

        foreach (var tenant in tenants)
        {
            tenant.VideosCreatedThisMonth = 0;
            tenant.QuotaResetAt = DateTime.UtcNow.AddMonths(1);
            await _uow.Tenants.UpdateAsync(tenant, ct);
        }

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("[QuotaResetScheduler] Reset quotas for {Count} tenants", tenants.Count);
    }
}
