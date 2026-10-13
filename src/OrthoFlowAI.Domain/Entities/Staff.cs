using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class Staff : TenantEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? ScheduleJson { get; set; }
    public string? PermissionsJson { get; set; }
    public StaffStatus Status { get; set; } = StaffStatus.Active;
    public DateTime? HireDate { get; set; }
    public decimal? Salary { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Branch? Branch { get; set; }
}
