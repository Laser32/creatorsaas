using CreatorSaaS.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CreatorSaaS.Infrastructure.Services;

public class SendGridEmailService : IEmailService
{
    private readonly SendGridClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> logger)
    {
        var apiKey = config["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid:ApiKey not configured");
        _client = new SendGridClient(apiKey);
        _config = config;
        _logger = logger;
        _fromEmail = config["Email:From"] ?? "noreply@creatorsaas.io";
        _fromName = config["Email:FromName"] ?? "CreatorSaaS";
    }

    public async Task SendVerificationEmailAsync(string email, string name, string token, CancellationToken ct = default)
    {
        try
        {
            var verificationUrl = $"{_config["App:BaseUrl"]}/verify-email?token={Uri.EscapeDataString(token)}";

            var subject = "Verify Your Email - CreatorSaaS";
            var htmlContent = $"""
                <h1>Verify Your Email</h1>
                <p>Hello {name},</p>
                <p>Welcome to CreatorSaaS! Please verify your email by clicking the link below:</p>
                <a href="{verificationUrl}" style="background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;">
                    Verify Email
                </a>
                <p>Or copy this link: {verificationUrl}</p>
                <p>This link expires in 24 hours.</p>
                """;

            await SendEmailAsync(email, subject, htmlContent, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verification email failed for {Email}", email);
        }
    }

    public async Task SendPasswordResetEmailAsync(string email, string name, string token, CancellationToken ct = default)
    {
        try
        {
            var resetUrl = $"{_config["App:BaseUrl"]}/reset-password?token={Uri.EscapeDataString(token)}";

            var subject = "Reset Your Password - CreatorSaaS";
            var htmlContent = $"""
                <h1>Password Reset Request</h1>
                <p>Hello {name},</p>
                <p>We received a request to reset your password. Click the link below to proceed:</p>
                <a href="{resetUrl}" style="background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;">
                    Reset Password
                </a>
                <p>Or copy this link: {resetUrl}</p>
                <p>This link expires in 1 hour. If you didn't request this, please ignore this email.</p>
                """;

            await SendEmailAsync(email, subject, htmlContent, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset email failed for {Email}", email);
        }
    }

    public async Task SendVideoCompleteEmailAsync(string email, string name, string videoTitle, string youtubeUrl, CancellationToken ct = default)
    {
        try
        {
            var subject = $"Your Video is Ready! '{videoTitle}' - CreatorSaaS";
            var htmlContent = $"""
                <h1>Your Video is Ready!</h1>
                <p>Hello {name},</p>
                <p>Your AI-generated video has been successfully uploaded to YouTube.</p>
                <h2>{videoTitle}</h2>
                <a href="{youtubeUrl}" style="background-color: #ff0000; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;">
                    Watch on YouTube
                </a>
                <p>URL: {youtubeUrl}</p>
                <p>Check out your dashboard to see video analytics and create more videos.</p>
                """;

            await SendEmailAsync(email, subject, htmlContent, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video complete email failed for {Email}", email);
        }
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlContent, CancellationToken ct)
    {
        var from = new EmailAddress(_fromEmail, _fromName);
        var to = new EmailAddress(toEmail);
        var msg = new SendGridMessage()
        {
            From = from,
            Subject = subject,
            HtmlContent = htmlContent
        };
        msg.AddTo(to);

        var response = await _client.SendEmailAsync(msg, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Body.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"SendGrid error: {response.StatusCode} - {errorBody}");
        }

        _logger.LogInformation("Email sent to {Email}", toEmail);
    }
}
