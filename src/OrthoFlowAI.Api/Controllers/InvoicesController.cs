using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Infrastructure.Services;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Invoice>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] InvoiceFilterRequest filter)
    {
        _logger.LogInformation("Getting invoices - Page: {PageNumber}, Size: {PageSize}", filter.PageNumber, filter.PageSize);
        var result = await _invoiceService.GetAllAsync(
            filter.PageNumber, filter.PageSize, filter.PatientId, filter.Status,
            filter.PaymentStatus, filter.DueDateFrom, filter.DueDateTo);
        return Ok(result);
    }

    [HttpPost("filter")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Invoice>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Filter([FromBody] InvoiceFilterRequest filter)
    {
        _logger.LogInformation("Filtering invoices - Page: {PageNumber}, Size: {PageSize}", filter.PageNumber, filter.PageSize);
        var result = await _invoiceService.GetAllAsync(
            filter.PageNumber, filter.PageSize, filter.PatientId, filter.Status,
            filter.PaymentStatus, filter.DueDateFrom, filter.DueDateTo);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Invoice>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogInformation("Getting invoice: {InvoiceId}", id);
        var result = await _invoiceService.GetByIdAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Invoice>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request)
    {
        _logger.LogInformation("Creating invoice for patient {PatientId}", request.PatientId);
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;

        var invoice = new Invoice
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            TotalAmount = request.TotalAmount,
            Discount = request.Discount,
            Tax = request.Tax,
            DueDate = request.DueDate,
            Notes = request.Notes,
            ItemsJson = request.ItemsJson,
            ClinicId = clinicId,
            Status = InvoiceStatus.Draft,
            PaymentStatus = PaymentStatus.Pending
        };

        invoice.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _invoiceService.CreateAsync(invoice);
        if (!result.Success) return BadRequest(result);

        _logger.LogInformation("Invoice created: {Id}", result.Data!.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Invoice>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateInvoiceRequest request)
    {
        _logger.LogInformation("Updating invoice: {InvoiceId}", id);
        var existing = await _invoiceService.GetByIdAsync(id);
        if (!existing.Success || existing.Data == null) return NotFound(existing);

        var invoice = existing.Data;
        if (request.TotalAmount.HasValue) invoice.TotalAmount = request.TotalAmount.Value;
        if (request.PaidAmount.HasValue) invoice.PaidAmount = request.PaidAmount.Value;
        if (request.Discount.HasValue) invoice.Discount = request.Discount.Value;
        if (request.Tax.HasValue) invoice.Tax = request.Tax.Value;
        if (request.Status.HasValue) invoice.Status = request.Status.Value;
        if (request.PaymentStatus.HasValue) invoice.PaymentStatus = request.PaymentStatus.Value;
        if (request.DueDate.HasValue) invoice.DueDate = request.DueDate;
        if (request.Notes != null) invoice.Notes = request.Notes;
        if (request.ItemsJson != null) invoice.ItemsJson = request.ItemsJson;

        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _invoiceService.UpdateAsync(invoice);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Deleting invoice: {InvoiceId}", id);
        var result = await _invoiceService.DeleteAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost("{id}/payments")]
    [ProducesResponseType(typeof(ApiResponse<Payment>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddPayment(string id, [FromBody] CreatePaymentRequest request)
    {
        _logger.LogInformation("Adding payment to invoice {InvoiceId}: {Amount}", id, request.Amount);

        var payment = new Payment
        {
            InvoiceId = id,
            PatientId = request.PatientId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            TransactionId = request.TransactionId,
            Notes = request.Notes,
            PaymentDate = DateTime.UtcNow
        };

        payment.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _invoiceService.AddPaymentAsync(payment);
        if (!result.Success) return BadRequest(result);

        _logger.LogInformation("Payment added: {PaymentId}", result.Data!.Id);
        return CreatedAtAction(nameof(GetById), new { id }, result);
    }

    [HttpGet("overdue")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Invoice>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting overdue invoices");
        var result = await _invoiceService.GetOverdueInvoicesAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("revenue-report")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        _logger.LogInformation("Getting revenue report for clinic {ClinicId}", clinicId);
        var result = await _invoiceService.GetTotalRevenueAsync(clinicId, startDate, endDate);
        return Ok(result);
    }
}
