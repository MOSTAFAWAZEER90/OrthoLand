using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class Invoice : TenantEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? DoctorId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
    public string? ItemsJson { get; set; }

    public Patient Patient { get; set; } = null!;
    public Doctor? Doctor { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
