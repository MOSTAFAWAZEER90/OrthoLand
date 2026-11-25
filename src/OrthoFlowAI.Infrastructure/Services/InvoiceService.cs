using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Invoice>> GetByIdAsync(string id)
    {
        var invoices = await _unitOfWork.Invoices.FindAsync(
            i => i.Id == id,
            i => i.Patient!,
            i => i.Doctor!);

        var invoice = invoices.FirstOrDefault();
        if (invoice is null)
        {
            return ApiResponse<Invoice>.FailResponse("Invoice not found.");
        }

        return ApiResponse<Invoice>.SuccessResponse(invoice);
    }

    public async Task<ApiResponse<PagedResult<Invoice>>> GetAllAsync(
        int pageNumber, int pageSize, string? patientId = null, InvoiceStatus? status = null,
        PaymentStatus? paymentStatus = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        Expression<Func<Invoice, bool>>? filter = BuildFilter(patientId, status, paymentStatus, startDate, endDate);

        var query = _unitOfWork.Invoices.Query()
            .Include(i => i.Patient)
            .Include(i => i.Doctor).ThenInclude(d => d!.User)
            .AsQueryable();
        if (filter != null) query = query.Where(filter);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<Invoice> { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
        return ApiResponse<PagedResult<Invoice>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<Invoice>> CreateAsync(Invoice invoice)
    {
        var lastInvoice = await _unitOfWork.Invoices.Query()
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        var nextNumber = 1;
        if (lastInvoice?.InvoiceNumber != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length > 1 && int.TryParse(parts[^1], out var lastNum))
            {
                nextNumber = lastNum + 1;
            }
        }

        invoice.InvoiceNumber = $"INV-{nextNumber:D6}";
        invoice.CreatedAt = DateTime.UtcNow;
        invoice.Status = InvoiceStatus.Draft;
        invoice.PaymentStatus = PaymentStatus.Pending;
        invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount - invoice.Discount + invoice.Tax;

        await _unitOfWork.Invoices.AddAsync(invoice);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Invoice>.SuccessResponse(invoice, "Invoice created successfully.");
    }

    public async Task<ApiResponse<Invoice>> UpdateAsync(Invoice invoice)
    {
        var existing = await _unitOfWork.Invoices.GetByIdAsync(invoice.Id);
        if (existing is null)
        {
            return ApiResponse<Invoice>.FailResponse("Invoice not found.");
        }

        existing.PatientId = invoice.PatientId;
        existing.DoctorId = invoice.DoctorId;
        existing.TotalAmount = invoice.TotalAmount;
        existing.Discount = invoice.Discount;
        existing.Tax = invoice.Tax;
        existing.DueDate = invoice.DueDate;
        existing.Notes = invoice.Notes;
        existing.ItemsJson = invoice.ItemsJson;
        existing.RemainingAmount = invoice.TotalAmount - existing.PaidAmount - invoice.Discount + invoice.Tax;

        _unitOfWork.Invoices.Update(existing);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Invoice>.SuccessResponse(existing, "Invoice updated successfully.");
    }

    public async Task<ApiResponse> DeleteAsync(string id)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);
        if (invoice is null)
        {
            return ApiResponse.FailResponse("Invoice not found.");
        }

        invoice.IsDeleted = true;
        _unitOfWork.Invoices.Update(invoice);
        await _unitOfWork.CommitAsync();

        return ApiResponse.SuccessResponse("Invoice deleted successfully.");
    }

    public async Task<ApiResponse<Payment>> AddPaymentAsync(Payment payment)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(payment.InvoiceId);
        if (invoice is null)
        {
            return ApiResponse<Payment>.FailResponse("Invoice not found.");
        }

        if (payment.Amount <= 0)
        {
            return ApiResponse<Payment>.FailResponse("Payment amount must be greater than zero.");
        }

        var outstanding = InstallmentService.NetTotal(invoice) - invoice.PaidAmount;
        if (payment.Amount > outstanding)
        {
            return ApiResponse<Payment>.FailResponse($"Payment exceeds the remaining balance ({outstanding:0.##}).");
        }

        payment.CreatedAt = DateTime.UtcNow;
        payment.PaymentDate = DateTime.UtcNow;
        payment.ClinicId = invoice.ClinicId;
        if (string.IsNullOrEmpty(payment.PatientId)) payment.PatientId = invoice.PatientId;
        await _unitOfWork.Payments.AddAsync(payment);

        // Settle instalments oldest-first, if this invoice is paid in instalments.
        var installments = await _unitOfWork.Installments.Query()
            .Where(i => i.InvoiceId == invoice.Id)
            .OrderBy(i => i.Number)
            .ToListAsync();
        if (installments.Count > 0)
        {
            InstallmentService.AllocatePayment(installments, payment.Amount, payment.PaymentDate);
            foreach (var inst in installments) _unitOfWork.Installments.Update(inst);
            var next = installments.FirstOrDefault(i => i.Status != InstallmentStatus.Paid);
            if (next != null) invoice.DueDate = next.DueDate;
        }

        invoice.PaidAmount += payment.Amount;
        invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount - invoice.Discount + invoice.Tax;

        if (invoice.PaidAmount >= invoice.TotalAmount - invoice.Discount + invoice.Tax)
        {
            invoice.PaymentStatus = PaymentStatus.Paid;
            invoice.Status = InvoiceStatus.Paid;
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.PaymentStatus = PaymentStatus.Partial;
            invoice.Status = InvoiceStatus.Partial;
        }

        _unitOfWork.Invoices.Update(invoice);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Payment>.SuccessResponse(payment, "Payment recorded successfully.");
    }

    public async Task<ApiResponse<List<Payment>>> GetPaymentsAsync(string invoiceId)
    {
        var payments = await _unitOfWork.Payments.FindAsync(
            p => p.InvoiceId == invoiceId,
            p => p.Patient!);

        return ApiResponse<List<Payment>>.SuccessResponse(payments.OrderBy(p => p.PaymentDate).ToList());
    }

    public async Task<ApiResponse<Invoice>> UpdateStatusAsync(string id, InvoiceStatus status)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);
        if (invoice is null)
        {
            return ApiResponse<Invoice>.FailResponse("Invoice not found.");
        }

        invoice.Status = status;
        _unitOfWork.Invoices.Update(invoice);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Invoice>.SuccessResponse(invoice, $"Invoice status updated to {status}.");
    }

    public async Task<ApiResponse<decimal>> GetTotalRevenueAsync(
        string clinicId, DateTime? startDate = null, DateTime? endDate = null)
    {
        Expression<Func<Payment, bool>> filter = p =>
            p.ClinicId == clinicId && !p.IsDeleted;

        if (startDate.HasValue)
        {
            filter = p => p.ClinicId == clinicId && !p.IsDeleted && p.PaymentDate >= startDate.Value;
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1);
            filter = startDate.HasValue
                ? p => p.ClinicId == clinicId && !p.IsDeleted && p.PaymentDate >= startDate.Value && p.PaymentDate < end
                : p => p.ClinicId == clinicId && !p.IsDeleted && p.PaymentDate < end;
        }

        var payments = await _unitOfWork.Payments.FindAsync(filter);
        var total = payments.Sum(p => p.Amount);

        return ApiResponse<decimal>.SuccessResponse(total);
    }

    public async Task<ApiResponse<PagedResult<Invoice>>> GetOverdueInvoicesAsync(int pageNumber, int pageSize)
    {
        var result = await _unitOfWork.Invoices.GetPagedAsync(
            pageNumber,
            pageSize,
            i => i.Status == InvoiceStatus.Overdue || (i.DueDate < DateTime.UtcNow && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled),
            q => q.OrderBy(i => i.DueDate));

        return ApiResponse<PagedResult<Invoice>>.SuccessResponse(result);
    }

    private static Expression<Func<Invoice, bool>>? BuildFilter(
        string? patientId, InvoiceStatus? status, PaymentStatus? paymentStatus,
        DateTime? startDate, DateTime? endDate)
    {
        Expression<Func<Invoice, bool>>? filter = null;

        if (!string.IsNullOrWhiteSpace(patientId))
        {
            filter = CombineFilters(filter, i => i.PatientId == patientId);
        }

        if (status.HasValue)
        {
            filter = CombineFilters(filter, i => i.Status == status.Value);
        }

        if (paymentStatus.HasValue)
        {
            filter = CombineFilters(filter, i => i.PaymentStatus == paymentStatus.Value);
        }

        if (startDate.HasValue)
        {
            filter = CombineFilters(filter, i => i.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1);
            filter = CombineFilters(filter, i => i.CreatedAt < end);
        }

        return filter;
    }

    private static Expression<Func<Invoice, bool>>? CombineFilters(
        Expression<Func<Invoice, bool>>? first, Expression<Func<Invoice, bool>> second)
    {
        if (first is null)
        {
            return second;
        }

        var parameter = Expression.Parameter(typeof(Invoice), "i");
        var body = Expression.AndAlso(
            Expression.Invoke(first, parameter),
            Expression.Invoke(second, parameter));

        return Expression.Lambda<Func<Invoice, bool>>(body, parameter);
    }
}
