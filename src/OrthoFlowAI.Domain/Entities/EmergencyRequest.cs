using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class EmergencyRequest : TenantEntity
{
    public string PatientId { get; set; } = string.Empty;
    public string? DoctorId { get; set; }
    public string Description { get; set; } = string.Empty;
    public EmergencySeverity Severity { get; set; }
    public string? PhotoUrlsJson { get; set; }
    public string? VideoUrl { get; set; }
    public string? VoiceNoteUrl { get; set; }
    public string? Status { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? DoctorNotes { get; set; }

    public Patient Patient { get; set; } = null!;
    public Doctor? Doctor { get; set; }
}
