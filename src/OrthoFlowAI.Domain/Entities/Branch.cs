using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Domain.Entities;

public class Branch : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsMainBranch { get; set; }
    public string? OperatingHours { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<Staff> Staff { get; set; } = new List<Staff>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
}
