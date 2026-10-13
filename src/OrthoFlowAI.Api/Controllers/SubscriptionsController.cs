using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(ISubscriptionService subscriptionService, ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<SubscriptionPlanResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans()
    {
        var result = await _subscriptionService.GetAvailablePlansAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get current clinic's subscription status
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<ClinicSubscriptionStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus()
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(clinicId))
        {
            return BadRequest(ApiResponse.FailResponse("Clinic context not found"));
        }

        _logger.LogInformation("Getting subscription status for clinic: {ClinicId}", clinicId);
        var result = await _subscriptionService.GetSubscriptionStatusAsync(clinicId);
        return Ok(result);
    }

    /// <summary>
    /// Create a Stripe checkout session for subscription upgrade
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionCheckoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutRequest request)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(clinicId))
        {
            return BadRequest(ApiResponse.FailResponse("Clinic context not found"));
        }

        _logger.LogInformation("Creating checkout session for clinic: {ClinicId}, plan: {PriceId}", clinicId, request.PriceId);

        var successUrl = request.SuccessUrl ?? $"{request.BaseUrl}/settings?subscription=success";
        var cancelUrl = request.CancelUrl ?? $"{request.BaseUrl}/settings?subscription=cancelled";

        var result = await _subscriptionService.CreateCheckoutSessionAsync(clinicId, request.PriceId, successUrl, cancelUrl);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a Stripe billing portal session
    /// </summary>
    [HttpPost("portal")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionPortalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePortal([FromBody] CreatePortalRequest request)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(clinicId))
        {
            return BadRequest(ApiResponse.FailResponse("Clinic context not found"));
        }

        _logger.LogInformation("Creating billing portal session for clinic: {ClinicId}", clinicId);

        var returnUrl = request.ReturnUrl ?? $"{request.BaseUrl}/settings";
        var result = await _subscriptionService.CreateBillingPortalSessionAsync(clinicId, returnUrl);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Stripe webhook endpoint
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        _logger.LogInformation("Received Stripe webhook");

        var result = await _subscriptionService.HandleWebhookAsync(json, signature);

        if (!result.Success)
        {
            _logger.LogWarning("Webhook processing failed: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }
}

public class CreateCheckoutRequest
{
    public string PriceId { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}

public class CreatePortalRequest
{
    public string ReturnUrl { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}