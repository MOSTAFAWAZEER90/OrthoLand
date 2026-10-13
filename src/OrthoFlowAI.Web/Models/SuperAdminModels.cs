namespace OrthoFlowAI.Web.Models;

public class SuperAdminClinicFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public string? SubscriptionPlan { get; set; }
    public string? SubscriptionStatus { get; set; }
}

public class ClinicSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? SubscriptionPlan { get; set; }
    public string? SubscriptionStatus { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalBranches { get; set; }
    public int TotalAppointments { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class SuperAdminStatsResponse
{
    public int TotalClinics { get; set; }
    public int ActiveClinics { get; set; }
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalAppointments { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public List<SubscriptionPlanStat> SubscriptionsByPlan { get; set; } = new();
    public List<ClinicRevenueStat> TopClinicsByRevenue { get; set; } = new();
}

public class SubscriptionPlanStat
{
    public string Plan { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ClinicRevenueStat
{
    public string ClinicId { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int PatientCount { get; set; }
}

public class ClinicDetailResponse
{
    public string Id { get; set; } = string.Empty;
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
    public string? SubscriptionPlan { get; set; }
    public string? SubscriptionStatus { get; set; }
    public string Language { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public ClinicStats Stats { get; set; } = new();
}

public class ClinicStats
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalStaff { get; set; }
    public int TotalBranches { get; set; }
    public int TotalAppointments { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int ActiveSubscriptions { get; set; }
}