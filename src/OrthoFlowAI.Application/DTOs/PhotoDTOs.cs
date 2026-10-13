using System.ComponentModel.DataAnnotations;
using OrthoFlowAI.Application.DTOs.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class UploadPhotoRequest
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    public string? DoctorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FileType { get; set; }

    public long FileSize { get; set; }

    [Required]
    public PhotoCategory Category { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime? VisitDate { get; set; }

    [MaxLength(500)]
    public string? BeforePhotoUrl { get; set; }

    [MaxLength(500)]
    public string? AfterPhotoUrl { get; set; }
}

public class PhotoResponse
{
    public string Id { get; set; } = string.Empty;
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
    public DateTime CreatedAt { get; set; }
}

public class PhotoFilterRequest : PaginationRequest
{
    public string? PatientId { get; set; }
    public string? DoctorId { get; set; }
    public PhotoCategory? Category { get; set; }
    public DateTime? VisitDateFrom { get; set; }
    public DateTime? VisitDateTo { get; set; }
}

public class BeforeAfterComparisonResponse
{
    public string PhotoId { get; set; } = string.Empty;
    public string? BeforePhotoUrl { get; set; }
    public string? AfterPhotoUrl { get; set; }
    public double? ComparisonScore { get; set; }
    public PhotoCategory Category { get; set; }
    public DateTime? VisitDate { get; set; }
    public string? Description { get; set; }
}
