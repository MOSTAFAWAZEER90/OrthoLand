using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OrthoFlowAI.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var clinicId = Context.User?.FindFirst("ClinicId")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to NotificationHub (ConnectionId: {ConnectionId})", userId, Context.ConnectionId);
        }

        if (!string.IsNullOrEmpty(clinicId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"clinic_{clinicId}");
            _logger.LogInformation("User joined clinic group {ClinicId} (ConnectionId: {ConnectionId})", clinicId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (exception != null)
        {
            _logger.LogError(exception, "User {UserId} disconnected with error (ConnectionId: {ConnectionId})", userId, Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("User {UserId} disconnected (ConnectionId: {ConnectionId})", userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendNotificationToUser(string userId, string title, string message, string type)
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
        {
            title,
            message,
            type,
            timestamp = DateTime.UtcNow
        });

        _logger.LogDebug("Notification sent to user {UserId}: {Title}", userId, title);
    }

    public async Task SendNotificationToClinic(string clinicId, string title, string message, string type)
    {
        await Clients.Group($"clinic_{clinicId}").SendAsync("ReceiveNotification", new
        {
            title,
            message,
            type,
            timestamp = DateTime.UtcNow
        });

        _logger.LogDebug("Notification sent to clinic {ClinicId}: {Title}", clinicId, title);
    }

    public async Task MarkNotificationRead(string notificationId)
    {
        var userId = Context.UserIdentifier ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await Clients.Caller.SendAsync("NotificationMarkedRead", notificationId);

        _logger.LogDebug("User {UserId} marked notification {NotificationId} as read", userId, notificationId);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Connection {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Connection {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", new { timestamp = DateTime.UtcNow });
    }
}
