using CreatorSaaS.Application.Commands.Videos;
using CreatorSaaS.Core.Entities;
using CreatorSaaS.Core.Enums;
using CreatorSaaS.Core.Interfaces;
using CreatorSaaS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CreatorSaaS.Workers.VideoProcessing;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly IUnitOfWork _uow;
    private readonly IAIScriptService _scriptService;
    private readonly IVoiceSynthesisService _voiceService;
    private readonly IBRollService _brollService;
    private readonly IVideoRenderService _renderService;
    private readonly IThumbnailService _thumbnailService;
    private readonly IYouTubeUploadService _youtubeService;
    private readonly IStorageService _storage;
    private readonly IEmailService _emailService;
    private readonly ILogger<VideoProcessingService> _logger;

    public VideoProcessingService(
        IUnitOfWork uow,
        IAIScriptService scriptService,
        IVoiceSynthesisService voiceService,
        IBRollService brollService,
        IVideoRenderService renderService,
        IThumbnailService thumbnailService,
        IYouTubeUploadService youtubeService,
        IStorageService storage,
        IEmailService emailService,
        ILogger<VideoProcessingService> logger)
    {
        _uow = uow;
        _scriptService = scriptService;
        _voiceService = voiceService;
        _brollService = brollService;
        _renderService = renderService;
        _thumbnailService = thumbnailService;
        _youtubeService = youtubeService;
        _storage = storage;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task StartVideoProcessingAsync(Guid videoJobId, CancellationToken ct)
    {
        var job = await _uow.VideoJobs.GetByIdAsync(videoJobId, ct)
            ?? throw new InvalidOperationException($"VideoJob {videoJobId} not found");

        job.Status = VideoJobStatus.GeneratingScript;
        job.StartedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        try
        {
            // Step 1: Generate Script
            _logger.LogInformation("Step 1: Generating script for job {JobId}", job.Id);
            var scriptResult = await _scriptService.GenerateScriptAsync(
                job.Topic, job.Style, job.Language, job.Keywords, job.TargetDurationSeconds, ct
            );

            job.ScriptTitle = scriptResult.Title;
            job.ScriptContent = scriptResult.Content;
            job.CurrentStep = 1;
            job.Status = VideoJobStatus.GeneratingScenes;
            await _uow.SaveChangesAsync(ct);

            // Step 2: Create VideoScenes
            _logger.LogInformation("Step 2: Generating scenes for job {JobId}", job.Id);
            var scenes = scriptResult.Scenes.Select((s, idx) => new VideoScene
            {
                VideoJobId = job.Id,
                Order = idx + 1,
                Title = s.Title,
                Narration = s.Narration,
                BRollQuery = s.BRollQuery,
                DurationSeconds = s.DurationSeconds
            }).ToList();

            foreach (var scene in scenes)
                await _uow.VideoScenes.AddAsync(scene, ct);
            await _uow.SaveChangesAsync(ct);

            // Step 3: Generate Title & Description
            _logger.LogInformation("Step 3: Generating title for job {JobId}", job.Id);
            var titleResult = await _scriptService.GenerateTitleAndDescriptionAsync(
                job.ScriptContent, job.Topic, ct
            );
            job.GeneratedTitle = titleResult.Title;
            job.GeneratedDescription = titleResult.Description;
            job.Tags = string.Join(",", titleResult.Tags);
            job.CurrentStep = 3;
            job.Status = VideoJobStatus.SynthesizingVoice;
            await _uow.SaveChangesAsync(ct);

            // Step 4: Synthesize Voice for all scenes
            _logger.LogInformation("Step 4: Synthesizing voice for job {JobId}", job.Id);
            var voiceId = job.VoiceId ?? "21m00Tcm4TlvDq8ikWAM";  // Default ElevenLabs voice
            var scenesDb = (await _uow.VideoScenes.FindAsync(s => s.VideoJobId == job.Id, ct)).OrderBy(s => s.Order).ToList();

            foreach (var scene in scenesDb)
            {
                var audioPath = Path.Combine(Path.GetTempPath(), $"audio_{scene.Id}.mp3");
                try
                {
                    await _voiceService.SynthesizeAsync(scene.Narration, voiceId, audioPath, ct);
                    scene.AudioFilePath = audioPath;
                    scene.AudioGenerated = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Voice synthesis failed for scene {SceneId}", scene.Id);
                }
            }
            job.CurrentStep = 4;
            job.Status = VideoJobStatus.FetchingBRoll;
            await _uow.SaveChangesAsync(ct);

            // Step 5: Fetch B-Roll for all scenes
            _logger.LogInformation("Step 5: Fetching B-Roll for job {JobId}", job.Id);
            foreach (var scene in scenesDb)
            {
                if (string.IsNullOrEmpty(scene.BRollQuery))
                    continue;

                var brollPath = Path.Combine(Path.GetTempPath(), $"broll_{scene.Id}.mp4");
                try
                {
                    var result = await _brollService.FetchClipAsync(scene.BRollQuery, brollPath, scene.DurationSeconds ?? 10, ct);
                    if (!string.IsNullOrEmpty(result))
                    {
                        scene.BRollLocalPath = result;
                        scene.BRollFetched = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "B-Roll fetch failed for scene {SceneId}", scene.Id);
                }
            }
            job.CurrentStep = 5;
            job.Status = VideoJobStatus.RenderingVideo;
            await _uow.SaveChangesAsync(ct);

            // Step 6: Render Video
            _logger.LogInformation("Step 6: Rendering video for job {JobId}", job.Id);
            var renderResult = await _renderService.RenderAsync(job, scenesDb, ct);
            job.VideoFilePath = renderResult.OutputPath;
            job.DurationSeconds = renderResult.DurationSeconds;
            job.FileSizeBytes = renderResult.FileSizeBytes;
            job.CurrentStep = 6;
            job.Status = VideoJobStatus.GeneratingThumbnail;
            await _uow.SaveChangesAsync(ct);

            // Step 7: Generate Thumbnail
            _logger.LogInformation("Step 7: Generating thumbnail for job {JobId}", job.Id);
            var thumbPath = Path.Combine(Path.GetTempPath(), $"thumb_{job.Id}.jpg");
            var thumbnailPath = await _thumbnailService.GenerateThumbnailAsync(
                job.GeneratedTitle ?? "Video", job.VideoFilePath, thumbPath, ct
            );
            job.ThumbnailFilePath = thumbnailPath;
            job.CurrentStep = 7;
            job.Status = VideoJobStatus.Uploading;
            await _uow.SaveChangesAsync(ct);

            // Step 8: Upload to YouTube (if channel connected)
            if (job.ChannelId.HasValue)
            {
                _logger.LogInformation("Step 8: Uploading to YouTube for job {JobId}", job.Id);
                var channel = await _uow.Channels.GetByIdAsync(job.ChannelId.Value, ct);
                if (channel?.IsConnected == true && !string.IsNullOrEmpty(channel.YouTubeAccessToken))
                {
                    try
                    {
                        var videoId = await _youtubeService.UploadAsync(
                            job, job.VideoFilePath, thumbnailPath, channel.YouTubeAccessToken, ct
                        );
                        job.YouTubeVideoId = videoId;
                        job.YouTubeUrl = $"https://www.youtube.com/watch?v={videoId}";
                        job.PublishedAt = DateTime.UtcNow;
                        
                        channel.TotalVideos += 1;
                        await _uow.SaveChangesAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "YouTube upload failed for job {JobId}", job.Id);
                    }
                }
            }

            job.CurrentStep = 8;
            job.Status = VideoJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.RetryCount = 0;
            await _uow.SaveChangesAsync(ct);

            // Send completion email
            var user = await _uow.Users.GetByIdAsync(job.CreatedByUserId, ct);
            if (user != null)
            {
                await _emailService.SendVideoCompleteEmailAsync(
                    user.Email, user.FirstName, job.GeneratedTitle ?? "Your Video",
                    job.YouTubeUrl ?? "", ct
                );
            }

            _logger.LogInformation("Video processing completed for job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video processing failed for job {JobId}", job.Id);
            job.Status = VideoJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.RetryCount += 1;
            job.CompletedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);
        }
    }

    public async Task ContinueVideoProcessingAsync(Guid videoJobId, int fromStep, CancellationToken ct)
    {
        _logger.LogInformation("Continuing video processing from step {Step} for job {JobId}", fromStep, videoJobId);
        // Implement for A/B variants - skip script generation, start from rendering
        await StartVideoProcessingAsync(videoJobId, ct);
    }
}
