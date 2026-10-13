using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/portal")]
[Authorize]
public class PatientPortalController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientPortalController> _logger;

    public PatientPortalController(IUnitOfWork unitOfWork, ILogger<PatientPortalController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private string CurrentClinicId =>
        HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;

    private async Task<Patient?> GetMyPatientAsync()
    {
        if (string.IsNullOrEmpty(CurrentUserId)) return null;

        var patients = await _unitOfWork.Patients.FindAsync(p => p.UserId == CurrentUserId);
        return patients.FirstOrDefault(p => !p.IsDeleted);
    }

    /// <summary>
    /// Get the portal overview for the current patient
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<PatientPortalSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe()
    {
        var patient = await GetMyPatientAsync();
        if (patient == null)
        {
            return NotFound(ApiResponse.FailResponse("Patient profile not linked to this account."));
        }

        var appointments = await _unitOfWork.Appointments.FindAsync(a => a.PatientId == patient.Id);
        var invoices = await _unitOfWork.Invoices.FindAsync(i => i.PatientId == patient.Id);
        var payments = await _unitOfWork.Payments.FindAsync(p => p.PatientId == patient.Id);
        var photos = await _unitOfWork.Photos.FindAsync(p => p.PatientId == patient.Id);

        var paidAmount = payments.Sum(p => p.Amount);
        var totalInvoices = invoices.Sum(i => i.TotalAmount);
        var outstanding = totalInvoices - paidAmount;

        string? doctorName = null;
        if (!string.IsNullOrEmpty(patient.AssignedDoctorId))
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(patient.AssignedDoctorId);
            if (doctor?.User != null)
            {
                doctorName = $"{doctor.User.FirstName} {doctor.User.LastName}".Trim();
            }
        }

        string? branchName = null;
        if (!string.IsNullOrEmpty(patient.BranchId))
        {
            var branch = await _unitOfWork.Branches.GetByIdAsync(patient.BranchId);
            branchName = branch?.Name;
        }

        var response = new PatientPortalSummaryResponse
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Email = patient.Email,
            Phone = patient.Phone,
            DateOfBirth = patient.DateOfBirth,
            Diagnosis = patient.Diagnosis,
            TreatmentPlanDescription = patient.TreatmentPlanDescription,
            TreatmentType = patient.TreatmentType,
            TreatmentStartDate = patient.TreatmentStartDate,
            EstimatedEndDate = patient.EstimatedEndDate,
            JourneyScore = patient.JourneyScore,
            ComplianceScore = patient.ComplianceScore,
            TotalTreatmentCost = patient.TotalTreatmentCost,
            AmountPaid = paidAmount,
            AmountRemaining = Math.Max(0, patient.TotalTreatmentCost - paidAmount),
            AssignedDoctorName = doctorName,
            BranchName = branchName,
            Stats = new PortalStats
            {
                TotalAppointments = appointments.Count(),
                UpcomingAppointments = appointments.Count(a =>
                    a.AppointmentDate > DateTime.UtcNow && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Completed),
                TotalPhotos = photos.Count(),
                UnpaidInvoices = invoices.Count(i =>
                    i.PaymentStatus == PaymentStatus.Pending || i.PaymentStatus == PaymentStatus.Partial || i.PaymentStatus == PaymentStatus.Overdue),
                OutstandingBalance = outstanding
            }
        };

        return Ok(ApiResponse<PatientPortalSummaryResponse>.SuccessResponse(response, "Portal data retrieved"));
    }

    /// <summary>
    /// Get current patient's appointments
    /// </summary>
    [HttpGet("appointments")]
    [ProducesResponseType(typeof(ApiResponse<List<PortalAppointmentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointments()
    {
        var patient = await GetMyPatientAsync();
        if (patient == null) return NotFound(ApiResponse.FailResponse("Patient profile not linked."));

        var appointments = await _unitOfWork.Appointments.FindAsync(a => a.PatientId == patient.Id);
        var items = appointments
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new PortalAppointmentResponse
            {
                Id = a.Id,
                AppointmentDate = a.AppointmentDate,
                Type = a.Type,
                Status = a.Status,
                Duration = a.Duration,
                Notes = a.Notes
            })
            .ToList();

        return Ok(ApiResponse<List<PortalAppointmentResponse>>.SuccessResponse(items));
    }

    /// <summary>
    /// Get current patient's photos
    /// </summary>
    [HttpGet("photos")]
    [ProducesResponseType(typeof(ApiResponse<List<PhotoResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPhotos()
    {
        var patient = await GetMyPatientAsync();
        if (patient == null) return NotFound(ApiResponse.FailResponse("Patient profile not linked."));

        var items = await _unitOfWork.Photos.Query()
            .Where(p => p.PatientId == patient.Id && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<PhotoResponse>>.SuccessResponse(
            items.Select(p => new PhotoResponse
            {
                Id = p.Id,
                PatientId = p.PatientId,
                DoctorId = p.DoctorId,
                Url = p.Url,
                FileName = p.FileName,
                FileType = p.FileType,
                FileSize = p.FileSize,
                Category = p.Category,
                Description = p.Description,
                VisitDate = p.VisitDate,
                BeforePhotoUrl = p.BeforePhotoUrl,
                AfterPhotoUrl = p.AfterPhotoUrl,
                ComparisonScore = p.ComparisonScore,
                CreatedAt = p.CreatedAt
            }).ToList()));
    }

    /// <summary>
    /// Get current patient's invoices
    /// </summary>
    [HttpGet("invoices")]
    [ProducesResponseType(typeof(ApiResponse<List<PortalInvoiceResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices()
    {
        var patient = await GetMyPatientAsync();
        if (patient == null) return NotFound(ApiResponse.FailResponse("Patient profile not linked."));

        var invoices = await _unitOfWork.Invoices.FindAsync(i => i.PatientId == patient.Id);
        var payments = await _unitOfWork.Payments.FindAsync(p => p.PatientId == patient.Id);

        var items = invoices.Select(i => new PortalInvoiceResponse
        {
            Id = i.Id,
            CreatedAt = i.CreatedAt,
            DueDate = i.DueDate,
            TotalAmount = i.TotalAmount,
            PaidAmount = payments.Where(p => p.InvoiceId == i.Id).Sum(p => p.Amount),
            Status = i.Status,
            PaymentStatus = i.PaymentStatus,
            Notes = i.Notes
        }).OrderByDescending(i => i.CreatedAt).ToList();

        return Ok(ApiResponse<List<PortalInvoiceResponse>>.SuccessResponse(items));
    }

    /// <summary>
    /// Get current patient's pain reports
    /// </summary>
    [HttpGet("pain-reports")]
    [ProducesResponseType(typeof(ApiResponse<List<PortalPainReportResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPainReports()
    {
        var patient = await GetMyPatientAsync();
        if (patient == null) return NotFound(ApiResponse.FailResponse("Patient profile not linked."));

        var reports = await _unitOfWork.PainReports.Query()
            .Where(p => p.PatientId == patient.Id && !p.IsDeleted)
            .OrderByDescending(p => p.ReportDate)
            .ToListAsync();

        return Ok(ApiResponse<List<PortalPainReportResponse>>.SuccessResponse(
            reports.Select(r => new PortalPainReportResponse
            {
                Id = r.Id,
                ReportDate = r.ReportDate,
                PainLevel = r.PainLevel,
                HasBrokenWire = r.HasBrokenWire,
                HasLooseBracket = r.HasLooseBracket,
                HasBleeding = r.HasBleeding,
                HasSwelling = r.HasSwelling,
                Description = r.Description,
                IsEmergency = r.IsEmergency,
                DoctorNotified = r.DoctorNotified
            }).ToList()));
    }

    /// <summary>
    /// Submit a pain report / progress update
    /// </summary>
    [HttpPost("pain-reports")]
    [ProducesResponseType(typeof(ApiResponse<PortalPainReportResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePainReport([FromBody] PortalPainReportRequest request)
    {
        var patient = await GetMyPatientAsync();
        if (patient == null) return NotFound(ApiResponse.FailResponse("Patient profile not linked."));

        var report = new PainReport
        {
            PatientId = patient.Id,
            ReportDate = DateTime.UtcNow,
            PainLevel = request.PainLevel,
            HasBrokenWire = request.HasBrokenWire,
            HasLooseBracket = request.HasLooseBracket,
            HasBleeding = request.HasBleeding,
            HasSwelling = request.HasSwelling,
            Description = request.Description,
            IsEmergency = request.PainLevel >= PainLevel.Severe,
            DoctorNotified = request.PainLevel >= PainLevel.Severe,
            ClinicId = CurrentClinicId
        };
        report.CreatedBy = CurrentUserId;

        await _unitOfWork.PainReports.AddAsync(report);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Pain report created for patient {PatientId} with level {Level}", patient.Id, report.PainLevel);

        return Created($"/api/portal/pain-reports/{report.Id}",
            ApiResponse<PortalPainReportResponse>.SuccessResponse(ToPainResponse(report), "Pain report submitted"));
    }

    /// <summary>
    /// Submit an emergency request (critical: notify clinic)
    /// </summary>
    [HttpPost("emergency")]
    [ProducesResponseType(typeof(ApiResponse<PortalEmergencyResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEmergency([FromBody] PortalEmergencyRequest request)
    {
        var patient = await GetMyPatientAsync();
        if (patient == null) return NotFound(ApiResponse.FailResponse("Patient profile not linked."));

        var emergency = new EmergencyRequest
        {
            PatientId = patient.Id,
            DoctorId = patient.AssignedDoctorId,
            Description = request.Description,
            Severity = request.Severity,
            Status = "Submitted",
            ClinicId = CurrentClinicId
        };
        emergency.CreatedBy = CurrentUserId;

        await _unitOfWork.EmergencyRequests.AddAsync(emergency);
        await _unitOfWork.CommitAsync();

        var notification = new Notification
        {
            UserId = CurrentUserId,
            Title = "Emergency request submitted",
            Message = $"Your emergency request has been received. A staff member will contact you shortly.",
            Type = NotificationType.Emergency,
            Channel = NotificationChannel.InApp,
            ClinicId = CurrentClinicId
        };
        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.CommitAsync();

        _logger.LogWarning("EMERGENCY request created for patient {PatientId} severity {Severity}", patient.Id, request.Severity);

        var response = new PortalEmergencyResponse
        {
            Id = emergency.Id,
            Description = emergency.Description,
            Severity = emergency.Severity,
            Status = emergency.Status,
            CreatedAt = emergency.CreatedAt
        };

        return Created($"/api/portal/emergency/{emergency.Id}",
            ApiResponse<PortalEmergencyResponse>.SuccessResponse(response, "Emergency request submitted"));
    }

    /// <summary>
    /// Get conversation between patient and the clinic staff
    /// </summary>
    [HttpGet("messages")]
    [ProducesResponseType(typeof(ApiResponse<List<PortalChatResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages([FromQuery] int count = 100)
    {
        var patient = await GetMyPatientAsync();
        if (patient == null || string.IsNullOrEmpty(patient.UserId))
        {
            return NotFound(ApiResponse.FailResponse("Patient profile not linked."));
        }

        var messages = await _unitOfWork.ChatMessages.Query()
            .Where(m => (m.SenderId == patient.UserId || m.ReceiverId == patient.UserId))
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync();
        messages.Reverse();

        var items = messages.Select(m => new PortalChatResponse
        {
            Id = m.Id,
            SenderId = m.SenderId,
            ReceiverId = m.ReceiverId,
            Message = m.Message,
            CreatedAt = m.CreatedAt,
            IsRead = m.IsRead
        }).ToList();

        return Ok(ApiResponse<List<PortalChatResponse>>.SuccessResponse(items, "Messages retrieved"));
    }

    /// <summary>
    /// Send a message to the clinic (receiver = clinic owner or assigned doctor)
    /// </summary>
    [HttpPost("messages")]
    [ProducesResponseType(typeof(ApiResponse<PortalChatResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage([FromBody] PortalMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(ApiResponse.FailResponse("Message cannot be empty."));
        }

        var patient = await GetMyPatientAsync();
        if (patient == null || string.IsNullOrEmpty(patient.UserId))
        {
            return NotFound(ApiResponse.FailResponse("Patient profile not linked."));
        }

        // Resolve staff receiver: assigned doctor first, otherwise any clinic staff
        string receiverId;
        if (!string.IsNullOrEmpty(patient.AssignedDoctorId))
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(patient.AssignedDoctorId);
            receiverId = doctor?.UserId ?? string.Empty;
        }
        else
        {
            var staff = await _unitOfWork.Staff.FindAsync(s => s.Status == StaffStatus.Active);
            receiverId = staff.FirstOrDefault()?.UserId ?? string.Empty;
        }

        if (string.IsNullOrEmpty(receiverId))
        {
            return BadRequest(ApiResponse.FailResponse("No clinic staff available to receive messages."));
        }

        var msg = new ChatMessage
        {
            SenderId = patient.UserId,
            ReceiverId = receiverId,
            PatientId = patient.Id,
            Message = request.Message,
            ClinicId = CurrentClinicId
        };
        msg.CreatedBy = CurrentUserId;

        await _unitOfWork.ChatMessages.AddAsync(msg);
        await _unitOfWork.CommitAsync();

        return Created($"/api/portal/messages/{msg.Id}",
            ApiResponse<PortalChatResponse>.SuccessResponse(new PortalChatResponse
            {
                Id = msg.Id,
                SenderId = msg.SenderId,
                ReceiverId = msg.ReceiverId,
                Message = msg.Message,
                CreatedAt = msg.CreatedAt,
                IsRead = msg.IsRead
            }, "Message sent"));
    }

    private static PortalPainReportResponse ToPainResponse(PainReport r) => new()
    {
        Id = r.Id,
        ReportDate = r.ReportDate,
        PainLevel = r.PainLevel,
        HasBrokenWire = r.HasBrokenWire,
        HasLooseBracket = r.HasLooseBracket,
        HasBleeding = r.HasBleeding,
        HasSwelling = r.HasSwelling,
        Description = r.Description,
        IsEmergency = r.IsEmergency,
        DoctorNotified = r.DoctorNotified
    };
}
