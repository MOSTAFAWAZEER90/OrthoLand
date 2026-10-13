using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string? UserId { get; set; }
    public string? ClinicId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
