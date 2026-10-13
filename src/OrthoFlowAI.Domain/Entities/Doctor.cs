using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Domain.Entities;

public class Doctor : TenantEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Bio { get; set; }
    public int YearsOfExperience { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsAcceptingPatients { get; set; } = true;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<TreatmentVisit> TreatmentPlans { get; set; } = new List<TreatmentVisit>();
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
