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
[Route("api/[controller]")]
[Authorize]
public class TreatmentVisitsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TreatmentVisitsController> _logger;

    public TreatmentVisitsController(IUnitOfWork unitOfWork, ILogger<TreatmentVisitsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all treatment visits for a patient
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TreatmentVisitResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPatient([FromQuery] string patientId)
    {
        if (string.IsNullOrEmpty(patientId))
        {
            return BadRequest(ApiResponse.FailResponse("PatientId is required"));
        }

        _logger.LogInformation("Getting treatment visits for patient: {PatientId}", patientId);

        var visits = await _unitOfWork.TreatmentVisits.FindAsync(v => v.PatientId == patientId && !v.IsDeleted);
        var items = visits.OrderByDescending(v => v.VisitDate).Select(MapToResponse).ToList();

        return Ok(ApiResponse<List<TreatmentVisitResponse>>.SuccessResponse(items, "Treatment visits retrieved"));
    }

    /// <summary>
    /// Get a treatment visit by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TreatmentVisitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var visit = await _unitOfWork.TreatmentVisits.GetByIdAsync(id);
        if (visit == null || visit.IsDeleted)
        {
            return NotFound(ApiResponse.FailResponse("Treatment visit not found"));
        }

        return Ok(ApiResponse<TreatmentVisitResponse>.SuccessResponse(MapToResponse(visit), "Treatment visit retrieved"));
    }

    /// <summary>
    /// Create a new treatment visit
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TreatmentVisitResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTreatmentVisitRequest request)
    {
        _logger.LogInformation("Creating treatment visit for patient: {PatientId}", request.PatientId);

        var patient = await _unitOfWork.Patients.GetByIdAsync(request.PatientId);
        if (patient == null || patient.IsDeleted)
        {
            return BadRequest(ApiResponse.FailResponse("Patient not found"));
        }

        var doctorId = request.DoctorId;
        if (string.IsNullOrEmpty(doctorId))
        {
            doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        var visit = new TreatmentVisit
        {
            PatientId = request.PatientId,
            DoctorId = doctorId,
            AppointmentId = request.AppointmentId,
            VisitDate = request.VisitDate,
            WireChanged = request.WireChanged,
            BracketReplaced = request.BracketReplaced,
            RubberBandsUsed = request.RubberBandsUsed,
            PainLevel = request.PainLevel,
            DoctorNotes = request.DoctorNotes,
            Medication = request.Medication,
            NextAppointmentDate = request.NextAppointmentDate,
            PhotosJson = request.PhotosJson,
            AttachmentsJson = request.AttachmentsJson,
            ClinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty
        };
        visit.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _unitOfWork.TreatmentVisits.AddAsync(visit);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Treatment visit created: {Id}", visit.Id);

        return CreatedAtAction(nameof(GetById), new { id = visit.Id },
            ApiResponse<TreatmentVisitResponse>.SuccessResponse(MapToResponse(visit), "Treatment visit created"));
    }

    /// <summary>
    /// Update an existing treatment visit
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TreatmentVisitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] CreateTreatmentVisitRequest request)
    {
        _logger.LogInformation("Updating treatment visit: {VisitId}", id);

        var visit = await _unitOfWork.TreatmentVisits.GetByIdAsync(id);
        if (visit == null || visit.IsDeleted)
        {
            return NotFound(ApiResponse.FailResponse("Treatment visit not found"));
        }

        visit.VisitDate = request.VisitDate;
        visit.WireChanged = request.WireChanged;
        visit.BracketReplaced = request.BracketReplaced;
        visit.RubberBandsUsed = request.RubberBandsUsed;
        visit.PainLevel = request.PainLevel;
        visit.DoctorNotes = request.DoctorNotes;
        visit.Medication = request.Medication;
        visit.NextAppointmentDate = request.NextAppointmentDate;
        visit.PhotosJson = request.PhotosJson;
        visit.AttachmentsJson = request.AttachmentsJson;

        visit.UpdatedAt = DateTime.UtcNow;
        visit.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _unitOfWork.TreatmentVisits.Update(visit);
        await _unitOfWork.CommitAsync();

        return Ok(ApiResponse<TreatmentVisitResponse>.SuccessResponse(MapToResponse(visit), "Treatment visit updated"));
    }

    /// <summary>
    /// Delete a treatment visit (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Deleting treatment visit: {VisitId}", id);

        var visit = await _unitOfWork.TreatmentVisits.GetByIdAsync(id);
        if (visit == null || visit.IsDeleted)
        {
            return NotFound(ApiResponse.FailResponse("Treatment visit not found"));
        }

        visit.IsDeleted = true;
        visit.UpdatedAt = DateTime.UtcNow;
        visit.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _unitOfWork.TreatmentVisits.Update(visit);
        await _unitOfWork.CommitAsync();

        return Ok(ApiResponse.SuccessResponse("Treatment visit deleted"));
    }

    private static TreatmentVisitResponse MapToResponse(TreatmentVisit v) => new()
    {
        Id = v.Id,
        PatientId = v.PatientId,
        PatientName = v.Patient?.FirstName + " " + v.Patient?.LastName ?? "",
        DoctorId = v.DoctorId,
        DoctorName = v.Doctor?.User?.FirstName + " " + v.Doctor?.User?.LastName ?? "",
        AppointmentId = v.AppointmentId,
        VisitDate = v.VisitDate,
        WireChanged = v.WireChanged,
        BracketReplaced = v.BracketReplaced,
        RubberBandsUsed = v.RubberBandsUsed,
        PainLevel = v.PainLevel,
        DoctorNotes = v.DoctorNotes,
        Medication = v.Medication,
        NextAppointmentDate = v.NextAppointmentDate,
        PhotosJson = v.PhotosJson,
        AttachmentsJson = v.AttachmentsJson,
        CreatedAt = v.CreatedAt
    };
}