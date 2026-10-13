using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Infrastructure.Services;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DashboardStats>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats([FromQuery] DashboardFilterRequest? filter = null)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        _logger.LogInformation("Getting dashboard stats for clinic: {ClinicId}", clinicId);

        var result = await _dashboardService.GetStatsAsync(clinicId, filter?.DateFrom, filter?.DateTo);
        return Ok(result);
    }

    /// <summary>
    /// Get revenue chart data
    /// </summary>
    [HttpGet("revenue-chart")]
    [ProducesResponseType(typeof(ApiResponse<RevenueChart>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueChart([FromQuery] int months = 12)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        _logger.LogInformation("Getting revenue chart for clinic: {ClinicId}, months: {Months}", clinicId, months);

        var result = await _dashboardService.GetRevenueChartAsync(clinicId, months);
        return Ok(result);
    }

    /// <summary>
    /// Get appointment chart data
    /// </summary>
    [HttpGet("appointment-chart")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentChart>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointmentChart([FromQuery] int months = 12)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        _logger.LogInformation("Getting appointment chart for clinic: {ClinicId}, months: {Months}", clinicId, months);

        var result = await _dashboardService.GetAppointmentChartAsync(clinicId, months);
        return Ok(result);
    }

    /// <summary>
    /// Get top doctors
    /// </summary>
    [HttpGet("top-doctors")]
    [ProducesResponseType(typeof(ApiResponse<List<TopDoctor>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopDoctors([FromQuery] int count = 5)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        _logger.LogInformation("Getting top doctors for clinic: {ClinicId}", clinicId);

        var result = await _dashboardService.GetTopDoctorsAsync(clinicId, count);
        return Ok(result);
    }

    /// <summary>
    /// Get recent activity
    /// </summary>
    [HttpGet("recent-activity")]
    [ProducesResponseType(typeof(ApiResponse<List<RecentActivity>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int count = 20)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        _logger.LogInformation("Getting recent activity for clinic: {ClinicId}", clinicId);

        var result = await _dashboardService.GetRecentActivityAsync(clinicId, count);
        return Ok(result);
    }
}
