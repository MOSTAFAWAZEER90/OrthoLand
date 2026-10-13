using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class Payment : TenantEntity
{
    public string InvoiceId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public Invoice Invoice { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
}
