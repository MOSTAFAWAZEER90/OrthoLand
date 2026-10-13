using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class TreatmentVisit : TenantEntity
{
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
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

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public Appointment? Appointment { get; set; }
}
