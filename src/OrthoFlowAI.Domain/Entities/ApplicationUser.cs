using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class ApplicationUser : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ProfileImage { get; set; }
    public UserRole Role { get; set; }
    public string? ClinicId { get; set; }
    public string? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public Enums.Language? Language { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }

    public Clinic? Clinic { get; set; }
    public Branch? Branch { get; set; }
    public Doctor? DoctorProfile { get; set; }
    public Staff? StaffProfile { get; set; }
}
