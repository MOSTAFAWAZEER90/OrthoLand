using System.Collections.Generic;
using System.Threading.Tasks;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.Services;

public interface INotificationChannel
{
    NotificationChannel Channel { get; }
    Task<ApiResponse> SendAsync(string recipient, string subject, string body, Dictionary<string, string>? metadata = null);
    Task<bool> IsAvailableAsync();
}

public interface INotificationChannelFactory
{
    INotificationChannel GetChannel(NotificationChannel channel);
    IEnumerable<INotificationChannel> GetAvailableChannels();
}