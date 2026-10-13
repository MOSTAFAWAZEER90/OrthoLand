using System.ComponentModel.DataAnnotations;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class NotificationResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public NotificationChannel Channel { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SendNotificationRequest
{
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public NotificationType Type { get; set; }

    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    [MaxLength(4000)]
    public string? MetadataJson { get; set; }
}

public class MarkAsReadRequest
{
    [Required]
    public string NotificationId { get; set; } = string.Empty;
}
