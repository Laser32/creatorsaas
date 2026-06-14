using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreatorSaaS.Application.DTOs;
using CreatorSaaS.Core.Interfaces;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace CreatorSaaS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly ILogger<BillingController> _logger;

    public BillingController(IUnitOfWork uow, IConfiguration config, ILogger<BillingController> logger)
    {
        _uow = uow;
        _config = config;
        _logger = logger;

        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());
    private string UserEmail => User.FindFirst(ClaimTypes.Email)?.Value ?? "";

    [HttpGet("subscription")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(CancellationToken ct)
    {
        var subscription = await _uow.Subscriptions.FirstOrDefaultAsync(
            s => s.TenantId == TenantId, ct);

        if (subscription == null)
        {
            // Return default starter plan info
            return Ok(new SubscriptionDto(
                Guid.Empty, "starter", 1, null, null, false, 5, 19.99m
            ));
        }

        return Ok(new SubscriptionDto(
            subscription.Id,
            subscription.Plan,
            (int)subscription.Status,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd,
            subscription.MonthlyVideoQuota,
            subscription.PriceMonthly
        ));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutSessionDto>> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionDto dto, CancellationToken ct)
    {
        try
        {
            var tenant = await _uow.Tenants.GetByIdAsync(TenantId, ct);
            if (tenant == null) return NotFound();

            // Get or create Stripe customer
            string customerId = tenant.StripeCustomerId ?? await CreateStripeCustomerAsync(tenant.Id.ToString(), UserEmail);

            if (tenant.StripeCustomerId == null)
            {
                tenant.StripeCustomerId = customerId;
                await _uow.Tenants.UpdateAsync(tenant, ct);
                await _uow.SaveChangesAsync(ct);
            }

            var baseUrl = _config["App:BaseUrl"] ?? "http://localhost:3000";

            var options = new SessionCreateOptions
            {
                Customer = customerId,
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = dto.PriceId,
                        Quantity = 1,
                    }
                },
                Mode = "subscription",
                SuccessUrl = $"{baseUrl}/billing?success=true",
                CancelUrl = $"{baseUrl}/billing?canceled=true",
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "tenant_id", TenantId.ToString() }
                    }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Checkout session created: {SessionId} for tenant {TenantId}",
                session.Id, TenantId);

            return Ok(new CheckoutSessionDto(session.Id, session.Url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout session creation failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("portal")]
    public async Task<ActionResult<BillingPortalUrlDto>> GetBillingPortal(CancellationToken ct)
    {
        try
        {
            var tenant = await _uow.Tenants.GetByIdAsync(TenantId, ct);
            if (tenant?.StripeCustomerId == null)
                return BadRequest(new { error = "No billing account found" });

            var baseUrl = _config["App:BaseUrl"] ?? "http://localhost:3000";

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = tenant.StripeCustomerId,
                ReturnUrl = $"{baseUrl}/billing",
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            return Ok(new BillingPortalUrlDto(session.Url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Billing portal creation failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public ActionResult<object> GetPlans()
    {
        return Ok(new[]
        {
            new {
                id = "starter",
                name = "Starter",
                price = 19.99,
                videosPerMonth = 5,
                priceId = _config["Stripe:PriceStarter"],
                features = new[] { "5 videos/month", "Standard AI", "YouTube upload", "Basic analytics" }
            },
            new {
                id = "pro",
                name = "Pro",
                price = 49.99,
                videosPerMonth = 25,
                priceId = _config["Stripe:PricePro"],
                popular = true,
                features = new[] { "25 videos/month", "Advanced GPT-4", "HD B-Roll", "Advanced analytics", "A/B testing" }
            },
            new {
                id = "agency",
                name = "Agency",
                price = 149.99,
                videosPerMonth = -1,
                priceId = _config["Stripe:PriceAgency"],
                features = new[] { "Unlimited videos", "Claude/GPT-4", "4K B-Roll", "Full analytics", "Team members", "API access" }
            },
        });
    }

    private async Task<string> CreateStripeCustomerAsync(string tenantId, string email)
    {
        var options = new CustomerCreateOptions
        {
            Email = email,
            Metadata = new Dictionary<string, string>
            {
                { "tenant_id", tenantId }
            }
        };
        var service = new CustomerService();
        var customer = await service.CreateAsync(options);
        return customer.Id;
    }
}
