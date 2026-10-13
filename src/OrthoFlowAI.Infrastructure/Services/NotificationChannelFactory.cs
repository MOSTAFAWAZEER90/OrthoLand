using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Infrastructure.Services;

public class NotificationChannelFactory : INotificationChannelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<INotificationChannel> _channels;

    public NotificationChannelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _channels = _serviceProvider.GetServices<INotificationChannel>();
    }

    public INotificationChannel GetChannel(NotificationChannel channel)
    {
        return _channels.FirstOrDefault(c => c.Channel == channel) 
            ?? throw new InvalidOperationException($"No notification channel registered for {channel}");
    }

    public IEnumerable<INotificationChannel> GetAvailableChannels()
    {
        return _channels.Where(c => c.IsAvailableAsync().GetAwaiter().GetResult());
    }
}