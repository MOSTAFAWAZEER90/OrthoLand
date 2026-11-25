using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrthoFlowAI.Application.Messaging;
using OrthoFlowAI.Domain.Common;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace OrthoFlowAI.Infrastructure.WhatsApp;

public class WhatsAppSettings
{
    public const string SectionName = "WhatsApp";

    /// <summary>"" (disabled), "Meta" (WhatsApp Cloud API), "Twilio", or "Log" (development: only logs).</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Country code used for local numbers such as 010xxxxxxxx.</summary>
    public string DefaultCountryCode { get; set; } = PhoneNumbers.EgyptCountryCode;

    public MetaWhatsAppSettings Meta { get; set; } = new();
    public string TwilioFromNumber { get; set; } = string.Empty;
}

public class MetaWhatsAppSettings
{
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v21.0";
    public string BaseUrl { get; set; } = "https://graph.facebook.com";

    /// <summary>
    /// Approved template name per message kind, e.g. { "AppointmentReminder": "appointment_reminder_ar" }.
    /// Businesses may only start a conversation with an approved template; a kind without
    /// a template is sent as plain text, which Meta only delivers inside a 24h customer window.
    /// </summary>
    public Dictionary<string, string> Templates { get; set; } = new();
}

/// <summary>Sends a WhatsApp message to a number in international digits form.</summary>
public interface IWhatsAppSender
{
    bool IsConfigured { get; }
    Task<ApiResponse> SendAsync(string internationalDigits, PatientMessage message, CancellationToken ct = default);
}

/// <summary>Used when WhatsApp:Provider is empty.</summary>
public sealed class DisabledWhatsAppSender : IWhatsAppSender
{
    public bool IsConfigured => false;
    public Task<ApiResponse> SendAsync(string to, PatientMessage message, CancellationToken ct = default) =>
        Task.FromResult(ApiResponse.FailResponse("WhatsApp is not configured (set WhatsApp:Provider)."));
}

/// <summary>Development provider: logs the message instead of sending it.</summary>
public sealed class LogWhatsAppSender(ILogger<LogWhatsAppSender> logger) : IWhatsAppSender
{
    public bool IsConfigured => true;

    public Task<ApiResponse> SendAsync(string to, PatientMessage message, CancellationToken ct = default)
    {
        logger.LogInformation("[WhatsApp:Log] to +{To} ({Kind}, {Lang}):\n{Text}", to, message.Kind, message.Language, message.Text);
        return Task.FromResult(ApiResponse.SuccessResponse("Logged (WhatsApp provider = Log)"));
    }
}

/// <summary>WhatsApp Business Cloud API (graph.facebook.com/{version}/{phone-number-id}/messages).</summary>
public sealed class MetaWhatsAppSender(HttpClient http, IOptions<WhatsAppSettings> options, ILogger<MetaWhatsAppSender> logger)
    : IWhatsAppSender
{
    private readonly MetaWhatsAppSettings _meta = options.Value.Meta;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_meta.PhoneNumberId) && !string.IsNullOrWhiteSpace(_meta.AccessToken);

    public async Task<ApiResponse> SendAsync(string to, PatientMessage message, CancellationToken ct = default)
    {
        if (!IsConfigured) return ApiResponse.FailResponse("WhatsApp Cloud API is not configured.");

        var body = BuildPayload(to, message, _meta);
        var url = $"{_meta.BaseUrl.TrimEnd('/')}/{_meta.ApiVersion}/{_meta.PhoneNumberId}/messages";

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _meta.AccessToken);

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("WhatsApp {Kind} sent to +{To}", message.Kind, to);
                return ApiResponse.SuccessResponse("WhatsApp message sent");
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("WhatsApp Cloud API returned {Status}: {Error}", (int)response.StatusCode, error);
            return ApiResponse.FailResponse($"WhatsApp error {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "WhatsApp Cloud API request failed");
            return ApiResponse.FailResponse("WhatsApp request failed: " + ex.Message);
        }
    }

    /// <summary>Template message when one is configured for the kind, otherwise plain text.</summary>
    public static object BuildPayload(string to, PatientMessage message, MetaWhatsAppSettings meta)
    {
        if (meta.Templates.TryGetValue(message.Kind.ToString(), out var template) && !string.IsNullOrWhiteSpace(template))
        {
            return new
            {
                messaging_product = "whatsapp",
                to,
                type = "template",
                template = new
                {
                    name = template,
                    language = new { code = message.Language },
                    components = new[]
                    {
                        new
                        {
                            type = "body",
                            parameters = message.TemplateParameters.Select(p => new { type = "text", text = p }).ToArray(),
                        },
                    },
                },
            };
        }

        return new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { preview_url = false, body = message.Text },
        };
    }
}

/// <summary>WhatsApp through Twilio (whatsapp:+number).</summary>
public sealed class TwilioWhatsAppSender(IOptions<WhatsAppSettings> options, IOptions<Settings.TwilioSettings> twilio,
    ILogger<TwilioWhatsAppSender> logger) : IWhatsAppSender
{
    private readonly string _from = string.IsNullOrWhiteSpace(options.Value.TwilioFromNumber)
        ? twilio.Value.WhatsAppFromNumber
        : options.Value.TwilioFromNumber;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_from)
        && !string.IsNullOrWhiteSpace(twilio.Value.AccountSid)
        && !string.IsNullOrWhiteSpace(twilio.Value.AuthToken);

    public async Task<ApiResponse> SendAsync(string to, PatientMessage message, CancellationToken ct = default)
    {
        if (!IsConfigured) return ApiResponse.FailResponse("Twilio WhatsApp is not configured.");
        try
        {
            var from = _from.StartsWith("whatsapp:") ? _from : "whatsapp:" + _from;
            var sent = await MessageResource.CreateAsync(new CreateMessageOptions(new PhoneNumber($"whatsapp:+{to}"))
            {
                From = new PhoneNumber(from),
                Body = message.Text,
            });
            logger.LogInformation("Twilio WhatsApp {Kind} sent to +{To}: {Sid}", message.Kind, to, sent.Sid);
            return ApiResponse.SuccessResponse("WhatsApp message sent");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Twilio WhatsApp send failed");
            return ApiResponse.FailResponse("WhatsApp error: " + ex.Message);
        }
    }
}
