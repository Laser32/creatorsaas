using CreatorSaaS.Core.Enums;

namespace CreatorSaaS.Core.Entities;

public class Subscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Plan { get; set; } = "starter";
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }
    public string? StripeCustomerId { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; } = false;
    public DateTime? CanceledAt { get; set; }
    public int MonthlyVideoQuota { get; set; }
    public decimal PriceMonthly { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
