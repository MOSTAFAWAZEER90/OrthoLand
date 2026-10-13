using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Infrastructure.Settings;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace OrthoFlowAI.Infrastructure.Services;

public class PushNotificationChannel : INotificationChannel
{
    private readonly PushNotificationSettings _settings;
    private readonly ILogger<PushNotificationChannel> _logger;
    private bool _initialized;

    public NotificationChannel Channel => NotificationChannel.Push;

    public PushNotificationChannel(IOptions<PushNotificationSettings> settings, ILogger<PushNotificationChannel> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private void Initialize()
    {
        if (!_initialized && _settings.Provider == "Firebase" && !string.IsNullOrEmpty(_settings.FirebaseProjectId))
        {
            try
            {
                var credential = GoogleCredential.FromJson($@"
                {{
                    ""type"": ""service_account"",
                    ""project_id"": ""{_settings.FirebaseProjectId}"",
                    ""private_key"": ""{_settings.FirebasePrivateKey.Replace("\n", "\\n")}"",
                    ""client_email"": ""{_settings.FirebaseClientEmail}""
                }}");

                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = _settings.FirebaseProjectId
                });

                _initialized = true;
                _logger.LogInformation("Firebase Admin SDK initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase");
            }
        }
    }

    public async Task<ApiResponse> SendAsync(string recipient, string subject, string body, Dictionary<string, string>? metadata = null)
    {
        Initialize();
        
        if (!_initialized)
        {
            return ApiResponse.FailResponse("Push notifications not configured");
        }

        try
        {
            var message = new Message
            {
                Token = recipient,
                Notification = new Notification
                {
                    Title = subject,
                    Body = body
                },
                Data = metadata ?? new Dictionary<string, string>(),
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Title = subject,
                        Body = body,
                        Icon = "ic_notification",
                        Color = "#6366f1"
                    }
                },
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Alert = new ApsAlert
                        {
                            Title = subject,
                            Body = body
                        },
                        Badge = 1,
                        Sound = "default"
                    }
                }
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            
            _logger.LogInformation("Push notification sent to {Recipient}: {Response}", recipient, response);
            return ApiResponse.SuccessResponse("Push notification sent");
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Firebase error sending to {Recipient}: {ErrorCode}", recipient, ex.ErrorCode);
            return ApiResponse.FailResponse($"Firebase error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to {Recipient}", recipient);
            return ApiResponse.FailResponse($"Error sending push: {ex.Message}");
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        Initialize();
        return _initialized;
    }
}