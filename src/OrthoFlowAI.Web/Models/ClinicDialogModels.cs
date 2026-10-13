namespace OrthoFlowAI.Web.Models;

public class ClinicCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? BusinessHours { get; set; }
    public string? WorkingDays { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string SubscriptionPlan { get; set; } = "Starter";
    public string SubscriptionStatus { get; set; } = "Trial";
    public string? Language { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ClinicUpdateRequest
{
    public string? Name { get; set; }
    public string? Logo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? BusinessHours { get; set; }
    public string? WorkingDays { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string SubscriptionPlan { get; set; } = "Starter";
    public string SubscriptionStatus { get; set; } = "Trial";
    public string? Language { get; set; }
    public bool IsActive { get; set; } = true;
}