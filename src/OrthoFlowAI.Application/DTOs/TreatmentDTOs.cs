using System.ComponentModel.DataAnnotations;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class CreateTreatmentVisitRequest
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    public string? AppointmentId { get; set; }

    [Required]
    public DateTime VisitDate { get; set; }

    public bool WireChanged { get; set; }

    public bool BracketReplaced { get; set; }

    public bool RubberBandsUsed { get; set; }

    public PainLevel PainLevel { get; set; }

    [MaxLength(2000)]
    public string? DoctorNotes { get; set; }

    [MaxLength(500)]
    public string? Medication { get; set; }

    public DateTime? NextAppointmentDate { get; set; }

    [MaxLength(8000)]
    public string? PhotosJson { get; set; }

    [MaxLength(8000)]
    public string? AttachmentsJson { get; set; }
}

public class TreatmentVisitResponse
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? AppointmentId { get; set; }
    public DateTime VisitDate { get; set; }
    public bool WireChanged { get; set; }
    public bool BracketReplaced { get; set; }
    public bool RubberBandsUsed { get; set; }
    public PainLevel PainLevel { get; set; }
    public string? DoctorNotes { get; set; }
    public string? Medication { get; set; }
    public DateTime? NextAppointmentDate { get; set; }
    public string? PhotosJson { get; set; }
    public string? AttachmentsJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TreatmentTimelineResponse
{
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public TreatmentType? TreatmentType { get; set; }
    public DateTime? TreatmentStartDate { get; set; }
    public DateTime? EstimatedEndDate { get; set; }
    public int TotalVisits { get; set; }
    public List<TreatmentVisitResponse> Visits { get; set; } = new();
    public double JourneyScore { get; set; }
    public double ComplianceScore { get; set; }
}
