using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreatorSaaS.Application.Commands.Videos;
using CreatorSaaS.Application.DTOs;
using CreatorSaaS.Application.Queries.Videos;
using System.Security.Claims;

namespace CreatorSaaS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VideosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VideosController> _logger;

    public VideosController(IMediator mediator, ILogger<VideosController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());
    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpPost]
    public async Task<ActionResult<VideoJobDetailDto>> CreateVideoJob([FromBody] CreateVideoJobDto dto)
    {
        try
        {
            var command = new CreateVideoJobCommand(TenantId, UserId, dto);
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetVideoJob), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create video job failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VideoJobDetailDto>> GetVideoJob(Guid id)
    {
        try
        {
            var query = new GetVideoJobByIdQuery(TenantId, id);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get video job failed: {Id}", id);
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<VideoJobListDto>>> ListVideoJobs(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? channelId,
        [FromQuery] int status = -1,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "created",
        [FromQuery] string sortOrder = "desc")
    {
        try
        {
            var query = new ListVideoJobsQuery(
                TenantId, projectId, channelId, status, page, pageSize, sortBy, sortOrder
            );
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List video jobs failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelVideoJob(Guid id)
    {
        try
        {
            var command = new CancelVideoJobCommand(TenantId, UserId, id);
            await _mediator.Send(command);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cancel video job failed: {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/variants")]
    public async Task<ActionResult<VideoJobDetailDto>> CreateVideoVariant(
        Guid id,
        [FromBody] GenerateVideoVariantDto dto)
    {
        try
        {
            var command = new CreateVideoVariantCommand(TenantId, UserId, dto);
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetVideoJob), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create video variant failed");
            return BadRequest(new { error = ex.Message });
        }
    }
}
