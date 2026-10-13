using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class Feedback : TenantEntity
{
    public string PatientId { get; set; } = string.Empty;
    public string? DoctorId { get; set; }
    public string? AppointmentId { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public string? Comment { get; set; }
    public FeedbackType Type { get; set; }
    public int? NPSScore { get; set; } // 0-10 for NPS
    public string? NPSComment { get; set; }
    public DateTime? VisitDate { get; set; }
    public bool IsAnonymous { get; set; }
    public string? Tags { get; set; }
    public FeedbackSentiment Sentiment { get; set; } = FeedbackSentiment.Neutral;

    public Patient Patient { get; set; } = null!;
    public Doctor? Doctor { get; set; }
}
