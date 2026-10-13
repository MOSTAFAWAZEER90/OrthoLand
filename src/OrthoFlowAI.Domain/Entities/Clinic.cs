using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Domain.Entities;

public class Clinic : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? BusinessHours { get; set; }
    public string? WorkingDays { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? SubscriptionPlan { get; set; }
    public string? SubscriptionStatus { get; set; }
    public string Language { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ApplicationUser> Doctors { get; set; } = new List<ApplicationUser>();
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<Staff> Staff { get; set; } = new List<Staff>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
}
