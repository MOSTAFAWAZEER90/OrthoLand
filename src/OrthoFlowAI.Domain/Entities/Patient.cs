using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class Patient : TenantEntity
{
    public string? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsuranceNumber { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentPlanDescription { get; set; }
    public TreatmentType? TreatmentType { get; set; }
    public DateTime? TreatmentStartDate { get; set; }
    public DateTime? EstimatedEndDate { get; set; }
    public PatientStatus Status { get; set; } = PatientStatus.Active;
    public string? AssignedDoctorId { get; set; }
    public string? BranchId { get; set; }
    public string? Tags { get; set; }
    public double JourneyScore { get; set; }
    public double ComplianceScore { get; set; }
    public decimal TotalTreatmentCost { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRemaining { get; set; }
    public string? Notes { get; set; }
    public string? QRCode { get; set; }
    public string? DeviceToken { get; set; }

    public Doctor? Doctor { get; set; }
    public Branch? Branch { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<PainReport> PainReports { get; set; } = new List<PainReport>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<TreatmentVisit> TreatmentVisits { get; set; } = new List<TreatmentVisit>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public ICollection<Referral> Referrals { get; set; } = new List<Referral>();
}
