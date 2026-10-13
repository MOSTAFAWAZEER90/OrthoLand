using Microsoft.AspNetCore.Http;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Interfaces;
using OrthoFlowAI.Infrastructure.Data;

namespace OrthoFlowAI.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private IRepository<Clinic>? _clinics;
    private IRepository<ApplicationUser>? _applicationUsers;
    private IRepository<Doctor>? _doctors;
    private IRepository<Patient>? _patients;
    private IRepository<Branch>? _branches;
    private IRepository<Appointment>? _appointments;
    private IRepository<TreatmentVisit>? _treatmentVisits;
    private IRepository<Photo>? _photos;
    private IRepository<Invoice>? _invoices;
    private IRepository<Payment>? _payments;
    private IRepository<Document>? _documents;
    private IRepository<Notification>? _notifications;
    private IRepository<PainReport>? _painReports;
    private IRepository<EmergencyRequest>? _emergencyRequests;
    private IRepository<InventoryItem>? _inventoryItems;
    private IRepository<Staff>? _staff;
    private IRepository<ChatMessage>? _chatMessages;
    private IRepository<Referral>? _referrals;
    private IRepository<Subscription>? _subscriptions;
    private IRepository<Feedback>? _feedbacks;
    private IRepository<AuditLog>? _auditLogs;

    public UnitOfWork(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public IRepository<Clinic> Clinics => _clinics ??= new GenericRepository<Clinic>(_context);
    public IRepository<ApplicationUser> ApplicationUsers => _applicationUsers ??= new GenericRepository<ApplicationUser>(_context);
    public IRepository<Doctor> Doctors => _doctors ??= new GenericRepository<Doctor>(_context);
    public IRepository<Patient> Patients => _patients ??= new GenericRepository<Patient>(_context);
    public IRepository<Branch> Branches => _branches ??= new GenericRepository<Branch>(_context);
    public IRepository<Appointment> Appointments => _appointments ??= new GenericRepository<Appointment>(_context);
    public IRepository<TreatmentVisit> TreatmentVisits => _treatmentVisits ??= new GenericRepository<TreatmentVisit>(_context);
    public IRepository<Photo> Photos => _photos ??= new GenericRepository<Photo>(_context);
    public IRepository<Invoice> Invoices => _invoices ??= new GenericRepository<Invoice>(_context);
    public IRepository<Payment> Payments => _payments ??= new GenericRepository<Payment>(_context);
    public IRepository<Document> Documents => _documents ??= new GenericRepository<Document>(_context);
    public IRepository<Notification> Notifications => _notifications ??= new GenericRepository<Notification>(_context);
    public IRepository<PainReport> PainReports => _painReports ??= new GenericRepository<PainReport>(_context);
    public IRepository<EmergencyRequest> EmergencyRequests => _emergencyRequests ??= new GenericRepository<EmergencyRequest>(_context);
    public IRepository<InventoryItem> InventoryItems => _inventoryItems ??= new GenericRepository<InventoryItem>(_context);
    public IRepository<Staff> Staff => _staff ??= new GenericRepository<Staff>(_context);
    public IRepository<ChatMessage> ChatMessages => _chatMessages ??= new GenericRepository<ChatMessage>(_context);
    public IRepository<Referral> Referrals => _referrals ??= new GenericRepository<Referral>(_context);
    public IRepository<Subscription> Subscriptions => _subscriptions ??= new GenericRepository<Subscription>(_context);
    public IRepository<Feedback> Feedbacks => _feedbacks ??= new GenericRepository<Feedback>(_context);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new GenericRepository<AuditLog>(_context);

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public string GetCurrentClinicId()
    {
        return _httpContextAccessor.HttpContext?.Items["ClinicId"]?.ToString() ?? string.Empty;
    }

    public string GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
