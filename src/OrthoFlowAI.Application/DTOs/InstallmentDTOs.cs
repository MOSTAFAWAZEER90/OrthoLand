using System.ComponentModel.DataAnnotations;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class CreateInstallmentPlanRequest
{
    /// <summary>Number of monthly payments (e.g. 12 or 24 for orthodontic treatment).</summary>
    [Range(1, 60)]
    public int Count { get; set; } = 12;

    /// <summary>Due date of the first instalment. Defaults to one month from today.</summary>
    public DateTime? FirstDueDate { get; set; }

    [Range(1, 12)]
    public int IntervalMonths { get; set; } = 1;
}

public class InstallmentResponse
{
    public string Id { get; set; } = string.Empty;
    public int Number { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InstallmentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class InstallmentPlanResponse
{
    public string InvoiceId { get; set; } = string.Empty;
    public decimal InvoiceTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int PaidCount { get; set; }
    public int OverdueCount { get; set; }
    public InstallmentResponse? NextDue { get; set; }
    public List<InstallmentResponse> Installments { get; set; } = new();
}
