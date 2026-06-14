using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreatorSaaS.Application.DTOs;
using CreatorSaaS.Core.Entities;
using CreatorSaaS.Core.Interfaces;
using CreatorSaaS.Core.Exceptions;
using System.Security.Claims;

namespace CreatorSaaS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IUnitOfWork uow, ILogger<ProjectsController> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());
    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDto dto, CancellationToken ct)
    {
        try
        {
            // Verify unique name in tenant
            var existing = await _uow.Projects.FirstOrDefaultAsync(
                p => p.TenantId == TenantId && p.Name == dto.Name, ct);
            if (existing != null)
                return BadRequest(new { error = "A project with this name already exists" });

            var project = new Project
            {
                TenantId = TenantId,
                Name = dto.Name,
                Description = dto.Description,
                DefaultLanguage = dto.DefaultLanguage ?? "en",
                DefaultVoiceId = dto.DefaultVoiceId,
                DefaultStyle = dto.DefaultStyle ?? "tutorial",
                IsActive = true
            };

            await _uow.Projects.AddAsync(project, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Project created: {ProjectId} for tenant {TenantId}", project.Id, TenantId);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, MapDto(project));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project creation failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> List(CancellationToken ct)
    {
        var projects = await _uow.Projects.FindAsync(p => p.TenantId == TenantId, ct);
        return Ok(projects.OrderByDescending(p => p.CreatedAt).Select(MapDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken ct)
    {
        var project = await _uow.Projects.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Project", id);

        if (project.TenantId != TenantId)
            return Forbid();

        return Ok(MapDto(project));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, [FromBody] CreateProjectDto dto, CancellationToken ct)
    {
        var project = await _uow.Projects.GetByIdAsync(id, ct);
        if (project == null || project.TenantId != TenantId)
            return NotFound();

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.DefaultLanguage = dto.DefaultLanguage ?? project.DefaultLanguage;
        project.DefaultVoiceId = dto.DefaultVoiceId ?? project.DefaultVoiceId;
        project.DefaultStyle = dto.DefaultStyle ?? project.DefaultStyle;

        await _uow.Projects.UpdateAsync(project, ct);
        await _uow.SaveChangesAsync(ct);

        return Ok(MapDto(project));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var project = await _uow.Projects.GetByIdAsync(id, ct);
        if (project == null || project.TenantId != TenantId)
            return NotFound();

        await _uow.Projects.DeleteAsync(project, ct);
        await _uow.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpGet("{id}/analytics")]
    public async Task<ActionResult<ProjectAnalyticsSummaryDto>> GetAnalytics(Guid id, CancellationToken ct)
    {
        var project = await _uow.Projects.GetByIdAsync(id, ct);
        if (project == null || project.TenantId != TenantId)
            return NotFound();

        var jobs = (await _uow.VideoJobs.FindAsync(j => j.ProjectId == id && j.YouTubeVideoId != null, ct)).ToList();

        var summary = new ProjectAnalyticsSummaryDto(
            jobs.Count,
            jobs.Sum(j => j.Views ?? 0),
            jobs.Sum(j => j.Likes ?? 0),
            jobs.Any(j => j.ClickThroughRate.HasValue)
                ? jobs.Where(j => j.ClickThroughRate.HasValue).Average(j => j.ClickThroughRate!.Value)
                : 0,
            jobs.Any(j => j.AverageViewDurationSeconds.HasValue)
                ? jobs.Where(j => j.AverageViewDurationSeconds.HasValue).Average(j => j.AverageViewDurationSeconds!.Value)
                : 0,
            jobs.OrderByDescending(j => j.Views)
                .Take(5)
                .Select(j => new VideoAnalyticsDto(j.Id, j.Views, j.Likes, j.Comments,
                    j.ClickThroughRate, j.AverageViewDurationSeconds, j.AnalyticsLastFetchedAt))
                .ToList()
        );

        return Ok(summary);
    }

    private static ProjectDto MapDto(Project p) =>
        new(p.Id, p.Name, p.Description, p.DefaultLanguage, p.DefaultVoiceId, p.DefaultStyle, p.CreatedAt);
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChannelsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IYouTubeUploadService _youtube;
    private readonly ILogger<ChannelsController> _logger;
    private readonly IConfiguration _config;

    public ChannelsController(IUnitOfWork uow, IYouTubeUploadService youtube,
        IConfiguration config, ILogger<ChannelsController> logger)
    {
        _uow = uow;
        _youtube = youtube;
        _config = config;
        _logger = logger;
    }

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    [HttpPost]
    public async Task<ActionResult<ChannelDto>> Create([FromBody] CreateChannelDto dto, CancellationToken ct)
    {
        var project = await _uow.Projects.GetByIdAsync(dto.ProjectId, ct);
        if (project == null || project.TenantId != TenantId)
            return Forbid();

        var channel = new Channel
        {
            TenantId = TenantId,
            ProjectId = dto.ProjectId,
            Name = dto.Name,
            IsConnected = false
        };

        await _uow.Channels.AddAsync(channel, ct);
        await _uow.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = channel.Id }, MapDto(channel));
    }

    [HttpGet]
    public async Task<ActionResult<List<ChannelDto>>> List([FromQuery] Guid? projectId, CancellationToken ct)
    {
        var channels = await _uow.Channels.FindAsync(
            c => c.TenantId == TenantId && (projectId == null || c.ProjectId == projectId), ct);

        return Ok(channels.OrderBy(c => c.Name).Select(MapDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChannelDto>> GetById(Guid id, CancellationToken ct)
    {
        var channel = await _uow.Channels.GetByIdAsync(id, ct);
        if (channel == null || channel.TenantId != TenantId)
            return NotFound();

        return Ok(MapDto(channel));
    }

    // Returns YouTube OAuth URL for channel connection
    [HttpGet("{id}/youtube/auth-url")]
    public ActionResult<object> GetYouTubeAuthUrl(Guid id)
    {
        var clientId = _config["YouTube:ClientId"];
        var redirectUri = _config["YouTube:RedirectUri"] ?? $"{Request.Scheme}://{Request.Host}/api/oauth/youtube/callback";
        var state = $"{id}:{TenantId}";

        var authUrl = $"https://accounts.google.com/o/oauth2/auth"
            + $"?client_id={Uri.EscapeDataString(clientId ?? "")}"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + $"&response_type=code"
            + $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/youtube.upload https://www.googleapis.com/auth/youtube")}"
            + $"&access_type=offline"
            + $"&prompt=consent"
            + $"&state={Uri.EscapeDataString(state)}";

        return Ok(new { url = authUrl });
    }

    // Called after YouTube OAuth callback
    [HttpPost("{id}/youtube/connect")]
    public async Task<ActionResult<ChannelDto>> ConnectYouTube(Guid id, [FromBody] ConnectYouTubeDto dto, CancellationToken ct)
    {
        var channel = await _uow.Channels.GetByIdAsync(id, ct);
        if (channel == null || channel.TenantId != TenantId)
            return NotFound();

        try
        {
            // Exchange auth code for tokens
            var accessToken = await _youtube.RefreshAccessTokenAsync(dto.AuthorizationCode, ct);
            channel.YouTubeAccessToken = accessToken;
            channel.YouTubeTokenExpiry = DateTime.UtcNow.AddHours(1);
            channel.IsConnected = true;

            await _uow.Channels.UpdateAsync(channel, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("YouTube connected for channel {ChannelId}", id);
            return Ok(MapDto(channel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube connection failed");
            return BadRequest(new { error = "Failed to connect YouTube: " + ex.Message });
        }
    }

    [HttpDelete("{id}/youtube/disconnect")]
    public async Task<ActionResult> DisconnectYouTube(Guid id, CancellationToken ct)
    {
        var channel = await _uow.Channels.GetByIdAsync(id, ct);
        if (channel == null || channel.TenantId != TenantId)
            return NotFound();

        channel.YouTubeAccessToken = null;
        channel.YouTubeRefreshToken = null;
        channel.YouTubeTokenExpiry = null;
        channel.YouTubeChannelId = null;
        channel.IsConnected = false;

        await _uow.Channels.UpdateAsync(channel, ct);
        await _uow.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var channel = await _uow.Channels.GetByIdAsync(id, ct);
        if (channel == null || channel.TenantId != TenantId)
            return NotFound();

        await _uow.Channels.DeleteAsync(channel, ct);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ChannelDto MapDto(Channel c) =>
        new(c.Id, c.ProjectId, c.Name, c.IsConnected, c.YouTubeChannelId, c.TotalViews, c.TotalVideos);
}
