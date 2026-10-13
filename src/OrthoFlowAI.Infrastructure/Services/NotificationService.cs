using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Notification>> GetByIdAsync(string id)
    {
        var notifications = await _unitOfWork.Notifications.FindAsync(n => n.Id == id);
        var notification = notifications.FirstOrDefault();
        if (notification is null)
        {
            return ApiResponse<Notification>.FailResponse("Notification not found.");
        }

        return ApiResponse<Notification>.SuccessResponse(notification);
    }

    public async Task<ApiResponse<PagedResult<Notification>>> GetAllAsync(
        int pageNumber, int pageSize, string? userId = null, NotificationType? type = null,
        bool? isRead = null, NotificationChannel? channel = null)
    {
        Expression<Func<Notification, bool>>? filter = BuildFilter(userId, type, isRead, channel);

        var result = await _unitOfWork.Notifications.GetPagedAsync(
            pageNumber,
            pageSize,
            filter,
            q => q.OrderByDescending(n => n.CreatedAt));

        return ApiResponse<PagedResult<Notification>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<Notification>> CreateAsync(Notification notification)
    {
        notification.CreatedAt = DateTime.UtcNow;
        notification.IsRead = false;

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Notification>.SuccessResponse(notification, "Notification created successfully.");
    }

    public async Task<ApiResponse<Notification>> MarkAsReadAsync(string id)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
        if (notification is null)
        {
            return ApiResponse<Notification>.FailResponse("Notification not found.");
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Notification>.SuccessResponse(notification, "Notification marked as read.");
    }

    public async Task<ApiResponse> MarkAllAsReadAsync(string userId)
    {
        var unread = await _unitOfWork.Notifications.FindAsync(n =>
            n.UserId == userId && !n.IsRead);

        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            _unitOfWork.Notifications.Update(notification);
        }

        await _unitOfWork.CommitAsync();

        return ApiResponse.SuccessResponse($"{unread.Count} notifications marked as read.");
    }

    public async Task<ApiResponse> DeleteAsync(string id)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
        if (notification is null)
        {
            return ApiResponse.FailResponse("Notification not found.");
        }

        notification.IsDeleted = true;
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.CommitAsync();

        return ApiResponse.SuccessResponse("Notification deleted successfully.");
    }

    public async Task<ApiResponse<int>> GetUnreadCountAsync(string userId)
    {
        var count = await _unitOfWork.Notifications.Query()
            .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);

        return ApiResponse<int>.SuccessResponse(count);
    }

    public async Task<ApiResponse<List<Notification>>> GetRecentAsync(string userId, int count = 10)
    {
        var notifications = await _unitOfWork.Notifications.Query()
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();

        return ApiResponse<List<Notification>>.SuccessResponse(notifications);
    }

    private static Expression<Func<Notification, bool>>? BuildFilter(
        string? userId, NotificationType? type, bool? isRead, NotificationChannel? channel)
    {
        Expression<Func<Notification, bool>>? filter = null;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            filter = CombineFilters(filter, n => n.UserId == userId);
        }

        if (type.HasValue)
        {
            filter = CombineFilters(filter, n => n.Type == type.Value);
        }

        if (isRead.HasValue)
        {
            filter = CombineFilters(filter, n => n.IsRead == isRead.Value);
        }

        if (channel.HasValue)
        {
            filter = CombineFilters(filter, n => n.Channel == channel.Value);
        }

        return filter;
    }

    private static Expression<Func<Notification, bool>>? CombineFilters(
        Expression<Func<Notification, bool>>? first, Expression<Func<Notification, bool>> second)
    {
        if (first is null)
        {
            return second;
        }

        var parameter = Expression.Parameter(typeof(Notification), "n");
        var body = Expression.AndAlso(
            Expression.Invoke(first, parameter),
            Expression.Invoke(second, parameter));

        return Expression.Lambda<Func<Notification, bool>>(body, parameter);
    }
}
