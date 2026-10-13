using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Domain.Entities;

public class ChatMessage : TenantEntity
{
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string? PatientId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool TypingIndicator { get; set; }

    public ApplicationUser Sender { get; set; } = null!;
    public ApplicationUser Receiver { get; set; } = null!;
}
