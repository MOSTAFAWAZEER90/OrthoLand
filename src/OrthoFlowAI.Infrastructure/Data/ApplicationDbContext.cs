using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Infrastructure.Identity;

namespace OrthoFlowAI.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationIdentityUser>
{
    private string? _clinicId;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public void SetClinicId(string clinicId) => _clinicId = clinicId;

    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<TreatmentVisit> TreatmentVisits => Set<TreatmentVisit>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PainReport> PainReports => Set<PainReport>();
    public DbSet<EmergencyRequest> EmergencyRequests => Set<EmergencyRequest>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                ApplyTenantFilter(builder, entityType.ClrType);
            }

            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                ApplySoftDeleteFilter(builder, entityType.ClrType);
            }
        }

        builder.Entity<Clinic>(e =>
        {
            e.ToTable("clinics");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(c => c.Logo).HasColumnName("logo");
            e.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(c => c.Email).HasColumnName("email").HasMaxLength(200);
            e.Property(c => c.Address).HasColumnName("address").HasMaxLength(500);
            e.Property(c => c.City).HasColumnName("city").HasMaxLength(100);
            e.Property(c => c.Country).HasColumnName("country").HasMaxLength(100);
            e.Property(c => c.Website).HasColumnName("website").HasMaxLength(300);
            e.Property(c => c.BusinessHours).HasColumnName("business_hours");
            e.Property(c => c.WorkingDays).HasColumnName("working_days");
            e.Property(c => c.PrimaryColor).HasColumnName("primary_color").HasMaxLength(20);
            e.Property(c => c.SecondaryColor).HasColumnName("secondary_color").HasMaxLength(20);
            e.Property(c => c.SubscriptionPlan).HasColumnName("subscription_plan").HasMaxLength(50);
            e.Property(c => c.SubscriptionStatus).HasColumnName("subscription_status").HasMaxLength(50);
            e.Property(c => c.Language).HasColumnName("language").HasMaxLength(10);
            e.Property(c => c.IsActive).HasColumnName("is_active");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            e.Property(c => c.IsDeleted).HasColumnName("is_deleted");
            e.Property(c => c.CreatedBy).HasColumnName("created_by");
            e.Property(c => c.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(c => c.Name);
            e.HasIndex(c => c.Email);
            e.HasIndex(c => c.IsActive);
            e.HasIndex(c => c.IsDeleted);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("application_users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            e.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(u => u.ProfileImage).HasColumnName("profile_image");
            e.Property(u => u.Role).HasColumnName("role").HasConversion<string>();
            e.Property(u => u.ClinicId).HasColumnName("clinic_id");
            e.Property(u => u.BranchId).HasColumnName("branch_id");
            e.Property(u => u.IsActive).HasColumnName("is_active");
            e.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
            e.Property(u => u.Language).HasColumnName("language").HasConversion<string?>().HasMaxLength(20);
            e.Property(u => u.DateOfBirth).HasColumnName("date_of_birth");
            e.Property(u => u.Gender).HasColumnName("gender").HasConversion<string?>().HasMaxLength(20);
            e.Property(u => u.CreatedAt).HasColumnName("created_at");
            e.Property(u => u.UpdatedAt).HasColumnName("updated_at");
            e.Property(u => u.IsDeleted).HasColumnName("is_deleted");
            e.Property(u => u.CreatedBy).HasColumnName("created_by");
            e.Property(u => u.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(u => u.ClinicId);
            e.HasIndex(u => u.BranchId);
            e.HasIndex(u => u.Role);
            e.HasIndex(u => u.IsActive);
            e.HasIndex(u => u.IsDeleted);

            e.HasOne(u => u.Clinic)
                .WithMany(c => c.Doctors)
                .HasForeignKey(u => u.ClinicId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(u => u.Branch)
                .WithMany()
                .HasForeignKey(u => u.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Doctor>(e =>
        {
            e.ToTable("doctors");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.UserId).HasColumnName("user_id");
            e.Property(d => d.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(d => d.Specialty).HasColumnName("specialty").HasMaxLength(200);
            e.Property(d => d.LicenseNumber).HasColumnName("license_number").HasMaxLength(100);
            e.Property(d => d.Bio).HasColumnName("bio");
            e.Property(d => d.YearsOfExperience).HasColumnName("years_of_experience");
            e.Property(d => d.ConsultationFee).HasColumnName("consultation_fee").HasColumnType("decimal(18,2)");
            e.Property(d => d.IsAcceptingPatients).HasColumnName("is_accepting_patients");
            e.Property(d => d.CreatedAt).HasColumnName("created_at");
            e.Property(d => d.UpdatedAt).HasColumnName("updated_at");
            e.Property(d => d.IsDeleted).HasColumnName("is_deleted");
            e.Property(d => d.CreatedBy).HasColumnName("created_by");
            e.Property(d => d.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(d => d.UserId);
            e.HasIndex(d => d.ClinicId);
            e.HasIndex(d => d.LicenseNumber);
            e.HasIndex(d => d.IsDeleted);

            e.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(d => d.Clinic)
                .WithMany()
                .HasForeignKey(d => d.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Patient>(e =>
        {
            e.ToTable("patients");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.UserId).HasColumnName("user_id");
            e.Property(p => p.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(p => p.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            e.Property(p => p.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            e.Property(p => p.Email).HasColumnName("email").HasMaxLength(200);
            e.Property(p => p.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(p => p.DateOfBirth).HasColumnName("date_of_birth");
            e.Property(p => p.Gender).HasColumnName("gender").HasConversion<string?>().HasMaxLength(20);
            e.Property(p => p.Address).HasColumnName("address").HasMaxLength(500);
            e.Property(p => p.City).HasColumnName("city").HasMaxLength(100);
            e.Property(p => p.EmergencyContactName).HasColumnName("emergency_contact_name").HasMaxLength(200);
            e.Property(p => p.EmergencyContactPhone).HasColumnName("emergency_contact_phone").HasMaxLength(50);
            e.Property(p => p.InsuranceProvider).HasColumnName("insurance_provider").HasMaxLength(200);
            e.Property(p => p.InsuranceNumber).HasColumnName("insurance_number").HasMaxLength(100);
            e.Property(p => p.Diagnosis).HasColumnName("diagnosis");
            e.Property(p => p.TreatmentPlanDescription).HasColumnName("treatment_plan_description");
            e.Property(p => p.TreatmentType).HasColumnName("treatment_type").HasConversion<string?>().HasMaxLength(50);
            e.Property(p => p.TreatmentStartDate).HasColumnName("treatment_start_date");
            e.Property(p => p.EstimatedEndDate).HasColumnName("estimated_end_date");
            e.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50);
            e.Property(p => p.AssignedDoctorId).HasColumnName("assigned_doctor_id");
            e.Property(p => p.BranchId).HasColumnName("branch_id");
            e.Property(p => p.Tags).HasColumnName("tags").HasMaxLength(500);
            e.Property(p => p.JourneyScore).HasColumnName("journey_score");
            e.Property(p => p.ComplianceScore).HasColumnName("compliance_score");
            e.Property(p => p.TotalTreatmentCost).HasColumnName("total_treatment_cost").HasColumnType("decimal(18,2)");
            e.Property(p => p.AmountPaid).HasColumnName("amount_paid").HasColumnType("decimal(18,2)");
            e.Property(p => p.AmountRemaining).HasColumnName("amount_remaining").HasColumnType("decimal(18,2)");
            e.Property(p => p.Notes).HasColumnName("notes");
            e.Property(p => p.QRCode).HasColumnName("qr_code");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.Property(p => p.IsDeleted).HasColumnName("is_deleted");
            e.Property(p => p.CreatedBy).HasColumnName("created_by");
            e.Property(p => p.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(p => p.ClinicId);
            e.HasIndex(p => p.AssignedDoctorId);
            e.HasIndex(p => p.BranchId);
            e.HasIndex(p => p.Status);
            e.HasIndex(p => new { p.FirstName, p.LastName });
            e.HasIndex(p => p.Email);
            e.HasIndex(p => p.IsDeleted);

            e.HasOne(p => p.Doctor)
                .WithMany(d => d.Patients)
                .HasForeignKey(p => p.AssignedDoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.Branch)
                .WithMany(b => b.Patients)
                .HasForeignKey(p => p.BranchId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.Clinic)
                .WithMany(c => c.Patients)
                .HasForeignKey(p => p.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Branch>(e =>
        {
            e.ToTable("branches");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("id");
            e.Property(b => b.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(b => b.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(b => b.Address).HasColumnName("address").HasMaxLength(500);
            e.Property(b => b.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(b => b.Email).HasColumnName("email").HasMaxLength(200);
            e.Property(b => b.IsMainBranch).HasColumnName("is_main_branch");
            e.Property(b => b.OperatingHours).HasColumnName("operating_hours");
            e.Property(b => b.CreatedAt).HasColumnName("created_at");
            e.Property(b => b.UpdatedAt).HasColumnName("updated_at");
            e.Property(b => b.IsDeleted).HasColumnName("is_deleted");
            e.Property(b => b.CreatedBy).HasColumnName("created_by");
            e.Property(b => b.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(b => b.ClinicId);
            e.HasIndex(b => b.IsDeleted);

            e.HasOne(b => b.Clinic)
                .WithMany(c => c.Branches)
                .HasForeignKey(b => b.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Appointment>(e =>
        {
            e.ToTable("appointments");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(a => a.PatientId).HasColumnName("patient_id").IsRequired();
            e.Property(a => a.DoctorId).HasColumnName("doctor_id").IsRequired();
            e.Property(a => a.BranchId).HasColumnName("branch_id");
            e.Property(a => a.AppointmentDate).HasColumnName("appointment_date");
            e.Property(a => a.EndTime).HasColumnName("end_time");
            e.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50);
            e.Property(a => a.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50);
            e.Property(a => a.Notes).HasColumnName("notes");
            e.Property(a => a.Duration).HasColumnName("duration");
            e.Property(a => a.Color).HasColumnName("color").HasMaxLength(20);
            e.Property(a => a.IsRecurring).HasColumnName("is_recurring");
            e.Property(a => a.RecurrencePattern).HasColumnName("recurrence_pattern").HasMaxLength(200);
            e.Property(a => a.RecurrenceEndDate).HasColumnName("recurrence_end_date");
            e.Property(a => a.CreatedAt).HasColumnName("created_at");
            e.Property(a => a.UpdatedAt).HasColumnName("updated_at");
            e.Property(a => a.IsDeleted).HasColumnName("is_deleted");
            e.Property(a => a.CreatedBy).HasColumnName("created_by");
            e.Property(a => a.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(a => a.ClinicId);
            e.HasIndex(a => a.PatientId);
            e.HasIndex(a => a.DoctorId);
            e.HasIndex(a => a.BranchId);
            e.HasIndex(a => a.AppointmentDate);
            e.HasIndex(a => a.Status);
            e.HasIndex(a => a.IsDeleted);

            e.HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Branch)
                .WithMany(b => b.Appointments)
                .HasForeignKey(a => a.BranchId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(a => a.Clinic)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TreatmentVisit>(e =>
        {
            e.ToTable("treatment_visits");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(t => t.PatientId).HasColumnName("patient_id").IsRequired();
            e.Property(t => t.DoctorId).HasColumnName("doctor_id").IsRequired();
            e.Property(t => t.AppointmentId).HasColumnName("appointment_id");
            e.Property(t => t.VisitDate).HasColumnName("visit_date");
            e.Property(t => t.WireChanged).HasColumnName("wire_changed");
            e.Property(t => t.BracketReplaced).HasColumnName("bracket_replaced");
            e.Property(t => t.RubberBandsUsed).HasColumnName("rubber_bands_used");
            e.Property(t => t.PainLevel).HasColumnName("pain_level").HasConversion<string>().HasMaxLength(50);
            e.Property(t => t.DoctorNotes).HasColumnName("doctor_notes");
            e.Property(t => t.Medication).HasColumnName("medication").HasMaxLength(500);
            e.Property(t => t.NextAppointmentDate).HasColumnName("next_appointment_date");
            e.Property(t => t.PhotosJson).HasColumnName("photos_json");
            e.Property(t => t.AttachmentsJson).HasColumnName("attachments_json");
            e.Property(t => t.CreatedAt).HasColumnName("created_at");
            e.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            e.Property(t => t.IsDeleted).HasColumnName("is_deleted");
            e.Property(t => t.CreatedBy).HasColumnName("created_by");
            e.Property(t => t.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(t => t.ClinicId);
            e.HasIndex(t => t.PatientId);
            e.HasIndex(t => t.DoctorId);
            e.HasIndex(t => t.AppointmentId);
            e.HasIndex(t => t.IsDeleted);

            e.HasOne(t => t.Patient)
                .WithMany(p => p.TreatmentVisits)
                .HasForeignKey(t => t.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.Doctor)
                .WithMany(d => d.TreatmentPlans)
                .HasForeignKey(t => t.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.Appointment)
                .WithMany()
                .HasForeignKey(t => t.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.Clinic)
                .WithMany()
                .HasForeignKey(t => t.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Photo>(e =>
        {
            e.ToTable("photos");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(p => p.PatientId).HasColumnName("patient_id").IsRequired();
            e.Property(p => p.DoctorId).HasColumnName("doctor_id");
            e.Property(p => p.Url).HasColumnName("url").HasMaxLength(1000).IsRequired();
            e.Property(p => p.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
            e.Property(p => p.FileType).HasColumnName("file_type").HasMaxLength(50);
            e.Property(p => p.FileSize).HasColumnName("file_size");
            e.Property(p => p.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(50);
            e.Property(p => p.Description).HasColumnName("description");
            e.Property(p => p.VisitDate).HasColumnName("visit_date");
            e.Property(p => p.BeforePhotoUrl).HasColumnName("before_photo_url").HasMaxLength(1000);
            e.Property(p => p.AfterPhotoUrl).HasColumnName("after_photo_url").HasMaxLength(1000);
            e.Property(p => p.ComparisonScore).HasColumnName("comparison_score");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.Property(p => p.IsDeleted).HasColumnName("is_deleted");
            e.Property(p => p.CreatedBy).HasColumnName("created_by");
            e.Property(p => p.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(p => p.ClinicId);
            e.HasIndex(p => p.PatientId);
            e.HasIndex(p => p.DoctorId);
            e.HasIndex(p => p.Category);
            e.HasIndex(p => p.IsDeleted);

            e.HasOne(p => p.Patient)
                .WithMany(pt => pt.Photos)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.Clinic)
                .WithMany()
                .HasForeignKey(p => p.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Invoice>(e =>
        {
            e.ToTable("invoices");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(i => i.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(50).IsRequired();
            e.Property(i => i.PatientId).HasColumnName("patient_id").IsRequired();
            e.Property(i => i.DoctorId).HasColumnName("doctor_id");
            e.Property(i => i.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(18,2)");
            e.Property(i => i.PaidAmount).HasColumnName("paid_amount").HasColumnType("decimal(18,2)");
            e.Property(i => i.RemainingAmount).HasColumnName("remaining_amount").HasColumnType("decimal(18,2)");
            e.Property(i => i.Discount).HasColumnName("discount").HasColumnType("decimal(18,2)");
            e.Property(i => i.Tax).HasColumnName("tax").HasColumnType("decimal(18,2)");
            e.Property(i => i.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50);
            e.Property(i => i.PaymentStatus).HasColumnName("payment_status").HasConversion<string>().HasMaxLength(50);
            e.Property(i => i.DueDate).HasColumnName("due_date");
            e.Property(i => i.Notes).HasColumnName("notes");
            e.Property(i => i.ItemsJson).HasColumnName("items_json");
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.Property(i => i.UpdatedAt).HasColumnName("updated_at");
            e.Property(i => i.IsDeleted).HasColumnName("is_deleted");
            e.Property(i => i.CreatedBy).HasColumnName("created_by");
            e.Property(i => i.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(i => i.ClinicId);
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.HasIndex(i => i.PatientId);
            e.HasIndex(i => i.DoctorId);
            e.HasIndex(i => i.Status);
            e.HasIndex(i => i.IsDeleted);

            e.HasOne(i => i.Patient)
                .WithMany(p => p.Invoices)
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.Doctor)
                .WithMany()
                .HasForeignKey(i => i.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(i => i.Clinic)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(p => p.InvoiceId).HasColumnName("invoice_id").IsRequired();
            e.Property(p => p.PatientId).HasColumnName("patient_id").IsRequired();
            e.Property(p => p.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
            e.Property(p => p.PaymentMethod).HasColumnName("payment_method").HasConversion<string>().HasMaxLength(50);
            e.Property(p => p.TransactionId).HasColumnName("transaction_id").HasMaxLength(200);
            e.Property(p => p.Status).HasColumnName("status").HasMaxLength(50);
            e.Property(p => p.Notes).HasColumnName("notes");
            e.Property(p => p.PaymentDate).HasColumnName("payment_date");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.Property(p => p.IsDeleted).HasColumnName("is_deleted");
            e.Property(p => p.CreatedBy).HasColumnName("created_by");
            e.Property(p => p.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(p => p.ClinicId);
            e.HasIndex(p => p.InvoiceId);
            e.HasIndex(p => p.PatientId);
            e.HasIndex(p => p.IsDeleted);

            e.HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Clinic)
                .WithMany()
                .HasForeignKey(p => p.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(d => d.PatientId).HasColumnName("patient_id").IsRequired();
            e.Property(d => d.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
            e.Property(d => d.FileUrl).HasColumnName("file_url").HasMaxLength(1000).IsRequired();
            e.Property(d => d.FileType).HasColumnName("file_type").HasMaxLength(50);
            e.Property(d => d.FileSize).HasColumnName("file_size");
            e.Property(d => d.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50);
            e.Property(d => d.Description).HasColumnName("description");
            e.Property(d => d.IsSigned).HasColumnName("is_signed");
            e.Property(d => d.SignedAt).HasColumnName("signed_at");
            e.Property(d => d.CreatedAt).HasColumnName("created_at");
            e.Property(d => d.UpdatedAt).HasColumnName("updated_at");
            e.Property(d => d.IsDeleted).HasColumnName("is_deleted");
            e.Property(d => d.CreatedBy).HasColumnName("created_by");
            e.Property(d => d.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(d => d.ClinicId);
            e.HasIndex(d => d.PatientId);
            e.HasIndex(d => d.Type);
            e.HasIndex(d => d.IsDeleted);

            e.HasOne(d => d.Patient)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(d => d.Clinic)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(n => n.Id);
            e.Property(n => n.Id).HasColumnName("id");
            e.Property(n => n.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(n => n.UserId).HasColumnName("user_id").IsRequired();
            e.Property(n => n.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
            e.Property(n => n.Message).HasColumnName("message").IsRequired();
            e.Property(n => n.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50);
            e.Property(n => n.IsRead).HasColumnName("is_read");
            e.Property(n => n.ReadAt).HasColumnName("read_at");
            e.Property(n => n.Channel).HasColumnName("channel").HasConversion<string>().HasMaxLength(50);
            e.Property(n => n.MetadataJson).HasColumnName("metadata_json");
            e.Property(n => n.CreatedAt).HasColumnName("created_at");
            e.Property(n => n.UpdatedAt).HasColumnName("updated_at");
            e.Property(n => n.IsDeleted).HasColumnName("is_deleted");
            e.Property(n => n.CreatedBy).HasColumnName("created_by");
            e.Property(n => n.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(n => n.ClinicId);
            e.HasIndex(n => n.UserId);
            e.HasIndex(n => n.Type);
            e.HasIndex(n => n.IsRead);
            e.HasIndex(n => n.IsDeleted);

            e.HasOne(n => n.Clinic)
                .WithMany(c => c.Notifications)
                .HasForeignKey(n => n.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PainReport>(e =>
        {
            e.ToTable("pain_reports");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(p => p.PatientId).HasColumnName("patient_id").IsRequired();
            e.Property(p => p.ReportDate).HasColumnName("report_date");
            e.Property(p => p.PainLevel).HasColumnName("pain_level").HasConversion<string>().HasMaxLength(50);
            e.Property(p => p.HasBrokenWire).HasColumnName("has_broken_wire");
            e.Property(p => p.HasLooseBracket).HasColumnName("has_loose_bracket");
            e.Property(p => p.HasBleeding).HasColumnName("has_bleeding");
            e.Property(p => p.HasSwelling).HasColumnName("has_swelling");
            e.Property(p => p.Description).HasColumnName("description");
            e.Property(p => p.PhotoUrl).HasColumnName("photo_url").HasMaxLength(1000);
            e.Property(p => p.IsEmergency).HasColumnName("is_emergency");
            e.Property(p => p.DoctorNotified).HasColumnName("doctor_notified");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.Property(p => p.IsDeleted).HasColumnName("is_deleted");
            e.Property(p => p.CreatedBy).HasColumnName("created_by");
            e.Property(p => p.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(p => p.ClinicId);
            e.HasIndex(p => p.PatientId);
            e.HasIndex(p => p.IsEmergency);
            e.HasIndex(p => p.IsDeleted);

            e.HasOne(p => p.Patient)
                .WithMany(pt => pt.PainReports)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Clinic)
                .WithMany()
                .HasForeignKey(p => p.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EmergencyRequest>(e =>
        {
            e.ToTable("emergency_requests");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(r => r.PatientId).HasColumnName("patient_id").IsRequired();
            e.Property(r => r.DoctorId).HasColumnName("doctor_id");
            e.Property(r => r.Description).HasColumnName("description").IsRequired();
            e.Property(r => r.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(50);
            e.Property(r => r.PhotoUrlsJson).HasColumnName("photo_urls_json");
            e.Property(r => r.VideoUrl).HasColumnName("video_url").HasMaxLength(1000);
            e.Property(r => r.VoiceNoteUrl).HasColumnName("voice_note_url").HasMaxLength(1000);
            e.Property(r => r.Status).HasColumnName("status").HasMaxLength(50);
            e.Property(r => r.ResolvedAt).HasColumnName("resolved_at");
            e.Property(r => r.DoctorNotes).HasColumnName("doctor_notes");
            e.Property(r => r.CreatedAt).HasColumnName("created_at");
            e.Property(r => r.UpdatedAt).HasColumnName("updated_at");
            e.Property(r => r.IsDeleted).HasColumnName("is_deleted");
            e.Property(r => r.CreatedBy).HasColumnName("created_by");
            e.Property(r => r.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(r => r.ClinicId);
            e.HasIndex(r => r.PatientId);
            e.HasIndex(r => r.DoctorId);
            e.HasIndex(r => r.Severity);
            e.HasIndex(r => r.IsDeleted);

            e.HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Doctor)
                .WithMany()
                .HasForeignKey(r => r.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(r => r.Clinic)
                .WithMany()
                .HasForeignKey(r => r.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InventoryItem>(e =>
        {
            e.ToTable("inventory_items");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(i => i.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(i => i.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(50);
            e.Property(i => i.Brand).HasColumnName("brand").HasMaxLength(200);
            e.Property(i => i.Model).HasColumnName("model").HasMaxLength(200);
            e.Property(i => i.Quantity).HasColumnName("quantity");
            e.Property(i => i.MinQuantity).HasColumnName("min_quantity");
            e.Property(i => i.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,2)");
            e.Property(i => i.SupplierName).HasColumnName("supplier_name").HasMaxLength(200);
            e.Property(i => i.SupplierPhone).HasColumnName("supplier_phone").HasMaxLength(50);
            e.Property(i => i.LastRestockedAt).HasColumnName("last_restocked_at");
            e.Property(i => i.BranchId).HasColumnName("branch_id");
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.Property(i => i.UpdatedAt).HasColumnName("updated_at");
            e.Property(i => i.IsDeleted).HasColumnName("is_deleted");
            e.Property(i => i.CreatedBy).HasColumnName("created_by");
            e.Property(i => i.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(i => i.ClinicId);
            e.HasIndex(i => i.BranchId);
            e.HasIndex(i => i.Category);
            e.HasIndex(i => i.IsDeleted);

            e.HasOne(i => i.Branch)
                .WithMany(b => b.Inventory)
                .HasForeignKey(i => i.BranchId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(i => i.Clinic)
                .WithMany(c => c.Inventory)
                .HasForeignKey(i => i.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Staff>(e =>
        {
            e.ToTable("staff");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
            e.Property(s => s.Position).HasColumnName("position").HasMaxLength(200);
            e.Property(s => s.Department).HasColumnName("department").HasMaxLength(200);
            e.Property(s => s.ScheduleJson).HasColumnName("schedule_json");
            e.Property(s => s.PermissionsJson).HasColumnName("permissions_json");
            e.Property(s => s.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50);
            e.Property(s => s.HireDate).HasColumnName("hire_date");
            e.Property(s => s.Salary).HasColumnName("salary").HasColumnType("decimal(18,2)");
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            e.Property(s => s.IsDeleted).HasColumnName("is_deleted");
            e.Property(s => s.CreatedBy).HasColumnName("created_by");
            e.Property(s => s.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(s => s.ClinicId);
            e.HasIndex(s => s.UserId);
            e.HasIndex(s => s.IsDeleted);

            e.HasOne(s => s.User)
                .WithOne(u => u.StaffProfile)
                .HasForeignKey<Staff>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Clinic)
                .WithMany(c => c.Staff)
                .HasForeignKey(s => s.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChatMessage>(e =>
        {
            e.ToTable("chat_messages");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(m => m.SenderId).HasColumnName("sender_id").IsRequired();
            e.Property(m => m.ReceiverId).HasColumnName("receiver_id").IsRequired();
            e.Property(m => m.PatientId).HasColumnName("patient_id");
            e.Property(m => m.Message).HasColumnName("message").IsRequired();
            e.Property(m => m.FileUrl).HasColumnName("file_url").HasMaxLength(1000);
            e.Property(m => m.IsRead).HasColumnName("is_read");
            e.Property(m => m.ReadAt).HasColumnName("read_at");
            e.Property(m => m.TypingIndicator).HasColumnName("typing_indicator");
            e.Property(m => m.CreatedAt).HasColumnName("created_at");
            e.Property(m => m.UpdatedAt).HasColumnName("updated_at");
            e.Property(m => m.IsDeleted).HasColumnName("is_deleted");
            e.Property(m => m.CreatedBy).HasColumnName("created_by");
            e.Property(m => m.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(m => m.ClinicId);
            e.HasIndex(m => m.SenderId);
            e.HasIndex(m => m.ReceiverId);
            e.HasIndex(m => m.PatientId);
            e.HasIndex(m => m.IsDeleted);

            e.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Clinic)
                .WithMany()
                .HasForeignKey(m => m.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Referral>(e =>
        {
            e.ToTable("referrals");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(r => r.ReferrerPatientId).HasColumnName("referrer_patient_id").IsRequired();
            e.Property(r => r.ReferredPatientId).HasColumnName("referred_patient_id").IsRequired();
            e.Property(r => r.ReferralCode).HasColumnName("referral_code").HasMaxLength(50);
            e.Property(r => r.ReferrerDiscount).HasColumnName("referrer_discount").HasColumnType("decimal(18,2)");
            e.Property(r => r.ReferredDiscount).HasColumnName("referred_discount").HasColumnType("decimal(18,2)");
            e.Property(r => r.ReferrerReward).HasColumnName("referrer_reward").HasColumnType("decimal(18,2)");
            e.Property(r => r.Status).HasColumnName("status").HasMaxLength(50);
            e.Property(r => r.ReferredAt).HasColumnName("referred_at");
            e.Property(r => r.CreatedAt).HasColumnName("created_at");
            e.Property(r => r.UpdatedAt).HasColumnName("updated_at");
            e.Property(r => r.IsDeleted).HasColumnName("is_deleted");
            e.Property(r => r.CreatedBy).HasColumnName("created_by");
            e.Property(r => r.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(r => r.ClinicId);
            e.HasIndex(r => r.ReferrerPatientId);
            e.HasIndex(r => r.ReferredPatientId);
            e.HasIndex(r => r.ReferralCode);
            e.HasIndex(r => r.IsDeleted);

            e.HasOne(r => r.ReferrerPatient)
                .WithMany(p => p.Referrals)
                .HasForeignKey(r => r.ReferrerPatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.ReferredPatient)
                .WithMany()
                .HasForeignKey(r => r.ReferredPatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Clinic)
                .WithMany()
                .HasForeignKey(r => r.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Subscription>(e =>
        {
            e.ToTable("subscriptions");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(s => s.Plan).HasColumnName("plan").HasConversion<string>().HasMaxLength(50);
            e.Property(s => s.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50);
            e.Property(s => s.BillingPeriod).HasColumnName("billing_period").HasConversion<string>().HasMaxLength(50);
            e.Property(s => s.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
            e.Property(s => s.StartDate).HasColumnName("start_date");
            e.Property(s => s.EndDate).HasColumnName("end_date");
            e.Property(s => s.NextBillingDate).HasColumnName("next_billing_date");
            e.Property(s => s.TrialEndsAt).HasColumnName("trial_ends_at");
            e.Property(s => s.CouponsJson).HasColumnName("coupons_json");
            e.Property(s => s.MaxPatients).HasColumnName("max_patients");
            e.Property(s => s.MaxStaff).HasColumnName("max_staff");
            e.Property(s => s.MaxBranches).HasColumnName("max_branches");
            e.Property(s => s.FeaturesJson).HasColumnName("features_json");
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            e.Property(s => s.IsDeleted).HasColumnName("is_deleted");
            e.Property(s => s.CreatedBy).HasColumnName("created_by");
            e.Property(s => s.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(s => s.ClinicId);
            e.HasIndex(s => s.IsDeleted);

            e.HasOne(s => s.Clinic)
                .WithMany()
                .HasForeignKey(s => s.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Feedback>(e =>
        {
            e.ToTable("feedbacks");
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasColumnName("id");
            e.Property(f => f.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(f => f.PatientId).HasColumnName("patient_id").IsRequired();
            e.Property(f => f.DoctorId).HasColumnName("doctor_id");
            e.Property(f => f.Rating).HasColumnName("rating");
            e.Property(f => f.Comment).HasColumnName("comment");
            e.Property(f => f.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50);
            e.Property(f => f.NPSScore).HasColumnName("nps_score");
            e.Property(f => f.VisitDate).HasColumnName("visit_date");
            e.Property(f => f.CreatedAt).HasColumnName("created_at");
            e.Property(f => f.UpdatedAt).HasColumnName("updated_at");
            e.Property(f => f.IsDeleted).HasColumnName("is_deleted");
            e.Property(f => f.CreatedBy).HasColumnName("created_by");
            e.Property(f => f.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(f => f.ClinicId);
            e.HasIndex(f => f.PatientId);
            e.HasIndex(f => f.DoctorId);
            e.HasIndex(f => f.IsDeleted);

            e.HasOne(f => f.Patient)
                .WithMany()
                .HasForeignKey(f => f.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(f => f.Doctor)
                .WithMany()
                .HasForeignKey(f => f.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(f => f.Clinic)
                .WithMany()
                .HasForeignKey(f => f.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.UserId).HasColumnName("user_id");
            e.Property(a => a.ClinicId).HasColumnName("clinic_id");
            e.Property(a => a.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityName).HasColumnName("entity_name").HasMaxLength(200).IsRequired();
            e.Property(a => a.EntityId).HasColumnName("entity_id");
            e.Property(a => a.OldValuesJson).HasColumnName("old_values_json");
            e.Property(a => a.NewValuesJson).HasColumnName("new_values_json");
            e.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
            e.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
            e.Property(a => a.CreatedAt).HasColumnName("created_at");
            e.Property(a => a.UpdatedAt).HasColumnName("updated_at");
            e.Property(a => a.IsDeleted).HasColumnName("is_deleted");
            e.Property(a => a.CreatedBy).HasColumnName("created_by");
            e.Property(a => a.UpdatedBy).HasColumnName("updated_by");

            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.ClinicId);
            e.HasIndex(a => a.EntityName);
            e.HasIndex(a => a.Action);
            e.HasIndex(a => a.CreatedAt);
            e.HasIndex(a => a.IsDeleted);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantFilter(ModelBuilder builder, Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(TenantEntity.ClinicId));
        var field = Expression.Field(Expression.Constant(this), nameof(_clinicId));
        var comparison = Expression.Equal(property, field);
        var lambda = Expression.Lambda(comparison, parameter);
        builder.Entity(entityType).HasQueryFilter(lambda);
    }

    private static void ApplySoftDeleteFilter(ModelBuilder builder, Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var comparison = Expression.Equal(property, Expression.Constant(false));
        var lambda = Expression.Lambda(comparison, parameter);
        builder.Entity(entityType).HasQueryFilter(lambda);
    }
}
