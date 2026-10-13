using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Infrastructure.Services;

public interface IInvoiceService
{
    Task<ApiResponse<Invoice>> GetByIdAsync(string id);
    Task<ApiResponse<PagedResult<Invoice>>> GetAllAsync(
        int pageNumber, int pageSize, string? patientId = null, InvoiceStatus? status = null,
        PaymentStatus? paymentStatus = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<ApiResponse<Invoice>> CreateAsync(Invoice invoice);
    Task<ApiResponse<Invoice>> UpdateAsync(Invoice invoice);
    Task<ApiResponse> DeleteAsync(string id);
    Task<ApiResponse<Payment>> AddPaymentAsync(Payment payment);
    Task<ApiResponse<List<Payment>>> GetPaymentsAsync(string invoiceId);
    Task<ApiResponse<Invoice>> UpdateStatusAsync(string id, InvoiceStatus status);
    Task<ApiResponse<decimal>> GetTotalRevenueAsync(string clinicId, DateTime? startDate = null, DateTime? endDate = null);
    Task<ApiResponse<PagedResult<Invoice>>> GetOverdueInvoicesAsync(int pageNumber, int pageSize);
}
