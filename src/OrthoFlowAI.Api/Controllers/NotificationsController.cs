using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OrthoFlowAI.Api.Hubs;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Infrastructure.Services;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Get notifications with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Notification>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? userId = null,
        [FromQuery] NotificationType? type = null,
        [FromQuery] bool? isRead = null,
        [FromQuery] NotificationChannel? channel = null)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var targetUserId = userId ?? currentUserId;

        _logger.LogInformation("Getting notifications for user {UserId}, page {PageNumber}", targetUserId, pageNumber);

        var result = await _notificationService.GetAllAsync(pageNumber, pageSize, targetUserId, type, isRead, channel);
        return Ok(result);
    }

    /// <summary>
    /// Get a notification by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Notification>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogInformation("Getting notification: {NotificationId}", id);
        var result = await _notificationService.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new notification
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Notification>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SendNotificationRequest request)
    {
        _logger.LogInformation("Creating notification for user {UserId}: {Title}", request.UserId, request.Title);

        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;

        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Channel = request.Channel,
            MetadataJson = request.MetadataJson,
            ClinicId = clinicId,
            IsRead = false
        };

        notification.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _notificationService.CreateAsync(notification);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        await _hubContext.Clients.Group($"user_{request.UserId}").SendAsync("ReceiveNotification", new
        {
            id = result.Data!.Id,
            title = request.Title,
            message = request.Message,
            type = request.Type.ToString(),
            timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Notification created: {Id}", result.Data.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPut("{id}/read")]
    [ProducesResponseType(typeof(ApiResponse<Notification>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        _logger.LogInformation("Marking notification as read: {NotificationId}", id);
        var result = await _notificationService.MarkAsReadAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Mark all notifications as read for current user
    /// </summary>
    [HttpPut("read-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse.FailResponse("User not identified"));
        }

        _logger.LogInformation("Marking all notifications as read for user: {UserId}", userId);
        var result = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse.FailResponse("User not identified"));
        }

        _logger.LogInformation("Getting unread count for user: {UserId}", userId);
        var result = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(result);
    }
}
