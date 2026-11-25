using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrthoFlowAI.Application.Services;

namespace OrthoFlowAI.Infrastructure.WhatsApp;

public static class WhatsAppServiceCollectionExtensions
{
    /// <summary>
    /// Registers the WhatsApp channel and the sender chosen by WhatsApp:Provider
    /// ("Meta", "Twilio", "Log" or empty = disabled).
    /// </summary>
    public static IServiceCollection AddWhatsApp(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(WhatsAppSettings.SectionName);
        services.Configure<WhatsAppSettings>(section);
        var provider = section["Provider"]?.Trim() ?? string.Empty;

        switch (provider.ToLowerInvariant())
        {
            case "meta":
                services.AddHttpClient<IWhatsAppSender, MetaWhatsAppSender>(c => c.Timeout = TimeSpan.FromSeconds(20));
                break;
            case "twilio":
                services.AddScoped<IWhatsAppSender, TwilioWhatsAppSender>();
                break;
            case "log":
                services.AddScoped<IWhatsAppSender, LogWhatsAppSender>();
                break;
            default:
                services.AddSingleton<IWhatsAppSender, DisabledWhatsAppSender>();
                break;
        }

        services.AddScoped<INotificationChannel, WhatsAppNotificationChannel>();
        return services;
    }
}
