using System.ComponentModel.DataAnnotations;
using OrthoFlowAI.Application.DTOs.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class CreateAppointmentRequest
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    public string? BranchId { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    public DateTime? EndTime { get; set; }

    [Required]
    public AppointmentType Type { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Range(5, 480)]
    public int Duration { get; set; } = 30;

    [MaxLength(7)]
    public string? Color { get; set; }

    public bool IsRecurring { get; set; }

    [MaxLength(500)]
    public string? RecurrencePattern { get; set; }

    public DateTime? RecurrenceEndDate { get; set; }
}

public class UpdateAppointmentRequest
{
    public string? PatientId { get; set; }

    public string? DoctorId { get; set; }

    public string? BranchId { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public DateTime? EndTime { get; set; }

    public AppointmentStatus? Status { get; set; }

    public AppointmentType? Type { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Range(5, 480)]
    public int? Duration { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    public bool? IsRecurring { get; set; }

    [MaxLength(500)]
    public string? RecurrencePattern { get; set; }

    public DateTime? RecurrenceEndDate { get; set; }
}

public class AppointmentResponse
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime AppointmentDate { get; set; }
    public DateTime? EndTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public AppointmentType Type { get; set; }
    public string? Notes { get; set; }
    public int Duration { get; set; }
    public string? Color { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AppointmentFilterRequest : PaginationRequest
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? DoctorId { get; set; }
    public string? PatientId { get; set; }
    public string? BranchId { get; set; }
    public AppointmentStatus? Status { get; set; }
    public AppointmentType? Type { get; set; }
}
