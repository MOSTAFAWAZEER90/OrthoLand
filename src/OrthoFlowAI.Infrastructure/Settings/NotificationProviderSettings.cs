namespace OrthoFlowAI.Infrastructure.Settings;

public class TwilioSettings
{
    public const string SectionName = "Twilio";
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
    public string WhatsAppFromNumber { get; set; } = string.Empty;
    public string WhatsAppTemplateSid { get; set; } = string.Empty;
}

public class PushNotificationSettings
{
    public const string SectionName = "PushNotifications";
    public string Provider { get; set; } = "Firebase"; // Firebase, OneSignal
    public string FirebaseProjectId { get; set; } = string.Empty;
    public string FirebasePrivateKey { get; set; } = string.Empty;
    public string FirebaseClientEmail { get; set; } = string.Empty;
    public string OneSignalAppId { get; set; } = string.Empty;
    public string OneSignalApiKey { get; set; } = string.Empty;
}

public class HangfireSettings
{
    public const string SectionName = "Hangfire";
    public string DashboardPath { get; set; } = "/hangfire";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
    public bool EnableDashboard { get; set; } = true;
}