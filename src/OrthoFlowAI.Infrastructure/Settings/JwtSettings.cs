namespace OrthoFlowAI.Infrastructure.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; } = 60;
    public int RefreshExpirationInDays { get; set; } = 7;
}
