namespace OrthoFlowAI.Infrastructure.Settings;

public sealed class OpenAISettings
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
}
