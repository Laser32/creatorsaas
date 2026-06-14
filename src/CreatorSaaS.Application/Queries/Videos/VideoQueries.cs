using MediatR;
using CreatorSaaS.Core.Interfaces;
using CreatorSaaS.Core.Exceptions;
using CreatorSaaS.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace CreatorSaaS.Application.Queries.Videos;

// ─── GetVideoJobById ──────────────────────────────────────────────────────────

public record GetVideoJobByIdQuery(
    Guid TenantId,
    Guid VideoJobId
) : IRequest<VideoJobDetailDto>;

public class GetVideoJobByIdQueryHandler : IRequestHandler<GetVideoJobByIdQuery, VideoJobDetailDto>
{
    private readonly IUnitOfWork _uow;

    public GetVideoJobByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<VideoJobDetailDto> Handle(GetVideoJobByIdQuery query, CancellationToken ct)
    {
        var job = await _uow.VideoJobs.GetByIdAsync(query.VideoJobId, ct)
            ?? throw new NotFoundException("VideoJob", query.VideoJobId);

        if (job.TenantId != query.TenantId)
            throw new ForbiddenException();

        var scenes = await _uow.VideoScenes.FindAsync(s => s.VideoJobId == job.Id, ct);

        return MapToDetail(job, scenes.ToList());
    }

    private VideoJobDetailDto MapToDetail(Core.Entities.VideoJob job, List<Core.Entities.VideoScene> scenes) =>
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

// ─── ListVideoJobs ───────────────────────────────────────────────────────────

public record ListVideoJobsQuery(
    Guid TenantId,
    Guid? ProjectId = null,
    Guid? ChannelId = null,
    int Status = -1,  // -1 = all
    int Page = 1,
    int PageSize = 20,
    string SortBy = "created",
    string SortOrder = "desc"
) : IRequest<PaginatedResponse<VideoJobListDto>>;

public class ListVideoJobsQueryHandler : IRequestHandler<ListVideoJobsQuery, PaginatedResponse<VideoJobListDto>>
{
    private readonly IUnitOfWork _uow;

    public ListVideoJobsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PaginatedResponse<VideoJobListDto>> Handle(ListVideoJobsQuery query, CancellationToken ct)
    {
        var q = _uow.VideoJobs.Query()
            .Where(j => j.TenantId == query.TenantId && !j.IsDeleted);

        if (query.ProjectId.HasValue)
            q = q.Where(j => j.ProjectId == query.ProjectId);

        if (query.ChannelId.HasValue)
            q = q.Where(j => j.ChannelId == query.ChannelId);

        if (query.Status >= 0)
            q = q.Where(j => (int)j.Status == query.Status);

        // Sorting
        q = query.SortBy.ToLower() switch
        {
            "title" => query.SortOrder == "asc" 
                ? q.OrderBy(j => j.ScriptTitle) 
                : q.OrderByDescending(j => j.ScriptTitle),
            "views" => query.SortOrder == "asc" 
                ? q.OrderBy(j => j.Views) 
                : q.OrderByDescending(j => j.Views),
            _ => query.SortOrder == "asc" 
                ? q.OrderBy(j => j.CreatedAt) 
                : q.OrderByDescending(j => j.CreatedAt)
        };

        var total = await _uow.VideoJobs.CountAsync(j => 
            j.TenantId == query.TenantId && !j.IsDeleted &&
            (query.ProjectId == null || j.ProjectId == query.ProjectId) &&
            (query.ChannelId == null || j.ChannelId == query.ChannelId) &&
            (query.Status < 0 || (int)j.Status == query.Status), ct);

        var skip = (query.Page - 1) * query.PageSize;
        var jobs = q.Skip(skip).Take(query.PageSize).ToList();

        var dtos = jobs.Select(j => new VideoJobListDto(
            j.Id, j.Topic, j.ScriptTitle, (int)j.Status, j.CurrentStep,
            j.CreatedAt, j.CompletedAt, j.YouTubeUrl, j.Views
        )).ToList();

        var totalPages = (int)Math.Ceiling(total / (double)query.PageSize);
        return new PaginatedResponse<VideoJobListDto>(dtos, query.Page, query.PageSize, total, totalPages);
    }
}

// ─── GetProjectAnalytics ──────────────────────────────────────────────────────

public record GetProjectAnalyticsQuery(
    Guid TenantId,
    Guid ProjectId
) : IRequest<ProjectAnalyticsSummaryDto>;

public class GetProjectAnalyticsQueryHandler : IRequestHandler<GetProjectAnalyticsQuery, ProjectAnalyticsSummaryDto>
{
    private readonly IUnitOfWork _uow;

    public GetProjectAnalyticsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ProjectAnalyticsSummaryDto> Handle(GetProjectAnalyticsQuery query, CancellationToken ct)
    {
        var project = await _uow.Projects.GetByIdAsync(query.ProjectId, ct)
            ?? throw new NotFoundException("Project", query.ProjectId);

        if (project.TenantId != query.TenantId)
            throw new ForbiddenException();

        var jobs = (await _uow.VideoJobs.FindAsync(
            j => j.ProjectId == query.ProjectId && j.YouTubeVideoId != null, ct)).ToList();

        var totalVideos = jobs.Count;
        var totalViews = jobs.Sum(j => j.Views ?? 0);
        var totalLikes = jobs.Sum(j => j.Likes ?? 0);
        var avgCtr = jobs.Any(j => j.ClickThroughRate.HasValue)
            ? jobs.Where(j => j.ClickThroughRate.HasValue).Average(j => j.ClickThroughRate!.Value)
            : 0;
        var avgDuration = jobs.Any(j => j.AverageViewDurationSeconds.HasValue)
            ? jobs.Where(j => j.AverageViewDurationSeconds.HasValue).Average(j => j.AverageViewDurationSeconds!.Value)
            : 0;

        var topVideos = jobs
            .OrderByDescending(j => j.Views)
            .Take(5)
            .Select(j => new VideoAnalyticsDto(
                j.Id, j.Views, j.Likes, j.Comments, j.ClickThroughRate,
                j.AverageViewDurationSeconds, j.AnalyticsLastFetchedAt
            ))
            .ToList();

        return new ProjectAnalyticsSummaryDto(
            totalVideos, totalViews, totalLikes, avgCtr, avgDuration, topVideos
        );
    }
}
