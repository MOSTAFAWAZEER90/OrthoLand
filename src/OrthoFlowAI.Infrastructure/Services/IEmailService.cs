namespace OrthoFlowAI.Infrastructure.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string fromEmail, string fromName);
    Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string htmlBody);
}

public class EmailRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
}
