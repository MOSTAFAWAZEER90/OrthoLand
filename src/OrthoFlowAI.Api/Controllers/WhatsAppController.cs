using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrthoFlowAI.Application.Messaging;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Infrastructure.Data;
using OrthoFlowAI.Infrastructure.Services;
using OrthoFlowAI.Infrastructure.WhatsApp;

namespace OrthoFlowAI.Api.Controllers;

public class WhatsAppMessageResponse
{
    public string PatientName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Message { get; set; } = string.Empty;
    /// <summary>https://wa.me/... click-to-chat link (null if the phone number is invalid).</summary>
    public string? ClickToChatUrl { get; set; }
    /// <summary>True when a WhatsApp provider is configured, so POST .../send can deliver it automatically.</summary>
    public bool CanSendAutomatically { get; set; }
}

/// <summary>
/// WhatsApp messages for appointments and invoices. Every message is available as a
/// wa.me link (works with the clinic's normal WhatsApp, no API account needed) and can
/// be sent automatically when WhatsApp:Provider is configured.
/// </summary>
[ApiController]
[Route("api/whatsapp")]
[Authorize]
public class WhatsAppController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWhatsAppSender _sender;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(ApplicationDbContext db, IWhatsAppSender sender, IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppController> logger)
    {
        _db = db;
        _sender = sender;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpGet("appointments/{id}")]
    public async Task<IActionResult> AppointmentMessage(string id)
    {
        var built = await BuildAppointmentAsync(id);
        return built is null ? NotFound(ApiResponse.FailResponse("Appointment not found")) : Ok(Wrap(built.Value));
    }

    [HttpPost("appointments/{id}/send")]
    public async Task<IActionResult> SendAppointment(string id)
    {
        var built = await BuildAppointmentAsync(id);
        return built is null ? NotFound(ApiResponse.FailResponse("Appointment not found")) : await SendAsync(built.Value);
    }

    [HttpGet("invoices/{id}")]
    public async Task<IActionResult> InvoiceMessage(string id)
    {
        var built = await BuildInvoiceAsync(id);
        return built is null ? NotFound(ApiResponse.FailResponse("Invoice not found or nothing is due")) : Ok(Wrap(built.Value));
    }

    [HttpPost("invoices/{id}/send")]
    public async Task<IActionResult> SendInvoice(string id)
    {
        var built = await BuildInvoiceAsync(id);
        return built is null ? NotFound(ApiResponse.FailResponse("Invoice not found or nothing is due")) : await SendAsync(built.Value);
    }

    private async Task<(Patient Patient, PatientMessage Message)?> BuildAppointmentAsync(string id)
    {
        // Tenant query filter guarantees the appointment belongs to the caller's clinic.
        var appt = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appt?.Patient is null) return null;

        var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == appt.ClinicId);
        var doctor = appt.Doctor?.User is { } u ? $"{u.FirstName} {u.LastName}" : null;
        var msg = PatientMessages.AppointmentReminder(appt.Patient.FirstName, clinic?.Name ?? "", doctor,
            appt.AppointmentDate, clinic?.Language);
        return (appt.Patient, msg);
    }

    private async Task<(Patient Patient, PatientMessage Message)?> BuildInvoiceAsync(string id)
    {
        var invoice = await _db.Invoices.Include(i => i.Patient).FirstOrDefaultAsync(i => i.Id == id);
        if (invoice?.Patient is null) return null;

        var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == invoice.ClinicId);
        var next = await _db.Installments
            .Where(i => i.InvoiceId == id && i.Status != InstallmentStatus.Paid)
            .OrderBy(i => i.Number)
            .FirstOrDefaultAsync();

        var today = DateTime.UtcNow.Date;
        PatientMessage msg;
        if (next != null)
        {
            msg = next.DueDate.Date < today
                ? PatientMessages.InstallmentOverdue(invoice.Patient.FirstName, clinic?.Name ?? "", next.Number, next.Amount - next.PaidAmount, next.DueDate, clinic?.Language)
                : PatientMessages.InstallmentDueSoon(invoice.Patient.FirstName, clinic?.Name ?? "", next.Number, next.Amount - next.PaidAmount, next.DueDate, clinic?.Language);
        }
        else
        {
            var remaining = InstallmentService.NetTotal(invoice) - invoice.PaidAmount;
            if (remaining <= 0) return null;
            var due = invoice.DueDate ?? today;
            msg = due.Date < today
                ? PatientMessages.InstallmentOverdue(invoice.Patient.FirstName, clinic?.Name ?? "", 1, remaining, due, clinic?.Language)
                : PatientMessages.InstallmentDueSoon(invoice.Patient.FirstName, clinic?.Name ?? "", 1, remaining, due, clinic?.Language);
        }
        return (invoice.Patient, msg);
    }

    private ApiResponse<WhatsAppMessageResponse> Wrap((Patient Patient, PatientMessage Message) built) =>
        ApiResponse<WhatsAppMessageResponse>.SuccessResponse(new WhatsAppMessageResponse
        {
            PatientName = $"{built.Patient.FirstName} {built.Patient.LastName}",
            Phone = PhoneNumbers.ToE164(built.Patient.Phone, _settings.DefaultCountryCode),
            Message = built.Message.Text,
            ClickToChatUrl = WhatsAppLinks.ClickToChat(built.Patient.Phone, built.Message.Text, _settings.DefaultCountryCode),
            CanSendAutomatically = _sender.IsConfigured,
        });

    private async Task<IActionResult> SendAsync((Patient Patient, PatientMessage Message) built)
    {
        if (!_sender.IsConfigured)
            return BadRequest(ApiResponse.FailResponse("WhatsApp sending is not configured. Use the click-to-chat link instead."));

        var to = PhoneNumbers.ToInternationalDigits(built.Patient.Phone, _settings.DefaultCountryCode);
        if (to is null)
            return BadRequest(ApiResponse.FailResponse("The patient has no valid mobile number."));

        var result = await _sender.SendAsync(to, built.Message, HttpContext.RequestAborted);
        _logger.LogInformation("Manual WhatsApp {Kind} to patient {PatientId}: {Success}", built.Message.Kind, built.Patient.Id, result.Success);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
