using MediatR;
using BCrypt.Net;
using CreatorSaaS.Core.Entities;
using CreatorSaaS.Core.Interfaces;
using CreatorSaaS.Core.Exceptions;
using CreatorSaaS.Application.Services;
using CreatorSaaS.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace CreatorSaaS.Application.Commands.Auth;

// ─── Register ────────────────────────────────────────────────────────────────

public record RegisterCommand(
    string TenantName,
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<AuthResponse>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IEmailService _email;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(IUnitOfWork uow, IJwtService jwt, 
        IEmailService email, ILogger<RegisterCommandHandler> logger)
    {
        _uow = uow; _jwt = jwt; _email = email; _logger = logger;
    }

    public async Task<AuthResponse> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        // Check email uniqueness
        var existing = await _uow.Users.FirstOrDefaultAsync(u => u.Email == cmd.Email.ToLower(), ct);
        if (existing != null)
            throw new DomainException("EMAIL_TAKEN", "Email is already registered.");

        // Create tenant
        var slug = cmd.TenantName.ToLower().Replace(" ", "-").Replace(".", "");
        var tenant = new Tenant
        {
            Name = cmd.TenantName,
            Slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}",
            Plan = "starter",
            MonthlyVideoQuota = 5
        };
        await _uow.Tenants.AddAsync(tenant, ct);

        // Create owner user
        var verificationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var user = new User
        {
            TenantId = tenant.Id,
            Email = cmd.Email.ToLower(),
            PasswordHash = BCrypt.HashPassword(cmd.Password),
            FirstName = cmd.FirstName,
            LastName = cmd.LastName,
            Role = "owner",
            EmailVerificationToken = verificationToken
        };
        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        // Send verification email (non-blocking)
        _ = _email.SendVerificationEmailAsync(user.Email, user.FirstName, verificationToken, ct)
              .ContinueWith(t => _logger.LogError(t.Exception, "Email send failed"), 
                  TaskContinuationOptions.OnlyOnFaulted);

        var (token, refreshToken) = _jwt.GenerateTokens(user);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("New tenant registered: {TenantId} by {Email}", tenant.Id, user.Email);

        return new AuthResponse(token, refreshToken, MapUser(user), MapTenant(tenant));
    }

    private static UserDto MapUser(User u) => new(u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.TenantId, u.AvatarUrl);
    private static TenantDto MapTenant(Tenant t) => new(t.Id, t.Name, t.Slug, t.Plan, t.MonthlyVideoQuota, t.VideosCreatedThisMonth);
}

// ─── Login ───────────────────────────────────────────────────────────────────

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;

    public LoginCommandHandler(IUnitOfWork uow, IJwtService jwt) { _uow = uow; _jwt = jwt; }

    public async Task<AuthResponse> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await _uow.Users.FirstOrDefaultAsync(u => u.Email == cmd.Email.ToLower() && !u.IsDeleted, ct)
            ?? throw new DomainException("INVALID_CREDENTIALS", "Invalid email or password.");

        if (!BCrypt.Verify(cmd.Password, user.PasswordHash))
            throw new DomainException("INVALID_CREDENTIALS", "Invalid email or password.");

        var tenant = await _uow.Tenants.GetByIdAsync(user.TenantId, ct)
            ?? throw new NotFoundException("Tenant", user.TenantId);

        if (!tenant.IsActive)
            throw new DomainException("ACCOUNT_DISABLED", "Account has been disabled.");

        var (token, refreshToken) = _jwt.GenerateTokens(user);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        return new AuthResponse(token, refreshToken,
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.TenantId, user.AvatarUrl),
            new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Plan, tenant.MonthlyVideoQuota, tenant.VideosCreatedThisMonth));
    }
}

// ─── RefreshToken ─────────────────────────────────────────────────────────────

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;

    public RefreshTokenCommandHandler(IUnitOfWork uow, IJwtService jwt) { _uow = uow; _jwt = jwt; }

    public async Task<AuthResponse> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var user = await _uow.Users.FirstOrDefaultAsync(
            u => u.RefreshToken == cmd.RefreshToken && u.RefreshTokenExpiry > DateTime.UtcNow, ct)
            ?? throw new DomainException("INVALID_TOKEN", "Invalid or expired refresh token.");

        var tenant = await _uow.Tenants.GetByIdAsync(user.TenantId, ct)
            ?? throw new NotFoundException("Tenant", user.TenantId);

        var (token, refreshToken) = _jwt.GenerateTokens(user);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        await _uow.SaveChangesAsync(ct);

        return new AuthResponse(token, refreshToken,
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.TenantId, user.AvatarUrl),
            new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Plan, tenant.MonthlyVideoQuota, tenant.VideosCreatedThisMonth));
    }
}
