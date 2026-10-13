namespace OrthoFlowAI.Application.DTOs;

public class SubscriptionCheckoutResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class SubscriptionPortalResponse
{
    public string Url { get; set; } = string.Empty;
}

public class ClinicSubscriptionStatusResponse
{
    public string ClinicId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public int MaxPatients { get; set; }
    public int MaxStaff { get; set; }
    public int MaxBranches { get; set; }
    public List<string> Features { get; set; } = new();
    public decimal CurrentUsagePatients { get; set; }
    public decimal CurrentUsageStaff { get; set; }
    public decimal CurrentUsageBranches { get; set; }
}

public class SubscriptionPlanResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public int MaxPatients { get; set; }
    public int MaxStaff { get; set; }
    public int MaxBranches { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsPopular { get; set; }
    public string StripePriceIdMonthly { get; set; } = string.Empty;
    public string StripePriceIdYearly { get; set; } = string.Empty;
}