using System.ComponentModel.DataAnnotations;
using OrthoFlowAI.Application.DTOs.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class CreateInvoiceRequest
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    public string? DoctorId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Tax { get; set; }

    public DateTime? DueDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(8000)]
    public string? ItemsJson { get; set; }
}

public class UpdateInvoiceRequest
{
    [Range(0.01, double.MaxValue)]
    public decimal? TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? PaidAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Discount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Tax { get; set; }

    public InvoiceStatus? Status { get; set; }

    public PaymentStatus? PaymentStatus { get; set; }

    public DateTime? DueDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(8000)]
    public string? ItemsJson { get; set; }
}

public class InvoiceResponse
{
    public string Id { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public InvoiceStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
    public string? ItemsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class InvoiceFilterRequest : PaginationRequest
{
    public string? PatientId { get; set; }
    public string? DoctorId { get; set; }
    public InvoiceStatus? Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}

public class CreatePaymentRequest
{
    [Required]
    public string InvoiceId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    [MaxLength(200)]
    public string? TransactionId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class PaymentResponse
{
    public string Id { get; set; } = string.Empty;
    public string InvoiceId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
