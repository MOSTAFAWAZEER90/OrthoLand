using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class Document : TenantEntity
{
    public string PatientId { get; set; } = string.Empty;
    public string UploadedById { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public DocumentType Type { get; set; }
    public string? Description { get; set; }
    public bool IsSigned { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignedById { get; set; }
    public string? SignatureData { get; set; }
    public string? SignatureImageUrl { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public DateTime? ExpiryDate { get; set; }
    public string? Tags { get; set; }

    public Patient Patient { get; set; } = null!;
}
