namespace OrthoFlowAI.Application.DTOs;

public class TreatmentReportRequest
{
    public string PatientName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string TreatmentType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public List<TreatmentVisitData> Visits { get; set; } = new();
    public string? AdditionalNotes { get; set; }
}

public class TreatmentVisitData
{
    public DateTime VisitDate { get; set; }
    public string? DoctorNotes { get; set; }
    public bool WireChanged { get; set; }
    public bool BracketReplaced { get; set; }
    public bool RubberBandsUsed { get; set; }
    public string? PainLevel { get; set; }
    public string? Medication { get; set; }
}

public class PainReportData
{
    public DateTime ReportDate { get; set; }
    public string PainLevel { get; set; } = string.Empty;
    public bool HasBrokenWire { get; set; }
    public bool HasLooseBracket { get; set; }
    public bool HasBleeding { get; set; }
    public bool HasSwelling { get; set; }
    public string? Description { get; set; }
}

public class ClinicalNotesRequest
{
    public string PatientName { get; set; } = string.Empty;
    public string AppointmentType { get; set; } = string.Empty;
    public string? Observations { get; set; }
    public string? ProceduresPerformed { get; set; }
    public string? NextSteps { get; set; }
}

public class TreatmentSummaryRequest
{
    public string PatientId { get; set; } = string.Empty;
    public List<TreatmentVisitData> Visits { get; set; } = new();
}

public class RecommendationsRequest
{
    public string PatientProfile { get; set; } = string.Empty;
}
