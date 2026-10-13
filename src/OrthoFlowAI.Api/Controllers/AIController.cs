using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Infrastructure.Services;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly ILogger<AIController> _logger;

    public AIController(IAIService aiService, ILogger<AIController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Generate an AI-powered treatment report
    /// </summary>
    [HttpPost("generate-report")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateReport([FromBody] TreatmentReportRequest request)
    {
        _logger.LogInformation("Generating AI treatment report for patient: {PatientName}", request.PatientName);
        var result = await _aiService.GenerateTreatmentReportAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Generate an AI-powered treatment summary
    /// </summary>
    [HttpPost("generate-summary")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateSummary([FromBody] TreatmentSummaryRequest request)
    {
        _logger.LogInformation("Generating AI treatment summary for patient: {PatientId}", request.PatientId);
        var result = await _aiService.GenerateTreatmentSummaryAsync(request.PatientId, request.Visits);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Generate AI-powered treatment recommendations
    /// </summary>
    [HttpPost("generate-recommendations")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateRecommendations([FromBody] RecommendationsRequest request)
    {
        _logger.LogInformation("Generating AI recommendations for patient profile");
        var result = await _aiService.GenerateRecommendationsAsync(request.PatientProfile);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Clinical chat - ask AI clinical questions
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClinicalChat([FromBody] ClinicalChatRequest request)
    {
        _logger.LogInformation("Clinical chat query: {Query}", request.Message);

        var reportRequest = new TreatmentReportRequest
        {
            PatientName = "Clinical Query",
            Diagnosis = request.Message,
            TreatmentType = "General",
            StartDate = DateTime.UtcNow,
            AdditionalNotes = "The user is asking a clinical question. Provide a professional, evidence-based response."
        };

        var result = await _aiService.GenerateTreatmentReportAsync(reportRequest);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

public class ClinicalChatRequest
{
    public string Message { get; set; } = string.Empty;
}
