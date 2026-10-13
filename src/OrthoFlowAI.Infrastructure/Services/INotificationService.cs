using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Infrastructure.Services;

public interface INotificationService
{
    Task<ApiResponse<Notification>> GetByIdAsync(string id);
    Task<ApiResponse<PagedResult<Notification>>> GetAllAsync(
        int pageNumber, int pageSize, string? userId = null, NotificationType? type = null,
        bool? isRead = null, NotificationChannel? channel = null);
    Task<ApiResponse<Notification>> CreateAsync(Notification notification);
    Task<ApiResponse<Notification>> MarkAsReadAsync(string id);
    Task<ApiResponse> MarkAllAsReadAsync(string userId);
    Task<ApiResponse> DeleteAsync(string id);
    Task<ApiResponse<int>> GetUnreadCountAsync(string userId);
    Task<ApiResponse<List<Notification>>> GetRecentAsync(string userId, int count = 10);
}
