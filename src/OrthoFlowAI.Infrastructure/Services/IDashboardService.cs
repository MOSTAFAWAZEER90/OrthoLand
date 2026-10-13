using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Infrastructure.Services;

public interface IDashboardService
{
    Task<ApiResponse<DashboardStats>> GetStatsAsync(string clinicId, DateTime? startDate = null, DateTime? endDate = null);
    Task<ApiResponse<RevenueChart>> GetRevenueChartAsync(string clinicId, int months = 12);
    Task<ApiResponse<AppointmentChart>> GetAppointmentChartAsync(string clinicId, int months = 12);
    Task<ApiResponse<List<TopDoctor>>> GetTopDoctorsAsync(string clinicId, int count = 5);
    Task<ApiResponse<List<RecentActivity>>> GetRecentActivityAsync(string clinicId, int count = 20);
}

public class DashboardStats
{
    public int TotalPatients { get; set; }
    public int ActivePatients { get; set; }
    public int NewPatientsThisMonth { get; set; }
    public int TotalDoctors { get; set; }
    public int TodayAppointments { get; set; }
    public int WeekAppointments { get; set; }
    public int MonthAppointments { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal PendingPayments { get; set; }
    public int TotalInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public int PendingNotifications { get; set; }
    public int ActiveEmergencies { get; set; }
}

public class RevenueChart
{
    public List<ChartPoint> DataPoints { get; set; } = new();
}

public class AppointmentChart
{
    public List<ChartPoint> DataPoints { get; set; } = new();
}

public class ChartPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class TopDoctor
{
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
    public decimal Revenue { get; set; }
}

public class RecentActivity
{
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
