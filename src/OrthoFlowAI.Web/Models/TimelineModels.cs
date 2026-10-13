namespace OrthoFlowAI.Web.Models;

public abstract class TimelineItem
{
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class TreatmentVisitItem : TimelineItem
{
    public string? Id { get; set; }
    public bool WireChanged { get; set; }
    public bool BracketReplaced { get; set; }
    public bool RubberBandsUsed { get; set; }
    public string? PainLevel { get; set; }
    public string? DoctorNotes { get; set; }
    public string? Medication { get; set; }
    public DateTime? NextAppointmentDate { get; set; }
}

public class AppointmentItem : TimelineItem
{
    public string Status { get; set; } = string.Empty;
    public string AppointmentType { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string? Notes { get; set; }
}