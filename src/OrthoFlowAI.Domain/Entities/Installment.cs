using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

/// <summary>
/// One scheduled payment of an invoice that is paid in instalments (أقساط).
/// Orthodontic treatment usually runs 12-24 months and is paid monthly.
/// </summary>
public class Installment : TenantEntity
{
    public string InvoiceId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;

    /// <summary>1-based position in the schedule.</summary>
    public int Number { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;
    public DateTime? PaidAt { get; set; }

    /// <summary>When the "due soon" reminder was sent (null = not yet).</summary>
    public DateTime? DueSoonReminderSentAt { get; set; }
    /// <summary>When the "overdue" reminder was sent (null = not yet).</summary>
    public DateTime? OverdueReminderSentAt { get; set; }

    public decimal RemainingAmount => Amount - PaidAmount;

    public Invoice Invoice { get; set; } = null!;
}
