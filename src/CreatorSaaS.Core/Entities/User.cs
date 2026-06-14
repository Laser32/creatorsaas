namespace CreatorSaaS.Core.Entities;

public class User : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "member";        // owner | admin | member
    public string? AvatarUrl { get; set; }
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<VideoJob> VideoJobs { get; set; } = new List<VideoJob>();
}
