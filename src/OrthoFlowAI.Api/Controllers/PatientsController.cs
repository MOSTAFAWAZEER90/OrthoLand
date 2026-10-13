using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;
using OrthoFlowAI.Infrastructure.Services;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IPatientService patientService, IUnitOfWork unitOfWork, ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all patients with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Patient>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PatientFilterRequest filter)
    {
        _logger.LogInformation("Getting patients - Page: {PageNumber}, Size: {PageSize}, Status: {Status}",
            filter.PageNumber, filter.PageSize, filter.Status);

        var result = await _patientService.GetAllAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.SearchTerm,
            filter.Status,
            filter.AssignedDoctorId,
            filter.BranchId,
            filter.TreatmentType);

        return Ok(result);
    }

    /// <summary>
    /// Get all patients with filtering and pagination (POST)
    /// </summary>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Patient>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Filter([FromBody] PatientFilterRequest filter)
    {
        _logger.LogInformation("Filtering patients - Page: {PageNumber}, Size: {PageSize}", filter.PageNumber, filter.PageSize);

        var result = await _patientService.GetAllAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.SearchTerm,
            filter.Status,
            filter.AssignedDoctorId,
            filter.BranchId,
            filter.TreatmentType);

        return Ok(result);
    }

    /// <summary>
    /// Get a patient by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Patient>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogInformation("Getting patient: {PatientId}", id);
        var result = await _patientService.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get patient treatment profile with related data
    /// </summary>
    [HttpGet("{id}/profile")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(string id)
    {
        _logger.LogInformation("Getting patient profile: {PatientId}", id);
        var result = await _patientService.GetByIdAsync(id);

        if (!result.Success || result.Data == null)
        {
            return NotFound(result);
        }

        var patient = result.Data;
        var appointments = await _unitOfWork.Appointments.FindAsync(a => a.PatientId == id && !a.IsDeleted);
        var invoices = await _unitOfWork.Invoices.FindAsync(i => i.PatientId == id && !i.IsDeleted);
        var visits = await _unitOfWork.TreatmentVisits.FindAsync(v => v.PatientId == id && !v.IsDeleted);
        var photos = await _unitOfWork.Photos.FindAsync(p => p.PatientId == id && !p.IsDeleted);
        var documents = await _unitOfWork.Documents.FindAsync(d => d.PatientId == id && !d.IsDeleted);

        var profile = new
        {
            patient,
            stats = new
            {
                totalAppointments = appointments.Count(),
                completedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                upcomingAppointments = appointments.Count(a => a.AppointmentDate > DateTime.UtcNow && a.Status != AppointmentStatus.Cancelled),
                totalVisits = visits.Count(),
                totalInvoices = invoices.Count(),
                totalPhotos = photos.Count(),
                totalDocuments = documents.Count()
            }
        };

        return Ok(ApiResponse<object>.SuccessResponse(profile, "Patient profile retrieved"));
    }

    /// <summary>
    /// Get patient treatment timeline
    /// </summary>
    [HttpGet("{id}/timeline")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeline(string id)
    {
        _logger.LogInformation("Getting patient timeline: {PatientId}", id);
        var result = await _patientService.GetByIdAsync(id);

        if (!result.Success || result.Data == null)
        {
            return NotFound(result);
        }

        var visits = await _unitOfWork.TreatmentVisits.FindAsync(v => v.PatientId == id && !v.IsDeleted);
        var appointments = await _unitOfWork.Appointments.FindAsync(a => a.PatientId == id && !a.IsDeleted);

        var visitItems = visits
            .Select(v => new TimelineItem
            {
                Type = "visit",
                Date = v.VisitDate,
                Title = $"Visit - {v.VisitDate:MMM dd, yyyy}",
                DoctorNotes = v.DoctorNotes,
                WireChanged = v.WireChanged,
                BracketReplaced = v.BracketReplaced,
                RubberBandsUsed = v.RubberBandsUsed,
                PainLevel = v.PainLevel.ToString(),
                Medication = v.Medication
            });

        var appointmentItems = appointments
            .Select(a => new TimelineItem
            {
                Type = "appointment",
                Date = a.AppointmentDate,
                Title = $"{a.Type} - {a.AppointmentDate:MMM dd, yyyy HH:mm}",
                Notes = a.Notes,
                Status = a.Status.ToString(),
                AppointmentType = a.Type.ToString(),
                Duration = a.Duration
            });

        var timeline = visitItems
            .Concat(appointmentItems)
            .OrderByDescending(x => x.Date)
            .ToList();

        return Ok(ApiResponse<object>.SuccessResponse(timeline, "Patient timeline retrieved"));
    }

    /// <summary>
    /// Create a new patient
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Patient>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
    {
        _logger.LogInformation("Creating patient: {FirstName} {LastName}", request.FirstName, request.LastName);

        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;

        var patient = new Patient
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            City = request.City,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            InsuranceProvider = request.InsuranceProvider,
            InsuranceNumber = request.InsuranceNumber,
            Diagnosis = request.Diagnosis,
            TreatmentPlanDescription = request.TreatmentPlanDescription,
            TreatmentType = request.TreatmentType,
            TreatmentStartDate = request.TreatmentStartDate,
            EstimatedEndDate = request.EstimatedEndDate,
            AssignedDoctorId = request.AssignedDoctorId,
            BranchId = request.BranchId,
            Tags = request.Tags,
            TotalTreatmentCost = request.TotalTreatmentCost,
            Notes = request.Notes,
            ClinicId = clinicId,
            Status = PatientStatus.Active
        };

        patient.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _patientService.CreateAsync(patient);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation("Patient created: {Id}", result.Data!.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Update an existing patient
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Patient>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePatientRequest request)
    {
        _logger.LogInformation("Updating patient: {PatientId}", id);

        var existing = await _patientService.GetByIdAsync(id);
        if (!existing.Success || existing.Data == null)
        {
            return NotFound(existing);
        }

        var patient = existing.Data;

        if (request.FirstName != null) patient.FirstName = request.FirstName;
        if (request.LastName != null) patient.LastName = request.LastName;
        if (request.Email != null) patient.Email = request.Email;
        if (request.Phone != null) patient.Phone = request.Phone;
        if (request.DateOfBirth.HasValue) patient.DateOfBirth = request.DateOfBirth;
        if (request.Gender.HasValue) patient.Gender = request.Gender;
        if (request.Address != null) patient.Address = request.Address;
        if (request.City != null) patient.City = request.City;
        if (request.EmergencyContactName != null) patient.EmergencyContactName = request.EmergencyContactName;
        if (request.EmergencyContactPhone != null) patient.EmergencyContactPhone = request.EmergencyContactPhone;
        if (request.InsuranceProvider != null) patient.InsuranceProvider = request.InsuranceProvider;
        if (request.InsuranceNumber != null) patient.InsuranceNumber = request.InsuranceNumber;
        if (request.Diagnosis != null) patient.Diagnosis = request.Diagnosis;
        if (request.TreatmentPlanDescription != null) patient.TreatmentPlanDescription = request.TreatmentPlanDescription;
        if (request.TreatmentType.HasValue) patient.TreatmentType = request.TreatmentType;
        if (request.TreatmentStartDate.HasValue) patient.TreatmentStartDate = request.TreatmentStartDate;
        if (request.EstimatedEndDate.HasValue) patient.EstimatedEndDate = request.EstimatedEndDate;
        if (request.Status.HasValue) patient.Status = request.Status.Value;
        if (request.AssignedDoctorId != null) patient.AssignedDoctorId = request.AssignedDoctorId;
        if (request.BranchId != null) patient.BranchId = request.BranchId;
        if (request.Tags != null) patient.Tags = request.Tags;
        if (request.TotalTreatmentCost.HasValue) patient.TotalTreatmentCost = request.TotalTreatmentCost.Value;
        if (request.AmountPaid.HasValue) patient.AmountPaid = request.AmountPaid.Value;
        if (request.Notes != null) patient.Notes = request.Notes;

        patient.UpdatedAt = DateTime.UtcNow;
        patient.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _patientService.UpdateAsync(patient);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a patient
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Deleting patient: {PatientId}", id);
        var result = await _patientService.DeleteAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}

public class TimelineItem
{
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? DoctorNotes { get; set; }
    public bool WireChanged { get; set; }
    public bool BracketReplaced { get; set; }
    public bool RubberBandsUsed { get; set; }
    public string? PainLevel { get; set; }
    public string? Medication { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
    public string? AppointmentType { get; set; }
    public int Duration { get; set; }
}
