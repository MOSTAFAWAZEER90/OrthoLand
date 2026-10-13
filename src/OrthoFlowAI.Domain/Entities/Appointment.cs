using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class Appointment : TenantEntity
{
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string? BranchId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public DateTime? EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentType Type { get; set; }
    public string? Notes { get; set; }
    public int Duration { get; set; }
    public string? Color { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }

    // Reminder tracking
    public bool Reminder7DaysSent { get; set; }
    public bool Reminder3DaysSent { get; set; }
    public bool Reminder1DaySent { get; set; }
    public bool Reminder2HoursSent { get; set; }

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public Branch? Branch { get; set; }
}
