using Microsoft.AspNetCore.Identity;

namespace OrthoFlowAI.Infrastructure.Identity;

public class ApplicationIdentityUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public string? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? Language { get; set; }
}
