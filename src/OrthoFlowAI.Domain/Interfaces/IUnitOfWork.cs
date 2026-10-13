using Microsoft.AspNetCore.Http;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Clinic> Clinics { get; }
    IRepository<ApplicationUser> ApplicationUsers { get; }
    IRepository<Doctor> Doctors { get; }
    IRepository<Patient> Patients { get; }
    IRepository<Branch> Branches { get; }
    IRepository<Appointment> Appointments { get; }
    IRepository<TreatmentVisit> TreatmentVisits { get; }
    IRepository<Photo> Photos { get; }
    IRepository<Invoice> Invoices { get; }
    IRepository<Payment> Payments { get; }
    IRepository<Document> Documents { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<PainReport> PainReports { get; }
    IRepository<EmergencyRequest> EmergencyRequests { get; }
    IRepository<InventoryItem> InventoryItems { get; }
    IRepository<Staff> Staff { get; }
    IRepository<ChatMessage> ChatMessages { get; }
    IRepository<Referral> Referrals { get; }
    IRepository<Subscription> Subscriptions { get; }
    IRepository<Feedback> Feedbacks { get; }
    IRepository<AuditLog> AuditLogs { get; }

    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    string GetCurrentClinicId();
    string GetCurrentUserId();
}
