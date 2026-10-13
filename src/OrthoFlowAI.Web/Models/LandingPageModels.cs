namespace OrthoFlowAI.Web.Models;

public record FeatureItem(string Icon, string Title, string Description, string Color);

public record PricingPlan(
    string Name,
    string Description,
    decimal PriceMonthly,
    decimal PriceYearly,
    int MaxPatients,
    int MaxStaff,
    int MaxBranches,
    string[] Features,
    bool IsPopular,
    string StripePriceIdMonthly,
    string StripePriceIdYearly
);