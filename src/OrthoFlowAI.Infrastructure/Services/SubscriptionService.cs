using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;
using OrthoFlowAI.Infrastructure.Settings;
using Stripe;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using BillingPortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;
using BillingPortalSessionService = Stripe.BillingPortal.SessionService;

namespace OrthoFlowAI.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly StripeSettings _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(IOptions<StripeSettings> settings, IUnitOfWork unitOfWork, ILogger<SubscriptionService> logger)
    {
        _settings = settings.Value;
        _unitOfWork = unitOfWork;
        _logger = logger;

        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<ApiResponse<SubscriptionCheckoutResponse>> CreateCheckoutSessionAsync(string clinicId, string priceId, string successUrl, string cancelUrl)
    {
        try
        {
            var clinic = await _unitOfWork.Clinics.GetByIdAsync(clinicId);
            if (clinic == null || clinic.IsDeleted)
            {
                return ApiResponse<SubscriptionCheckoutResponse>.FailResponse("Clinic not found");
            }

            var subscriptions = await _unitOfWork.Subscriptions.FindAsync(s => s.ClinicId == clinicId && !s.IsDeleted);
            var currentSubscription = subscriptions.FirstOrDefault();

            string customerId = currentSubscription?.StripeCustomerId ?? string.Empty;

            // Create or get Stripe customer
            if (string.IsNullOrEmpty(customerId))
            {
                var customerOptions = new CustomerCreateOptions
                {
                    Email = clinic.Email,
                    Name = clinic.Name,
                    Metadata = new Dictionary<string, string> { { "clinic_id", clinicId } }
                };
                var customerService = new CustomerService();
                var customer = await customerService.CreateAsync(customerOptions);
                customerId = customer.Id;

                if (currentSubscription != null)
                {
                    currentSubscription.StripeCustomerId = customerId;
                    currentSubscription.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Subscriptions.Update(currentSubscription);
                    await _unitOfWork.CommitAsync();
                }
            }

            // Create checkout session
            var sessionOptions = new CheckoutSessionCreateOptions
            {
                Customer = customerId,
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<CheckoutSessionLineItemOptions>
                {
                    new CheckoutSessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string> { { "clinic_id", clinicId } }
            };

            var sessionService = new CheckoutSessionService();
            var session = await sessionService.CreateAsync(sessionOptions);

            return ApiResponse<SubscriptionCheckoutResponse>.SuccessResponse(new SubscriptionCheckoutResponse
            {
                SessionId = session.Id,
                Url = session.Url
            }, "Checkout session created");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session for clinic {ClinicId}", clinicId);
            return ApiResponse<SubscriptionCheckoutResponse>.FailResponse($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for clinic {ClinicId}", clinicId);
            return ApiResponse<SubscriptionCheckoutResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<SubscriptionPortalResponse>> CreateBillingPortalSessionAsync(string clinicId, string returnUrl)
    {
        try
        {
            var subscriptions = await _unitOfWork.Subscriptions.FindAsync(s => s.ClinicId == clinicId && !s.IsDeleted);
            var currentSubscription = subscriptions.FirstOrDefault();

            if (currentSubscription == null || string.IsNullOrEmpty(currentSubscription.StripeCustomerId))
            {
                return ApiResponse<SubscriptionPortalResponse>.FailResponse("No Stripe customer found for this clinic");
            }

            var portalOptions = new BillingPortalSessionCreateOptions
            {
                Customer = currentSubscription.StripeCustomerId,
                ReturnUrl = returnUrl
            };

            var portalService = new BillingPortalSessionService();
            var session = await portalService.CreateAsync(portalOptions);

            return ApiResponse<SubscriptionPortalResponse>.SuccessResponse(new SubscriptionPortalResponse
            {
                Url = session.Url
            }, "Billing portal session created");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating billing portal for clinic {ClinicId}", clinicId);
            return ApiResponse<SubscriptionPortalResponse>.FailResponse($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating billing portal for clinic {ClinicId}", clinicId);
            return ApiResponse<SubscriptionPortalResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse> HandleWebhookAsync(string json, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, signature, _settings.WebhookSecret);

            switch (stripeEvent.Type)
            {
                case "customer.subscription.created":
                case "customer.subscription.updated":
                case "customer.subscription.deleted":
                    await HandleSubscriptionChange(stripeEvent);
                    break;

                case "invoice.payment_succeeded":
                    await HandlePaymentSucceeded(stripeEvent);
                    break;

                case "invoice.payment_failed":
                    await HandlePaymentFailed(stripeEvent);
                    break;

                case "customer.subscription.trial_will_end":
                    await HandleTrialEnding(stripeEvent);
                    break;
            }

            return ApiResponse.SuccessResponse("Webhook processed");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return ApiResponse.FailResponse($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return ApiResponse.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ClinicSubscriptionStatusResponse>> GetSubscriptionStatusAsync(string clinicId)
    {
        try
        {
            var clinic = await _unitOfWork.Clinics.GetByIdAsync(clinicId);
            if (clinic == null || clinic.IsDeleted)
            {
                return ApiResponse<ClinicSubscriptionStatusResponse>.FailResponse("Clinic not found");
            }

            var subscriptions = await _unitOfWork.Subscriptions.FindAsync(s => s.ClinicId == clinicId && !s.IsDeleted);
            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
            {
                return ApiResponse<ClinicSubscriptionStatusResponse>.FailResponse("No subscription found");
            }

            // Get current usage
            var patientCount = (await _unitOfWork.Patients.FindAsync(p => p.ClinicId == clinicId && !p.IsDeleted)).Count();
            var staffCount = (await _unitOfWork.Staff.FindAsync(s => s.ClinicId == clinicId && !s.IsDeleted)).Count();
            var branchCount = (await _unitOfWork.Branches.FindAsync(b => b.ClinicId == clinicId && !b.IsDeleted)).Count();

            var features = new List<string>();
            if (!string.IsNullOrEmpty(subscription.FeaturesJson))
            {
                try
                {
                    features = JsonSerializer.Deserialize<List<string>>(subscription.FeaturesJson) ?? new List<string>();
                }
                catch { }
            }

            var response = new ClinicSubscriptionStatusResponse
            {
                ClinicId = clinicId,
                PlanName = subscription.Plan.ToString(),
                Status = subscription.Status.ToString(),
                CurrentPeriodEnd = subscription.EndDate,
                CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                StripeCustomerId = subscription.StripeCustomerId,
                StripeSubscriptionId = subscription.StripeSubscriptionId,
                MaxPatients = subscription.MaxPatients,
                MaxStaff = subscription.MaxStaff,
                MaxBranches = subscription.MaxBranches,
                Features = features,
                CurrentUsagePatients = patientCount,
                CurrentUsageStaff = staffCount,
                CurrentUsageBranches = branchCount
            };

            return ApiResponse<ClinicSubscriptionStatusResponse>.SuccessResponse(response, "Subscription status retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription status for clinic {ClinicId}", clinicId);
            return ApiResponse<ClinicSubscriptionStatusResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<SubscriptionPlanResponse>>> GetAvailablePlansAsync()
    {
        var plans = new List<SubscriptionPlanResponse>
        {
            new SubscriptionPlanResponse
            {
                Id = "starter",
                Name = "Starter",
                Description = "Perfect for small clinics getting started",
                PriceMonthly = 99,
                PriceYearly = 990,
                MaxPatients = 300,
                MaxStaff = 2,
                MaxBranches = 1,
                Features = new List<string>
                {
                    "Up to 300 patients",
                    "Appointment reminders (Email/SMS)",
                    "Basic photo management",
                    "Patient portal",
                    "Treatment timeline",
                    "Basic analytics",
                    "2 staff accounts",
                    "1 branch"
                },
                IsPopular = false,
                StripePriceIdMonthly = "price_starter_monthly",
                StripePriceIdYearly = "price_starter_yearly"
            },
            new SubscriptionPlanResponse
            {
                Id = "professional",
                Name = "Professional",
                Description = "For growing practices with advanced needs",
                PriceMonthly = 299,
                PriceYearly = 2990,
                MaxPatients = 1000,
                MaxStaff = 10,
                MaxBranches = 5,
                Features = new List<string>
                {
                    "Unlimited patients",
                    "WhatsApp integration",
                    "AI-powered reports",
                    "Advanced analytics & insights",
                    "Inventory management",
                    "Document management with e-signatures",
                    "Referral tracking",
                    "Patient feedback/NPS",
                    "Multi-language support",
                    "10 staff accounts",
                    "5 branches"
                },
                IsPopular = true,
                StripePriceIdMonthly = "price_professional_monthly",
                StripePriceIdYearly = "price_professional_yearly"
            },
            new SubscriptionPlanResponse
            {
                Id = "enterprise",
                Name = "Enterprise",
                Description = "For large clinic chains and organizations",
                PriceMonthly = 999,
                PriceYearly = 9990,
                MaxPatients = -1,
                MaxStaff = -1,
                MaxBranches = -1,
                Features = new List<string>
                {
                    "Unlimited everything",
                    "Custom branding & white-label",
                    "API access",
                    "Dedicated support & SLA",
                    "Custom integrations",
                    "White-label mobile app",
                    "Advanced security & compliance",
                    "Custom reporting",
                    "SSO & 2FA",
                    "Unlimited staff",
                    "Unlimited branches"
                },
                IsPopular = false,
                StripePriceIdMonthly = "price_enterprise_monthly",
                StripePriceIdYearly = "price_enterprise_yearly"
            }
        };

        return ApiResponse<List<SubscriptionPlanResponse>>.SuccessResponse(plans, "Available plans retrieved");
    }

    private async Task HandleSubscriptionChange(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        var clinicId = subscription.Metadata?.TryGetValue("clinic_id", out var cid) == true ? cid : null;
        if (string.IsNullOrEmpty(clinicId)) return;

        var subscriptions = await _unitOfWork.Subscriptions.FindAsync(s => s.ClinicId == clinicId && !s.IsDeleted);
        var currentSubscription = subscriptions.FirstOrDefault();

        if (currentSubscription == null) return;

        currentSubscription.StripeSubscriptionId = subscription.Id;
        currentSubscription.Status = MapStripeStatus(subscription.Status);
        currentSubscription.StartDate = subscription.CurrentPeriodStart;
        currentSubscription.EndDate = subscription.CurrentPeriodEnd;
        currentSubscription.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;

        if (subscription.Items?.Data?.Count > 0)
        {
            var price = subscription.Items.Data[0].Price;
            if (price != null)
            {
                currentSubscription.Amount = price.UnitAmount.HasValue ? price.UnitAmount.Value / 100m : 0;
                currentSubscription.BillingPeriod = price.Recurring?.Interval == "year" ? BillingPeriod.Yearly : BillingPeriod.Monthly;
                currentSubscription.StripePriceId = price.Id;
            }
        }

        currentSubscription.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Subscriptions.Update(currentSubscription);

        // Update clinic subscription info
        var clinic = await _unitOfWork.Clinics.GetByIdAsync(clinicId);
        if (clinic != null)
        {
            clinic.SubscriptionPlan = currentSubscription.Plan.ToString();
            clinic.SubscriptionStatus = currentSubscription.Status.ToString();
            clinic.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Clinics.Update(clinic);
        }

        await _unitOfWork.CommitAsync();
        _logger.LogInformation("Subscription updated for clinic {ClinicId}: {Status}", clinicId, currentSubscription.Status);
    }

    private async Task HandlePaymentSucceeded(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null || string.IsNullOrEmpty(invoice.SubscriptionId)) return;

        var subscriptions = await _unitOfWork.Subscriptions.FindAsync(s => s.StripeSubscriptionId == invoice.SubscriptionId && !s.IsDeleted);
        var subscription = subscriptions.FirstOrDefault();

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Subscriptions.Update(subscription);
            await _unitOfWork.CommitAsync();
        }
    }

    private async Task HandlePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null || string.IsNullOrEmpty(invoice.SubscriptionId)) return;

        var subscriptions = await _unitOfWork.Subscriptions.FindAsync(s => s.StripeSubscriptionId == invoice.SubscriptionId && !s.IsDeleted);
        var subscription = subscriptions.FirstOrDefault();

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.PastDue;
            subscription.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Subscriptions.Update(subscription);
            await _unitOfWork.CommitAsync();

            // Send notification
            await CreatePaymentFailedNotification(subscription.ClinicId);
        }
    }

    private async Task HandleTrialEnding(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        var clinicId = subscription.Metadata?.TryGetValue("clinic_id", out var cid) == true ? cid : null;
        if (string.IsNullOrEmpty(clinicId)) return;

        await CreateTrialEndingNotification(clinicId);
    }

    private async Task CreatePaymentFailedNotification(string clinicId)
    {
        var notification = new Notification
        {
            ClinicId = clinicId,
            Title = "Payment Failed",
            Message = "Your subscription payment failed. Please update your payment method to avoid service interruption.",
            Type = NotificationType.PaymentDue,
            Channel = NotificationChannel.InApp,
            IsRead = false
        };
        notification.CreatedBy = "system";
        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.CommitAsync();
    }

    private async Task CreateTrialEndingNotification(string clinicId)
    {
        var notification = new Notification
        {
            ClinicId = clinicId,
            Title = "Trial Ending Soon",
            Message = "Your trial period is ending in 3 days. Subscribe now to continue using OrthoFlowAI.",
            Type = NotificationType.General,
            Channel = NotificationChannel.InApp,
            IsRead = false
        };
        notification.CreatedBy = "system";
        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.CommitAsync();
    }

    private static SubscriptionStatus MapStripeStatus(string stripeStatus) => stripeStatus?.ToLower() switch
    {
        "active" => SubscriptionStatus.Active,
        "trialing" => SubscriptionStatus.Trial,
        "past_due" => SubscriptionStatus.PastDue,
        "canceled" => SubscriptionStatus.Cancelled,
        "incomplete" => SubscriptionStatus.Pending,
        "incomplete_expired" => SubscriptionStatus.Expired,
        "paused" => SubscriptionStatus.Suspended,
        _ => SubscriptionStatus.Pending
    };
}