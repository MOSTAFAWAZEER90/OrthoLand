using Microsoft.Extensions.Options;
using OrthoFlowAI.Application.Messaging;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Infrastructure.WhatsApp;

/// <summary>
/// The WhatsApp notification channel. Uses whichever <see cref="IWhatsAppSender"/> is
/// configured (Meta Cloud API, Twilio or Log) and normalises Egyptian phone numbers.
/// </summary>
public sealed class WhatsAppNotificationChannel(IWhatsAppSender sender, IOptions<WhatsAppSettings> options) : INotificationChannel
{
    // Metadata keys used to carry the structured message (needed for WhatsApp templates).
    public const string KindKey = "message_kind";
    public const string LanguageKey = "message_language";
    public const string ParamsKey = "template_params";
    private const char ParamSeparator = '\u001F';

    public NotificationChannel Channel => NotificationChannel.WhatsApp;

    public Task<bool> IsAvailableAsync() => Task.FromResult(sender.IsConfigured);

    public Task<ApiResponse> SendAsync(string recipient, string subject, string body, Dictionary<string, string>? metadata = null)
    {
        var to = PhoneNumbers.ToInternationalDigits(recipient, options.Value.DefaultCountryCode);
        if (to is null)
            return Task.FromResult(ApiResponse.FailResponse($"Invalid phone number for WhatsApp: {recipient}"));

        return sender.SendAsync(to, FromMetadata(subject, body, metadata));
    }

    public static Dictionary<string, string> ToMetadata(PatientMessage message) => new()
    {
        ["channel"] = "whatsapp",
        [KindKey] = message.Kind.ToString(),
        [LanguageKey] = message.Language,
        [ParamsKey] = string.Join(ParamSeparator, message.TemplateParameters),
    };

    public static PatientMessage FromMetadata(string subject, string body, Dictionary<string, string>? metadata)
    {
        var kind = metadata != null && metadata.TryGetValue(KindKey, out var k) && Enum.TryParse<PatientMessageKind>(k, out var parsed)
            ? parsed
            : PatientMessageKind.AppointmentReminder;
        var language = metadata != null && metadata.TryGetValue(LanguageKey, out var l) ? l : "ar";
        var parameters = metadata != null && metadata.TryGetValue(ParamsKey, out var p) && p.Length > 0
            ? p.Split(ParamSeparator)
            : Array.Empty<string>();
        return new PatientMessage(kind, language, subject, body, parameters);
    }
}
