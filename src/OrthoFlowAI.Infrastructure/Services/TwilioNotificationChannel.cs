using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Infrastructure.Settings;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace OrthoFlowAI.Infrastructure.Services;

public class TwilioNotificationChannel : INotificationChannel
{
    private readonly TwilioSettings _settings;
    private readonly ILogger<TwilioNotificationChannel> _logger;
    private bool _initialized;

    public NotificationChannel Channel => NotificationChannel.WhatsApp;

    public TwilioNotificationChannel(IOptions<TwilioSettings> settings, ILogger<TwilioNotificationChannel> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private void Initialize()
    {
        if (!_initialized && !string.IsNullOrEmpty(_settings.AccountSid) && !string.IsNullOrEmpty(_settings.AuthToken))
        {
            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
            _initialized = true;
        }
    }

    public async Task<ApiResponse> SendAsync(string recipient, string subject, string body, Dictionary<string, string>? metadata = null)
    {
        Initialize();
        
        if (!_initialized)
        {
            return ApiResponse.FailResponse("Twilio not configured");
        }

        try
        {
            var isWhatsApp = metadata?.TryGetValue("channel", out var channel) == true && channel == "whatsapp";
            var fromNumber = isWhatsApp ? _settings.WhatsAppFromNumber : _settings.FromPhoneNumber;
            var toNumber = isWhatsApp ? $"whatsapp:{recipient}" : recipient;

            var messageOptions = new CreateMessageOptions(new PhoneNumber(toNumber))
            {
                From = new PhoneNumber(fromNumber),
                Body = body
            };

            if (isWhatsApp && !string.IsNullOrEmpty(_settings.WhatsAppTemplateSid))
            {
                messageOptions.ContentSid = _settings.WhatsAppTemplateSid;
            }

            var message = await MessageResource.CreateAsync(messageOptions);

            _logger.LogInformation("Twilio message sent to {Recipient}: SID={Sid}, Status={Status}", recipient, message.Sid, message.Status);
            return ApiResponse.SuccessResponse("Message sent successfully");
        }
        catch (Twilio.Exceptions.ApiException ex)
        {
            _logger.LogError(ex, "Twilio API error sending to {Recipient}: {Message}", recipient, ex.Message);
            return ApiResponse.FailResponse($"Twilio error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Twilio message to {Recipient}", recipient);
            return ApiResponse.FailResponse($"Error sending message: {ex.Message}");
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        Initialize();
        return _initialized;
    }
}