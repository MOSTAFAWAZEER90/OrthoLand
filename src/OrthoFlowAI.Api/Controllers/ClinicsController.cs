using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Infrastructure.Services;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClinicsController : ControllerBase
{
    private readonly IClinicService _clinicService;
    private readonly ILogger<ClinicsController> _logger;

    public ClinicsController(IClinicService clinicService, ILogger<ClinicsController> logger)
    {
        _clinicService = clinicService;
        _logger = logger;
    }

    /// <summary>
    /// Get all clinics with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Clinic>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        _logger.LogInformation("Getting clinics - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
        var result = await _clinicService.GetAllAsync(pageNumber, pageSize, search, isActive);
        return Ok(result);
    }

    /// <summary>
    /// Get a clinic by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Clinic>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogInformation("Getting clinic: {ClinicId}", id);
        var result = await _clinicService.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new clinic
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Clinic>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateClinicRequest request)
    {
        _logger.LogInformation("Creating clinic: {Name}", request.Name);

        var clinic = new Clinic
        {
            Name = request.Name,
            Logo = request.Logo,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            Website = request.Website,
            BusinessHours = request.BusinessHours,
            WorkingDays = request.WorkingDays,
            PrimaryColor = request.PrimaryColor,
            SecondaryColor = request.SecondaryColor,
            Language = request.Language,
            IsActive = true
        };

        clinic.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _clinicService.CreateAsync(clinic);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation("Clinic created: {Id}", result.Data!.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Update an existing clinic
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Clinic>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateClinicRequest request)
    {
        _logger.LogInformation("Updating clinic: {ClinicId}", id);

        var existing = await _clinicService.GetByIdAsync(id);
        if (!existing.Success || existing.Data == null)
        {
            return NotFound(existing);
        }

        var clinic = existing.Data;

        if (request.Name != null) clinic.Name = request.Name;
        if (request.Logo != null) clinic.Logo = request.Logo;
        if (request.Phone != null) clinic.Phone = request.Phone;
        if (request.Email != null) clinic.Email = request.Email;
        if (request.Address != null) clinic.Address = request.Address;
        if (request.City != null) clinic.City = request.City;
        if (request.Country != null) clinic.Country = request.Country;
        if (request.Website != null) clinic.Website = request.Website;
        if (request.BusinessHours != null) clinic.BusinessHours = request.BusinessHours;
        if (request.WorkingDays != null) clinic.WorkingDays = request.WorkingDays;
        if (request.PrimaryColor != null) clinic.PrimaryColor = request.PrimaryColor;
        if (request.SecondaryColor != null) clinic.SecondaryColor = request.SecondaryColor;
        if (request.Language != null) clinic.Language = request.Language;
        if (request.IsActive.HasValue) clinic.IsActive = request.IsActive.Value;

        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _clinicService.UpdateAsync(clinic);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a clinic
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Deleting clinic: {ClinicId}", id);
        var result = await _clinicService.DeleteAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
