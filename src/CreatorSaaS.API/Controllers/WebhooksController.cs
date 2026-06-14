using Microsoft.AspNetCore.Mvc;
using Stripe;
using CreatorSaaS.Core.Interfaces;
using CreatorSaaS.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreatorSaaS.API.Controllers;

[ApiController]
[Route("api/webhooks")]
[IgnoreAntiforgeryToken]
public class WebhooksController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IUnitOfWork uow, IConfiguration config, ILogger<WebhooksController> logger)
    {
        _uow = uow;
        _config = config;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var sig = Request.Headers["Stripe-Signature"];
        var webhookSecret = _config["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, sig, webhookSecret);

            switch (stripeEvent.Type)
            {
                case Events.CustomerSubscriptionCreated:
                case Events.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdated(stripeEvent.Data.Object as Subscription);
                    break;

                case Events.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeleted(stripeEvent.Data.Object as Subscription);
                    break;

                case Events.InvoicePaid:
                    await HandleInvoicePaid(stripeEvent.Data.Object as Invoice);
                    break;

                case Events.PaymentIntentSucceeded:
                    _logger.LogInformation("Payment succeeded: {PaymentIntentId}", 
                        (stripeEvent.Data.Object as PaymentIntent)?.Id);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error: {Message}", ex.Message);
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing error");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task HandleSubscriptionUpdated(Subscription subscription)
    {
        if (string.IsNullOrEmpty(subscription.CustomerId))
            return;

        var tenantSub = await _uow.Subscriptions.FirstOrDefaultAsync(
            s => s.StripeSubscriptionId == subscription.Id
        );

        if (tenantSub == null)
        {
            // New subscription
            var tenant = await _uow.Tenants.FirstOrDefaultAsync(
                t => t.StripeCustomerId == subscription.CustomerId
            );

            if (tenant != null)
            {
                tenantSub = new Subscription
                {
                    TenantId = tenant.Id,
                    StripeSubscriptionId = subscription.Id,
                    StripeCustomerId = subscription.CustomerId,
                    Plan = ExtractPlanFromSubscription(subscription),
                    Status = (CreatorSaaS.Core.Enums.SubscriptionStatus)(int)subscription.Status
                };
                await _uow.Subscriptions.AddAsync(tenantSub);
            }
        }
        else
        {
            tenantSub.Status = (CreatorSaaS.Core.Enums.SubscriptionStatus)(int)subscription.Status;
            tenantSub.CurrentPeriodStart = subscription.CurrentPeriodStart.ToDateTime();
            tenantSub.CurrentPeriodEnd = subscription.CurrentPeriodEnd.ToDateTime();
            await _uow.Subscriptions.UpdateAsync(tenantSub);
        }

        await _uow.SaveChangesAsync();
        _logger.LogInformation("Subscription updated: {SubscriptionId}", subscription.Id);
    }

    private async Task HandleSubscriptionDeleted(Subscription subscription)
    {
        var tenantSub = await _uow.Subscriptions.FirstOrDefaultAsync(
            s => s.StripeSubscriptionId == subscription.Id
        );

        if (tenantSub != null)
        {
            tenantSub.Status = CreatorSaaS.Core.Enums.SubscriptionStatus.Canceled;
            tenantSub.CanceledAt = DateTime.UtcNow;
            await _uow.Subscriptions.UpdateAsync(tenantSub);
            await _uow.SaveChangesAsync();
        }

        _logger.LogInformation("Subscription deleted: {SubscriptionId}", subscription.Id);
    }

    private async Task HandleInvoicePaid(Invoice invoice)
    {
        if (string.IsNullOrEmpty(invoice.CustomerId))
            return;

        var tenant = await _uow.Tenants.FirstOrDefaultAsync(
            t => t.StripeCustomerId == invoice.CustomerId
        );

        if (tenant != null)
        {
            // Reset monthly video quota
            tenant.VideosCreatedThisMonth = 0;
            tenant.QuotaResetAt = DateTime.UtcNow.AddMonths(1);
            await _uow.Tenants.UpdateAsync(tenant);
            await _uow.SaveChangesAsync();
        }

        _logger.LogInformation("Invoice paid: {InvoiceId}", invoice.Id);
    }

    private string ExtractPlanFromSubscription(Subscription subscription)
    {
        if (subscription.Items?.Data?.Count > 0)
        {
            var price = subscription.Items.Data[0].Price;
            if (price?.Metadata?.ContainsKey("plan") == true)
                return price.Metadata["plan"];
        }
        return "starter";
    }
}
