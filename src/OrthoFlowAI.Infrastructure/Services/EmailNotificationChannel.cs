using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Infrastructure.Settings;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace OrthoFlowAI.Infrastructure.Services;

public class EmailNotificationChannel : INotificationChannel
{
    private readonly ISendGridClient _sendGridClient;
    private readonly SendGridSettings _settings;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public NotificationChannel Channel => NotificationChannel.Email;

    public EmailNotificationChannel(ISendGridClient sendGridClient, IOptions<SendGridSettings> settings, ILogger<EmailNotificationChannel> logger)
    {
        _sendGridClient = sendGridClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ApiResponse> SendAsync(string recipient, string subject, string body, Dictionary<string, string>? metadata = null)
    {
        try
        {
            var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var to = new EmailAddress(recipient);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);
            
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    msg.AddHeader(kvp.Key, kvp.Value);
                }
            }

            var response = await _sendGridClient.SendEmailAsync(msg);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent to {Recipient}: {Subject}", recipient, subject);
                return ApiResponse.SuccessResponse("Email sent successfully");
            }

            var errorBody = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid error: {StatusCode} - {Body}", response.StatusCode, errorBody);
            return ApiResponse.FailResponse($"Failed to send email: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Recipient}", recipient);
            return ApiResponse.FailResponse($"Error sending email: {ex.Message}");
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            return !string.IsNullOrEmpty(_settings.ApiKey);
        }
        catch
        {
            return false;
        }
    }
}