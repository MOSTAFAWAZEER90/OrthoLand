using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class Photo : TenantEntity
{
    public string PatientId { get; set; } = string.Empty;
    public string? DoctorId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public PhotoCategory Category { get; set; }
    public string? Description { get; set; }
    public DateTime? VisitDate { get; set; }
    public string? BeforePhotoUrl { get; set; }
    public string? AfterPhotoUrl { get; set; }
    public double? ComparisonScore { get; set; }

    public Patient Patient { get; set; } = null!;
    public Doctor? Doctor { get; set; }
}
