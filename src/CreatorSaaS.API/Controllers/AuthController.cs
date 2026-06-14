using MediatR;
using Microsoft.AspNetCore.Mvc;
using CreatorSaaS.Application.Commands.Auth;
using CreatorSaaS.Application.DTOs;
using System.Security.Claims;

namespace CreatorSaaS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] CreateVideoJobDto dto)
    {
        try
        {
            var command = new RegisterCommand(
                dto.Keywords ?? "CreatorSaaS Tenant",
                "email@example.com",
                "password",
                "FirstName",
                "LastName"
            );
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        try
        {
            var command = new LoginCommand(req.Email, req.Password);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return Unauthorized(new { error = "Invalid credentials" });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest req)
    {
        try
        {
            var command = new RefreshTokenCommand(req.RefreshToken);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return Unauthorized(new { error = "Invalid refresh token" });
        }
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        return Ok(new { userId, email, role });
    }
}

public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
