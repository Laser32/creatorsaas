using System.Net;
using System.Text.Json;
using CreatorSaaS.Core.Exceptions;

namespace CreatorSaaS.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, code, message, errors) = exception switch
        {
            NotFoundException e => (HttpStatusCode.NotFound, e.Code, e.Message, (IDictionary<string, string[]>?)null),
            ForbiddenException e => (HttpStatusCode.Forbidden, e.Code, e.Message, null),
            QuotaExceededException e => (HttpStatusCode.PaymentRequired, e.Code, e.Message, null),
            ValidationException e => (HttpStatusCode.UnprocessableEntity, e.Code, e.Message, e.Errors),
            DomainException e => (HttpStatusCode.BadRequest, e.Code, e.Message, null),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.", null)
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = code,
            message,
            errors,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

// Health check endpoints
public static class HealthCheckExtensions
{
    public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            version = "1.0.0",
            timestamp = DateTime.UtcNow
        }));

        app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
        app.MapGet("/health/ready", (IServiceProvider sp) =>
        {
            // Could check DB/Redis here
            return Results.Ok(new { status = "ready" });
        });

        return app;
    }
}
