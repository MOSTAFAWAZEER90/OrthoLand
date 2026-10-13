using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class PainReport : TenantEntity
{
    public string PatientId { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    public PainLevel PainLevel { get; set; }
    public bool HasBrokenWire { get; set; }
    public bool HasLooseBracket { get; set; }
    public bool HasBleeding { get; set; }
    public bool HasSwelling { get; set; }
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsEmergency { get; set; }
    public bool DoctorNotified { get; set; }

    public Patient Patient { get; set; } = null!;
}
