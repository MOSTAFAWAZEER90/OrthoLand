using System.ComponentModel.DataAnnotations;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class CreateDocumentRequest
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public DocumentType Type { get; set; }

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    public DateTime? ExpiryDate { get; set; }
}

public class UpdateDocumentRequest
{
    public string? FileName { get; set; }
    public DocumentType? Type { get; set; }
    public string? Description { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Tags { get; set; }
    public DocumentStatus? Status { get; set; }
}

public class SignDocumentRequest
{
    [Required]
    public string SignatureData { get; set; } = string.Empty; // Base64 encoded signature
    public string? SignatureImageUrl { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class DocumentResponse
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string UploadedById { get; set; } = string.Empty;
    public string UploadedByName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public DocumentType Type { get; set; }
    public string? Description { get; set; }
    public bool IsSigned { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignedById { get; set; }
    public string? SignedByName { get; set; }
    public string? SignatureImageUrl { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DocumentFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? PatientId { get; set; }
    public string? ClinicId { get; set; }
    public DocumentType? Type { get; set; }
    public DocumentStatus? Status { get; set; }
    public bool? IsSigned { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class BulkSignRequest
{
    public List<string> DocumentIds { get; set; } = new();
    public string SignatureData { get; set; } = string.Empty;
    public string? SignatureImageUrl { get; set; }
}