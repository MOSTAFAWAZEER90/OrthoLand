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

    /// <summary>Display names for lists/calendars (read-only, not stored; need Patient / Doctor.User loaded).</summary>
    public string? PatientName => Patient is { } p ? $"{p.FirstName} {p.LastName}" : null;
    public string? DoctorName => Doctor?.User is { } u ? $"{u.FirstName} {u.LastName}" : null;
    public Branch? Branch { get; set; }
}
