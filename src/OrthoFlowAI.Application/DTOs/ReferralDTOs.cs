using System.ComponentModel.DataAnnotations;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class CreateReferralRequest
{
    [Required]
    public string ReferrerPatientId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string ReferredEmail { get; set; } = string.Empty;

    [Required]
    public string ReferredFirstName { get; set; } = string.Empty;

    [Required]
    public string ReferredLastName { get; set; } = string.Empty;

    public string? ReferredPhone { get; set; }

    public decimal ReferrerDiscount { get; set; } = 100;
    public decimal ReferredDiscount { get; set; } = 100;
    public decimal ReferrerReward { get; set; } = 50;
    public string? Notes { get; set; }
}

public class ReferralResponse
{
    public string Id { get; set; } = string.Empty;
    public string ReferrerPatientId { get; set; } = string.Empty;
    public string ReferrerName { get; set; } = string.Empty;
    public string ReferredPatientId { get; set; } = string.Empty;
    public string ReferredName { get; set; } = string.Empty;
    public string ReferralCode { get; set; } = string.Empty;
    public decimal ReferrerDiscount { get; set; }
    public decimal ReferredDiscount { get; set; }
    public decimal ReferrerReward { get; set; }
    public ReferralStatus Status { get; set; }
    public DateTime ReferredAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

public class ReferralStatsResponse
{
    public int TotalReferrals { get; set; }
    public int CompletedReferrals { get; set; }
    public int PendingReferrals { get; set; }
    public decimal TotalRewardsEarned { get; set; }
    public decimal TotalDiscountsGiven { get; set; }
    public List<ReferralResponse> RecentReferrals { get; set; } = new();
}

public class ReferralProgramSettingsResponse
{
    public decimal DefaultReferrerDiscount { get; set; } = 100;
    public decimal DefaultReferredDiscount { get; set; } = 100;
    public decimal DefaultReferrerReward { get; set; } = 50;
    public int CodeExpiryDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}