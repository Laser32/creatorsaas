using MediatR;
using CreatorSaaS.Core.Entities;
using CreatorSaaS.Core.Enums;
using CreatorSaaS.Core.Interfaces;
using CreatorSaaS.Core.Exceptions;
using CreatorSaaS.Application.DTOs;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace CreatorSaaS.Application.Commands.Videos;

// ─── CreateVideoJob ──────────────────────────────────────────────────────────

public record CreateVideoJobCommand(
    Guid TenantId,
    Guid UserId,
    CreateVideoJobDto Input
) : IRequest<VideoJobDetailDto>;

public class CreateVideoJobCommandHandler : IRequestHandler<CreateVideoJobCommand, VideoJobDetailDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<CreateVideoJobCommandHandler> _logger;

    public CreateVideoJobCommandHandler(IUnitOfWork uow, IBackgroundJobClient backgroundJobs,
        ILogger<CreateVideoJobCommandHandler> logger)
    {
        _uow = uow;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task<VideoJobDetailDto> Handle(CreateVideoJobCommand cmd, CancellationToken ct)
    {
        var tenant = await _uow.Tenants.GetByIdAsync(cmd.TenantId, ct)
            ?? throw new NotFoundException("Tenant", cmd.TenantId);

        if (!tenant.IsActive)
            throw new DomainException("TENANT_INACTIVE", "Tenant account is not active.");

        // Check quota
        if (tenant.VideosCreatedThisMonth >= tenant.MonthlyVideoQuota)
            throw new QuotaExceededException(tenant.Plan, tenant.MonthlyVideoQuota);

        // Verify user belongs to tenant
        var user = await _uow.Users.GetByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        if (user.TenantId != cmd.TenantId)
            throw new ForbiddenException();

        // Verify project belongs to tenant
        var project = await _uow.Projects.GetByIdAsync(cmd.Input.ProjectId, ct)
            ?? throw new NotFoundException("Project", cmd.Input.ProjectId);

        if (project.TenantId != cmd.TenantId)
            throw new ForbiddenException();

        // Verify channel if provided
        if (cmd.Input.ChannelId.HasValue)
        {
            var channel = await _uow.Channels.GetByIdAsync(cmd.Input.ChannelId.Value, ct)
                ?? throw new NotFoundException("Channel", cmd.Input.ChannelId.Value);

            if (channel.TenantId != cmd.TenantId || channel.ProjectId != cmd.Input.ProjectId)
                throw new ForbiddenException();
        }

        // Create video job
        var videoJob = new VideoJob
        {
            TenantId = cmd.TenantId,
            ProjectId = cmd.Input.ProjectId,
            ChannelId = cmd.Input.ChannelId,
            CreatedByUserId = cmd.UserId,
            Topic = cmd.Input.Topic,
            Keywords = cmd.Input.Keywords,
            Language = cmd.Input.Language,
            Style = cmd.Input.Style,
            TargetDurationSeconds = cmd.Input.TargetDurationSeconds,
            VoiceId = cmd.Input.VoiceId ?? project.DefaultVoiceId,
            Status = VideoJobStatus.Pending,
            CurrentStep = 0
        };

        await _uow.VideoJobs.AddAsync(videoJob, ct);
        tenant.VideosCreatedThisMonth += 1;
        await _uow.SaveChangesAsync(ct);

        // Enqueue background job to start pipeline
        var hangfireJobId = _backgroundJobs.Enqueue<IVideoProcessingService>(
            svc => svc.StartVideoProcessingAsync(videoJob.Id, CancellationToken.None)
        );
        videoJob.HangfireJobId = hangfireJobId;
        videoJob.Status = VideoJobStatus.Queued;
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("VideoJob created: {VideoJobId} for tenant {TenantId}", videoJob.Id, cmd.TenantId);

        return MapToDetail(videoJob, new List<VideoScene>());
    }

    private VideoJobDetailDto MapToDetail(VideoJob job, List<VideoScene> scenes) =>
        new(
            job.Id, job.ProjectId, job.ChannelId, job.Topic, job.Keywords, job.Language,
            job.Style, job.TargetDurationSeconds, job.VoiceId, (int)job.Status, job.CurrentStep,
            job.ErrorMessage, job.RetryCount, job.ScriptTitle, job.ScriptContent,
            job.GeneratedTitle, job.GeneratedDescription, job.Tags,
            job.YouTubeVideoId, job.YouTubeUrl, job.Views, job.Likes, job.Comments,
            job.ClickThroughRate, job.CreatedAt, job.CompletedAt,
            scenes.OrderBy(s => s.Order).Select(s => new VideoSceneDto(
                s.Id, s.Order, s.Title, s.Narration, s.BRollQuery, s.DurationSeconds,
                s.AudioGenerated, s.BRollFetched
            )).ToList()
        );
}

// ─── CancelVideoJob ──────────────────────────────────────────────────────────

public record CancelVideoJobCommand(
    Guid TenantId,
    Guid UserId,
    Guid VideoJobId
) : IRequest<Unit>;

