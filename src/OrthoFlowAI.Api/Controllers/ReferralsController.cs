using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferralsController : ControllerBase
{
    private readonly IReferralService _referralService;
    private readonly ILogger<ReferralsController> _logger;

    public ReferralsController(IReferralService referralService, ILogger<ReferralsController> logger)
    {
        _referralService = referralService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new referral
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReferralResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateReferralRequest request)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(clinicId))
            return BadRequest(ApiResponse.FailResponse("Clinic context not found"));

        _logger.LogInformation("Creating referral by patient {PatientId}", request.ReferrerPatientId);
        var result = await _referralService.CreateReferralAsync(request, clinicId);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetByCode), new { code = result.Data!.ReferralCode }, result);
    }

    /// <summary>
    /// Get referral by code
    /// </summary>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<ReferralResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code)
    {
        _logger.LogInformation("Getting referral by code: {Code}", code);
        var result = await _referralService.GetByCodeAsync(code);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Complete a referral
    /// </summary>
    [HttpPost("complete")]
    [ProducesResponseType(typeof(ApiResponse<ReferralResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Complete([FromBody] CompleteReferralRequest request)
    {
        _logger.LogInformation("Completing referral with code: {Code}", request.Code);
        var result = await _referralService.CompleteReferralAsync(request.Code, request.ReferredPatientId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Get all referrals for the clinic
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReferralResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(clinicId))
            return BadRequest(ApiResponse.FailResponse("Clinic context not found"));

        _logger.LogInformation("Getting referrals for clinic: {ClinicId}", clinicId);
        var result = await _referralService.GetReferralsAsync(clinicId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get referral stats for a referrer
    /// </summary>
    [HttpGet("referrer/{patientId}/stats")]
    [ProducesResponseType(typeof(ApiResponse<ReferralStatsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReferrerStats(string patientId)
    {
        _logger.LogInformation("Getting referral stats for patient: {PatientId}", patientId);
        var result = await _referralService.GetReferrerStatsAsync(patientId);
        return Ok(result);
    }

    /// <summary>
    /// Get referral program settings
    /// </summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(ApiResponse<ReferralProgramSettingsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(clinicId))
            return BadRequest(ApiResponse.FailResponse("Clinic context not found"));

        var result = await _referralService.GetProgramSettingsAsync(clinicId);
        return Ok(result);
    }

    /// <summary>
    /// Update referral program settings
    /// </summary>
    [HttpPut("settings")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings([FromBody] ReferralProgramSettingsResponse settings)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(clinicId))
            return BadRequest(ApiResponse.FailResponse("Clinic context not found"));

        var result = await _referralService.UpdateProgramSettingsAsync(clinicId, settings);
        return Ok(result);
    }
}

public class CompleteReferralRequest
{
    public string Code { get; set; } = string.Empty;
    public string ReferredPatientId { get; set; } = string.Empty;
}