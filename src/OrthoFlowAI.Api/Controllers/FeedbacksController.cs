using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeedbacksController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;
    private readonly ILogger<FeedbacksController> _logger;

    public FeedbacksController(IFeedbackService feedbackService, ILogger<FeedbacksController> logger)
    {
        _feedbackService = feedbackService;
        _logger = logger;
    }

    /// <summary>
    /// Submit feedback
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackRequest request)
    {
        _logger.LogInformation("Creating feedback by patient {PatientId}", request.PatientId);
        var result = await _feedbackService.CreateFeedbackAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Get all feedback with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<FeedbackResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] FeedbackFilterRequest filter)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(clinicId))
            return BadRequest(ApiResponse.FailResponse("Clinic context not found"));

        filter.ClinicId = clinicId;
        _logger.LogInformation("Getting feedbacks for clinic: {ClinicId}", clinicId);
        var result = await _feedbackService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get feedback by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogInformation("Getting feedback: {FeedbackId}", id);
        var result = await _feedbackService.GetByIdAsync(id);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get feedback analytics
    /// </summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(clinicId))
            return BadRequest(ApiResponse.FailResponse("Clinic context not found"));

        _logger.LogInformation("Getting feedback analytics for clinic: {ClinicId}", clinicId);
        var result = await _feedbackService.GetAnalyticsAsync(clinicId, from, to);
        return Ok(result);
    }

    /// <summary>
    /// Get NPS stats
    /// </summary>
    [HttpGet("nps")]
    [ProducesResponseType(typeof(ApiResponse<NPSStatsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNPS([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(clinicId))
            return BadRequest(ApiResponse.FailResponse("Clinic context not found"));

        var result = await _feedbackService.GetNPSStatsAsync(clinicId, from, to);
        return Ok(result);
    }

    /// <summary>
    /// Delete feedback
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Deleting feedback: {FeedbackId}", id);
        var result = await _feedbackService.DeleteAsync(id);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}