public class CancelVideoJobCommandHandler : IRequestHandler<CancelVideoJobCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<CancelVideoJobCommandHandler> _logger;

    public CancelVideoJobCommandHandler(IUnitOfWork uow, IBackgroundJobClient backgroundJobs,
        ILogger<CancelVideoJobCommandHandler> logger)
    {
        _uow = uow;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task<Unit> Handle(CancelVideoJobCommand cmd, CancellationToken ct)
    {
        var job = await _uow.VideoJobs.GetByIdAsync(cmd.VideoJobId, ct)
            ?? throw new NotFoundException("VideoJob", cmd.VideoJobId);

        if (job.TenantId != cmd.TenantId || job.CreatedByUserId != cmd.UserId)
            throw new ForbiddenException();

        if (job.Status == VideoJobStatus.Completed || job.Status == VideoJobStatus.Cancelled)
            throw new DomainException("INVALID_STATE", "Cannot cancel a completed or already cancelled job.");

        job.Status = VideoJobStatus.Cancelled;
        if (!string.IsNullOrEmpty(job.HangfireJobId))
            _backgroundJobs.Delete(job.HangfireJobId);

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("VideoJob cancelled: {VideoJobId}", job.Id);

        return Unit.Value;
    }
}

// ─── CreateVideoVariant (A/B Testing) ────────────────────────────────────────

public record CreateVideoVariantCommand(
    Guid TenantId,
    Guid UserId,
    GenerateVideoVariantDto Input
) : IRequest<VideoJobDetailDto>;

public class CreateVideoVariantCommandHandler : IRequestHandler<CreateVideoVariantCommand, VideoJobDetailDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<CreateVideoVariantCommandHandler> _logger;

    public CreateVideoVariantCommandHandler(IUnitOfWork uow, IBackgroundJobClient backgroundJobs,
        ILogger<CreateVideoVariantCommandHandler> logger)
    {
        _uow = uow;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task<VideoJobDetailDto> Handle(CreateVideoVariantCommand cmd, CancellationToken ct)
    {
        var parentJob = await _uow.VideoJobs.GetByIdAsync(cmd.Input.ParentVideoJobId, ct)
            ?? throw new NotFoundException("VideoJob", cmd.Input.ParentVideoJobId);

        if (parentJob.TenantId != cmd.TenantId || parentJob.CreatedByUserId != cmd.UserId)
            throw new ForbiddenException();

        if (parentJob.Status != VideoJobStatus.Completed)
            throw new DomainException("INVALID_STATE", "Parent job must be completed before creating a variant.");

        // Create variant job
        var variantJob = new VideoJob
        {
            TenantId = cmd.TenantId,
            ProjectId = parentJob.ProjectId,
            ChannelId = parentJob.ChannelId,
            CreatedByUserId = cmd.UserId,
            Topic = parentJob.Topic,
            Keywords = parentJob.Keywords,
            Language = parentJob.Language,
            Style = parentJob.Style,
            TargetDurationSeconds = parentJob.TargetDurationSeconds,
            VoiceId = parentJob.VoiceId,
            Status = VideoJobStatus.Pending,
            CurrentStep = 0,
            IsVariant = true,
            ParentJobId = parentJob.Id,
            VariantLabel = cmd.Input.VariantLabel,
            ScriptTitle = cmd.Input.TitleOverride ?? parentJob.ScriptTitle,
            GeneratedDescription = cmd.Input.DescriptionOverride ?? parentJob.GeneratedDescription,
            ScriptContent = parentJob.ScriptContent  // Reuse script
        };

        await _uow.VideoJobs.AddAsync(variantJob, ct);
        await _uow.SaveChangesAsync(ct);

        // Enqueue rendering (skip script generation)
        var hangfireJobId = _backgroundJobs.Enqueue<IVideoProcessingService>(
            svc => svc.ContinueVideoProcessingAsync(variantJob.Id, 5, CancellationToken.None)
        );
        variantJob.HangfireJobId = hangfireJobId;
        variantJob.Status = VideoJobStatus.Queued;
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("VideoJob variant created: {VariantId} from parent {ParentId}", 
            variantJob.Id, parentJob.Id);

        return MapToDetail(variantJob, new List<VideoScene>());
    }

    private VideoJobDetailDto MapToDetail(VideoJob job, List<VideoScene> scenes) =>
        new(
            job.Id, job.ProjectId, job.ChannelId, job.Topic, job.Keywords, job.Language,
            job.Style, job.TargetDurationSeconds, job.VoiceId, (int)job.Status, job.CurrentStep,
            job.ErrorMessage, job.RetryCount, job.ScriptTitle, job.ScriptContent,
            job.GeneratedTitle, job.GeneratedDescription, job.Tags,
            job.YouTubeVideoId, job.YouTubeUrl, job.Views, job.Likes, job.Comments,
            job.ClickThroughRate, job.CreatedAt, job.CompletedAt,
            scenes.OrderBy(s => s.Order).Select(s => new VideoSceneDto(
                s.Id, s.Order, s.Title, s.Narration, s.BRollQuery, s.DurationSeconds,
                s.AudioGenerated, s.BRollFetched
            )).ToList()
        );
}

// Marker interface for Hangfire
public interface IVideoProcessingService
{
    Task StartVideoProcessingAsync(Guid videoJobId, CancellationToken ct);
    Task ContinueVideoProcessingAsync(Guid videoJobId, int fromStep, CancellationToken ct);
}
