using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrthoFlowAI.Infrastructure.Settings;

namespace OrthoFlowAI.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SendGridSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SendGridSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        return await SendEmailAsync(to, subject, htmlBody, _settings.FromEmail, _settings.FromName);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string fromEmail, string fromName)
    {
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            var sent = await SendViaSendGridAsync(to, subject, htmlBody, fromEmail, fromName);
            if (sent) return true;

            _logger.LogWarning("SendGrid delivery failed. Falling back to SMTP.");
        }

        return await SendViaSmtpAsync(to, subject, htmlBody, fromEmail, fromName);
    }

    public async Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string htmlBody)
    {
        var results = new List<bool>();
        foreach (var recipient in recipients)
        {
            results.Add(await SendEmailAsync(recipient, subject, htmlBody));
        }
        return results.All(r => r);
    }

    private async Task<bool> SendViaSendGridAsync(string to, string subject, string htmlBody, string? fromEmail, string? fromName)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var payload = new
            {
                personalizations = new[]
                {
                    new { to = new[] { new { email = to } } }
                },
                from = new { email = fromEmail ?? _settings.FromEmail, name = fromName ?? _settings.FromName },
                subject = subject,
                content = new[] { new { type = "text/html", value = htmlBody } }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.sendgrid.com/v3/mail/send", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent to {To} via SendGrid", to);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("SendGrid returned {StatusCode}: {Body}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via SendGrid to {To}", to);
            return false;
        }
    }

    private async Task<bool> SendViaSmtpAsync(string to, string subject, string htmlBody, string? fromEmail, string? fromName)
    {
        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail ?? _settings.FromEmail, fromName ?? _settings.FromName);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient
            {
                EnableSsl = true,
                UseDefaultCredentials = false
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To} via SMTP", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via SMTP to {To}", to);
            return false;
        }
    }
}
