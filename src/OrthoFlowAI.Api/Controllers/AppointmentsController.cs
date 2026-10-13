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
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(IAppointmentService appointmentService, ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all appointments with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Appointment>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AppointmentFilterRequest filter)
    {
        _logger.LogInformation("Getting appointments - Page: {PageNumber}, Size: {PageSize}",
            filter.PageNumber, filter.PageSize);

        var result = await _appointmentService.GetAllAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.DateFrom,
            filter.DateTo,
            filter.DoctorId,
            filter.PatientId,
            filter.Status);

        return Ok(result);
    }

    /// <summary>
    /// Filter appointments via POST
    /// </summary>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Appointment>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Filter([FromBody] AppointmentFilterRequest filter)
    {
        _logger.LogInformation("Filtering appointments - Page: {PageNumber}, Size: {PageSize}",
            filter.PageNumber, filter.PageSize);

        var result = await _appointmentService.GetAllAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.DateFrom,
            filter.DateTo,
            filter.DoctorId,
            filter.PatientId,
            filter.Status);

        return Ok(result);
    }

    /// <summary>
    /// Get an appointment by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Appointment>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogInformation("Getting appointment: {AppointmentId}", id);
        var result = await _appointmentService.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get calendar events within a date range
    /// </summary>
    [HttpGet("calendar-events")]
    [ProducesResponseType(typeof(ApiResponse<List<Appointment>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCalendarEvents(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? doctorId = null)
    {
        _logger.LogInformation("Getting calendar events from {StartDate} to {EndDate}", startDate, endDate);
        var result = await _appointmentService.GetCalendarEventsAsync(startDate, endDate, doctorId);
        return Ok(result);
    }

    /// <summary>
    /// Create a new appointment
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Appointment>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        _logger.LogInformation("Creating appointment for patient {PatientId} on {Date}",
            request.PatientId, request.AppointmentDate);

        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;

        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            BranchId = request.BranchId,
            AppointmentDate = request.AppointmentDate,
            EndTime = request.EndTime,
            Type = request.Type,
            Notes = request.Notes,
            Duration = request.Duration,
            Color = request.Color,
            IsRecurring = request.IsRecurring,
            RecurrencePattern = request.RecurrencePattern,
            RecurrenceEndDate = request.RecurrenceEndDate,
            Status = AppointmentStatus.Scheduled,
            ClinicId = clinicId
        };

        appointment.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _appointmentService.CreateAsync(appointment);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation("Appointment created: {Id}", result.Data!.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Update an existing appointment
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Appointment>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateAppointmentRequest request)
    {
        _logger.LogInformation("Updating appointment: {AppointmentId}", id);

        var existing = await _appointmentService.GetByIdAsync(id);
        if (!existing.Success || existing.Data == null)
        {
            return NotFound(existing);
        }

        var appointment = existing.Data;

        if (request.PatientId != null) appointment.PatientId = request.PatientId;
        if (request.DoctorId != null) appointment.DoctorId = request.DoctorId;
        if (request.BranchId != null) appointment.BranchId = request.BranchId;
        if (request.AppointmentDate.HasValue) appointment.AppointmentDate = request.AppointmentDate.Value;
        if (request.EndTime.HasValue) appointment.EndTime = request.EndTime;
        if (request.Status.HasValue) appointment.Status = request.Status.Value;
        if (request.Type.HasValue) appointment.Type = request.Type.Value;
        if (request.Notes != null) appointment.Notes = request.Notes;
        if (request.Duration.HasValue) appointment.Duration = request.Duration.Value;
        if (request.Color != null) appointment.Color = request.Color;
        if (request.IsRecurring.HasValue) appointment.IsRecurring = request.IsRecurring.Value;
        if (request.RecurrencePattern != null) appointment.RecurrencePattern = request.RecurrencePattern;
        if (request.RecurrenceEndDate.HasValue) appointment.RecurrenceEndDate = request.RecurrenceEndDate;

        appointment.UpdatedAt = DateTime.UtcNow;
        appointment.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _appointmentService.UpdateAsync(appointment);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete an appointment
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Deleting appointment: {AppointmentId}", id);
        var result = await _appointmentService.DeleteAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Update appointment status
    /// </summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<Appointment>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        _logger.LogInformation("Updating appointment {AppointmentId} status to {Status}", id, request.Status);
        var result = await _appointmentService.UpdateStatusAsync(id, request.Status);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}

public class UpdateStatusRequest
{
    public AppointmentStatus Status { get; set; }
}
