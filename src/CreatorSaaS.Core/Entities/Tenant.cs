namespace CreatorSaaS.Core.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;   // used in subdomain / URL
    public string? LogoUrl { get; set; }
    public string Plan { get; set; } = "starter";       // starter | pro | agency
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime? SubscriptionEndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int MonthlyVideoQuota { get; set; } = 5;
    public int VideosCreatedThisMonth { get; set; } = 0;
    public DateTime QuotaResetAt { get; set; } = DateTime.UtcNow.AddMonths(1);

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
