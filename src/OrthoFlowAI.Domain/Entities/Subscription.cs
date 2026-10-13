using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class Subscription : BaseEntity
{
    public string ClinicId { get; set; } = string.Empty;
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public BillingPeriod BillingPeriod { get; set; }
    public decimal Amount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public string? CouponsJson { get; set; }
    public int MaxPatients { get; set; }
    public int MaxStaff { get; set; }
    public int MaxBranches { get; set; }
    public string? FeaturesJson { get; set; }

    // Stripe integration fields
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }
    public bool CancelAtPeriodEnd { get; set; }

    public Clinic Clinic { get; set; } = null!;
}
