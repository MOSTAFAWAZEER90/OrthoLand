using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class Referral : TenantEntity
{
    public string ReferrerPatientId { get; set; } = string.Empty;
    public string ReferredPatientId { get; set; } = string.Empty;
    public string ReferralCode { get; set; } = string.Empty;
    public decimal ReferrerDiscount { get; set; }
    public decimal ReferredDiscount { get; set; }
    public decimal ReferrerReward { get; set; }
    public ReferralStatus Status { get; set; } = ReferralStatus.Pending;
    public DateTime ReferredAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public Patient ReferrerPatient { get; set; } = null!;
    public Patient ReferredPatient { get; set; } = null!;
}
