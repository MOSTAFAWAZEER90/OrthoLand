using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Application.Billing;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Infrastructure.Services;

public interface IInstallmentService
{
    Task<ApiResponse<InstallmentPlanResponse>> CreatePlanAsync(string invoiceId, CreateInstallmentPlanRequest request);
    Task<ApiResponse<InstallmentPlanResponse>> GetPlanAsync(string invoiceId);
}

/// <summary>Instalment plans (أقساط) for invoices.</summary>
public class InstallmentService : IInstallmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public InstallmentService(IUnitOfWork unitOfWork, TimeProvider? time = null)
    {
        _unitOfWork = unitOfWork;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Amount the patient owes in total: total - discount + tax.</summary>
    public static decimal NetTotal(Invoice invoice) => invoice.TotalAmount - invoice.Discount + invoice.Tax;

    public async Task<ApiResponse<InstallmentPlanResponse>> CreatePlanAsync(string invoiceId, CreateInstallmentPlanRequest request)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
        if (invoice is null)
            return ApiResponse<InstallmentPlanResponse>.FailResponse("Invoice not found.");

        if (invoice.Status == InvoiceStatus.Cancelled)
            return ApiResponse<InstallmentPlanResponse>.FailResponse("Cannot schedule instalments for a cancelled invoice.");

        var remaining = NetTotal(invoice) - invoice.PaidAmount;
        if (remaining <= 0)
            return ApiResponse<InstallmentPlanResponse>.FailResponse("Invoice is already fully paid.");

        var existing = await _unitOfWork.Installments.Query()
            .Where(i => i.InvoiceId == invoiceId)
            .ToListAsync();

        // Re-planning only replaces instalments that have not received any money.
        if (existing.Any(i => i.PaidAmount > 0 && i.Status != InstallmentStatus.Paid))
            return ApiResponse<InstallmentPlanResponse>.FailResponse(
                "An instalment is partially paid. Record the rest of that payment before changing the plan.");

        foreach (var old in existing.Where(i => i.Status != InstallmentStatus.Paid))
            _unitOfWork.Installments.Delete(old);

        var firstNumber = existing.Count(i => i.Status == InstallmentStatus.Paid) + 1;
        var firstDue = request.FirstDueDate?.Date ?? _time.GetUtcNow().UtcDateTime.Date.AddMonths(1);
        var planned = InstallmentPlanner.Plan(remaining, request.Count, firstDue, request.IntervalMonths);

        foreach (var p in planned)
        {
            await _unitOfWork.Installments.AddAsync(new Installment
            {
                InvoiceId = invoice.Id,
                PatientId = invoice.PatientId,
                ClinicId = invoice.ClinicId,
                Number = firstNumber + p.Number - 1,
                DueDate = DateTime.SpecifyKind(p.DueDate, DateTimeKind.Utc),
                Amount = p.Amount,
            });
        }

        // The invoice's own due date becomes the next instalment's due date.
        invoice.DueDate = DateTime.SpecifyKind(planned[0].DueDate, DateTimeKind.Utc);
        _unitOfWork.Invoices.Update(invoice);
        await _unitOfWork.CommitAsync();

        return await GetPlanAsync(invoiceId);
    }

    public async Task<ApiResponse<InstallmentPlanResponse>> GetPlanAsync(string invoiceId)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
        if (invoice is null)
            return ApiResponse<InstallmentPlanResponse>.FailResponse("Invoice not found.");

        var items = await _unitOfWork.Installments.Query()
            .Where(i => i.InvoiceId == invoiceId)
            .OrderBy(i => i.Number)
            .ToListAsync();

        var today = _time.GetUtcNow().UtcDateTime.Date;
        if (InstallmentPlanner.MarkOverdue(items, today) > 0)
        {
            foreach (var i in items) _unitOfWork.Installments.Update(i);
            await _unitOfWork.CommitAsync();
        }

        return ApiResponse<InstallmentPlanResponse>.SuccessResponse(ToResponse(invoice, items));
    }

    /// <summary>Called when a payment is recorded on an invoice that has a plan.</summary>
    public static void AllocatePayment(IList<Installment> installments, decimal amount, DateTime paidAt)
    {
        if (installments.Count == 0) return;
        InstallmentPlanner.ApplyPayment(installments, amount, paidAt);
    }

    internal static InstallmentPlanResponse ToResponse(Invoice invoice, IReadOnlyList<Installment> items)
    {
        var mapped = items.Select(i => new InstallmentResponse
        {
            Id = i.Id,
            Number = i.Number,
            DueDate = i.DueDate,
            Amount = i.Amount,
            PaidAmount = i.PaidAmount,
            RemainingAmount = i.Amount - i.PaidAmount,
            Status = i.Status,
            PaidAt = i.PaidAt,
        }).ToList();

        return new InstallmentPlanResponse
        {
            InvoiceId = invoice.Id,
            InvoiceTotal = NetTotal(invoice),
            PaidAmount = invoice.PaidAmount,
            RemainingAmount = NetTotal(invoice) - invoice.PaidAmount,
            PaidCount = mapped.Count(i => i.Status == InstallmentStatus.Paid),
            OverdueCount = mapped.Count(i => i.Status == InstallmentStatus.Overdue),
            NextDue = mapped.FirstOrDefault(i => i.Status != InstallmentStatus.Paid),
            Installments = mapped,
        };
    }
}
