using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CreatorSaaS.Core.Entities;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace CreatorSaaS.Application.Services;

public interface IJwtService
{
    (string AccessToken, string RefreshToken) GenerateTokens(User user);
    ClaimsPrincipal? GetPrincipalFromToken(string token);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtService(IConfiguration config)
    {
        _config = config;
        var secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT:Secret not configured");
        _key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        _issuer = config["Jwt:Issuer"] ?? "CreatorSaaS";
        _audience = config["Jwt:Audience"] ?? "CreatorSaaS-Clients";
        _expiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");
    }

    public (string AccessToken, string RefreshToken) GenerateTokens(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return (accessToken, refreshToken);
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = false // For refresh token validation, we ignore expiry
            }, out SecurityToken securityToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Role, user.Role),
            new("tenant_id", user.TenantId.ToString()),
            new("email_verified", user.EmailVerified.ToString().ToLower())
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
