using System.ComponentModel.DataAnnotations;

namespace OrthoFlowAI.Application.DTOs;

public class DashboardResponse
{
    public int TotalPatients { get; set; }
    public int ActivePatients { get; set; }
    public int TodayAppointments { get; set; }
    public int UpcomingAppointments { get; set; }
    public int MissedAppointments { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int CompletedTreatments { get; set; }
    public double TreatmentCompletionRate { get; set; }
    public double NoShowRate { get; set; }
    public double PatientSatisfaction { get; set; }
    public List<RecentActivityItem> RecentActivity { get; set; } = new();
    public List<TopDoctorItem> TopDoctors { get; set; } = new();
}

public class RecentActivityItem
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TopDoctorItem
{
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public int AppointmentsCount { get; set; }
    public int PatientsCount { get; set; }
    public decimal Revenue { get; set; }
    public double SatisfactionScore { get; set; }
}

public class DashboardFilterRequest
{
    [MaxLength(50)]
    public string? BranchId { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    [MaxLength(50)]
    public string? DoctorId { get; set; }
}
