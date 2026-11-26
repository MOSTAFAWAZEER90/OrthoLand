using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Application.Billing;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Infrastructure.Identity;
using IdentityUser = OrthoFlowAI.Infrastructure.Identity.ApplicationIdentityUser;

namespace OrthoFlowAI.Infrastructure.Data;

/// <summary>
/// Extra, realistic demo data for presentations (Development only, Seed:DemoData=true):
/// more Egyptian patients with appointments around today, instalment plans with paid /
/// overdue instalments, treatment visits, a patient-portal login, and a second clinic so
/// the Super Admin dashboard has something to compare. Runs once (marker patient).
/// </summary>
public static class DemoDataExtras
{
    private const string Marker = "demo-extras@orthoflow.local";

    private static readonly TimeZoneInfo Cairo = FindCairo();

    public static async Task SeedAsync(ApplicationDbContext db, UserManager<IdentityUser> users, ApplicationRoleManager roles)
    {
        if (await db.Patients.IgnoreQueryFilters().AnyAsync(p => p.Email == Marker))
            return;

        var clinic = await db.Clinics.FirstOrDefaultAsync(c => c.Name == "Smile Perfect Orthodontics");
        if (clinic is null) return;

        var rnd = new Random(42);
        var branchId = (await db.Branches.IgnoreQueryFilters().FirstAsync(b => b.ClinicId == clinic.Id)).Id;
        var doctors = await db.Doctors.IgnoreQueryFilters().Where(d => d.ClinicId == clinic.Id).Select(d => d.Id).ToListAsync();

        // ── Clinic 1: 14 more patients ─────────────────────────────────────
        var people = new (string First, string Last, Gender G, int Age, string City, TreatmentType T, decimal Cost)[]
        {
            ("Mariam", "Abdelrahman", Gender.Female, 16, "Nasr City", TreatmentType.Ceramic, 38000),
            ("Ahmed", "Fathy", Gender.Male, 14, "Maadi", TreatmentType.Traditional, 28000),
            ("Salma", "Mostafa", Gender.Female, 24, "Heliopolis", TreatmentType.Invisalign, 85000),
            ("Youssef", "Ibrahim", Gender.Male, 13, "Zamalek", TreatmentType.Damon, 45000),
            ("Habiba", "Samir", Gender.Female, 19, "6th of October", TreatmentType.SelfLigating, 42000),
            ("Mohamed", "Adel", Gender.Male, 31, "Dokki", TreatmentType.Lingual, 70000),
            ("Nour", "ElDin", Gender.Female, 15, "Sheikh Zayed", TreatmentType.Ceramic, 38000),
            ("Ali", "Hamdy", Gender.Male, 17, "Mohandessin", TreatmentType.Traditional, 26000),
            ("Rana", "Magdy", Gender.Female, 27, "New Cairo", TreatmentType.Invisalign, 90000),
            ("Omar", "Tarek", Gender.Male, 12, "Shubra", TreatmentType.Traditional, 25000),
            ("Laila", "Hesham", Gender.Female, 22, "Maadi", TreatmentType.Combined, 60000),
            ("Karim", "Nabil", Gender.Male, 35, "Heliopolis", TreatmentType.Invisalign, 95000),
            ("Jana", "Wael", Gender.Female, 11, "Nasr City", TreatmentType.Traditional, 24000),
            ("Hassan", "Gamal", Gender.Male, 20, "Giza", TreatmentType.SelfLigating, 40000),
        };
        var prefixes = new[] { "010", "011", "012", "015" };

        var newPatients = new List<Patient>();
        for (var i = 0; i < people.Length; i++)
        {
            var p = people[i];
            var started = DateTime.UtcNow.Date.AddMonths(-rnd.Next(2, 14));
            newPatients.Add(new Patient
            {
                ClinicId = clinic.Id,
                BranchId = branchId,
                AssignedDoctorId = doctors[i % doctors.Count],
                FirstName = p.First,
                LastName = p.Last,
                Email = $"{p.First.ToLower()}.{p.Last.ToLower()}@example.com",
                Phone = $"+2{prefixes[i % 4]}{rnd.Next(10000000, 99999999)}",
                DateOfBirth = new DateTime(DateTime.UtcNow.Year - p.Age, rnd.Next(1, 12), rnd.Next(1, 28), 0, 0, 0, DateTimeKind.Utc),
                Gender = p.G,
                City = p.City,
                Address = $"{rnd.Next(2, 120)} {p.City} St.",
                Diagnosis = p.Age < 16 ? "Crowding with deep bite" : "Class II malocclusion with spacing",
                TreatmentType = p.T,
                TreatmentStartDate = started,
                EstimatedEndDate = started.AddMonths(p.T == TreatmentType.Invisalign ? 14 : 20),
                Status = PatientStatus.Active,
                JourneyScore = Math.Round(40 + rnd.NextDouble() * 55, 1),
                ComplianceScore = Math.Round(55 + rnd.NextDouble() * 45, 1),
                TotalTreatmentCost = p.Cost,
                CreatedAt = started,
            });
        }
        newPatients[^1].Email = Marker; // marks that extras were seeded
        db.Patients.AddRange(newPatients);
        await db.SaveChangesAsync();

        // ── Appointments: past two weeks (done / no-show) and next two weeks ─
        var slots = new Dictionary<(string Doctor, DateTime Day), int>();
        DateTime NextSlot(string doctor, DateTime day)
        {
            var key = (doctor, day.Date);
            var n = slots.TryGetValue(key, out var c) ? c : 0;
            slots[key] = n + 1;
            return CairoToUtc(day.Date.AddHours(12 + n)); // 12:00, 13:00 ... clinic time
        }

        var appointments = new List<Appointment>();
        var visits = new List<TreatmentVisit>();
        var types = new[] { AppointmentType.Adjustment, AppointmentType.FollowUp, AppointmentType.Adjustment, AppointmentType.Consultation };
        for (var i = 0; i < newPatients.Count; i++)
        {
            var patient = newPatients[i];
            var doctor = patient.AssignedDoctorId!;

            var pastDay = DateTime.UtcNow.Date.AddDays(-rnd.Next(1, 14));
            var past = NextSlot(doctor, pastDay);
            var noShow = i % 7 == 3;
            var pastAppt = Appt(clinic.Id, branchId, patient.Id, doctor, past,
                noShow ? AppointmentStatus.NoShow : AppointmentStatus.Completed, types[i % types.Length]);
            appointments.Add(pastAppt);

            if (!noShow)
            {
                visits.Add(new TreatmentVisit
                {
                    ClinicId = clinic.Id,
                    PatientId = patient.Id,
                    DoctorId = doctor,
                    AppointmentId = pastAppt.Id,
                    VisitDate = past,
                    WireChanged = i % 2 == 0,
                    RubberBandsUsed = i % 3 == 0,
                    PainLevel = (PainLevel)rnd.Next(0, 4),
                    DoctorNotes = i % 2 == 0 ? "Wire changed, good progress on alignment." : "Routine check, patient compliant with elastics.",
                    NextAppointmentDate = past.AddDays(28),
                    CreatedAt = past,
                });
            }

            var futureDay = DateTime.UtcNow.Date.AddDays(rnd.Next(0, 14));
            appointments.Add(Appt(clinic.Id, branchId, patient.Id, doctor, NextSlot(doctor, futureDay),
                i % 2 == 0 ? AppointmentStatus.Confirmed : AppointmentStatus.Scheduled, types[(i + 1) % types.Length]));
        }
        db.Appointments.AddRange(appointments);
        db.TreatmentVisits.AddRange(visits);

        // ── Invoices with instalment plans ─────────────────────────────────
        var number = await db.Invoices.IgnoreQueryFilters().CountAsync() + 1;
        for (var i = 0; i < newPatients.Count; i++)
        {
            var patient = newPatients[i];
            var total = patient.TotalTreatmentCost;
            var down = Math.Round(total * 0.25m / 1000) * 1000;
            var invoice = new Invoice
            {
                ClinicId = clinic.Id,
                PatientId = patient.Id,
                DoctorId = patient.AssignedDoctorId,
                InvoiceNumber = $"INV-2026-{number++:000}",
                TotalAmount = total,
                Status = InvoiceStatus.Partial,
                PaymentStatus = PaymentStatus.Partial,
                ItemsJson = $"[{{\"description\":\"{patient.TreatmentType} braces\",\"amount\":{total * 0.6m:0}}},{{\"description\":\"Treatment follow-up\",\"amount\":{total * 0.4m:0}}}]",
                Notes = "Down payment + monthly instalments",
                CreatedAt = patient.TreatmentStartDate!.Value,
            };
            db.Invoices.Add(invoice);

            var payments = new List<Payment> { Pay(invoice, down, patient.TreatmentStartDate.Value, PaymentMethod.Cash) };

            var plan = InstallmentPlanner.Plan(total - down, 12, patient.TreatmentStartDate.Value.AddMonths(1));
            var installments = plan.Select(x => new Installment
            {
                ClinicId = clinic.Id,
                InvoiceId = invoice.Id,
                PatientId = patient.Id,
                Number = x.Number,
                DueDate = DateTime.SpecifyKind(x.DueDate, DateTimeKind.Utc),
                Amount = x.Amount,
            }).ToList();

            // Pay the instalments that are already due, except a few "late payers".
            var latePayer = i % 4 == 1;
            foreach (var inst in installments.Where(x => x.DueDate < DateTime.UtcNow))
            {
                if (latePayer && inst.DueDate > DateTime.UtcNow.AddMonths(-2)) break;
                payments.Add(Pay(invoice, inst.Amount, inst.DueDate.AddDays(rnd.Next(0, 4)),
                    i % 3 == 0 ? PaymentMethod.Cash : PaymentMethod.Online));
            }
            // Everything after the down payment goes to the instalments, oldest first.
            var paidTowardsPlan = payments.Skip(1).Sum(x => x.Amount);
            if (paidTowardsPlan > 0) InstallmentPlanner.ApplyPayment(installments, paidTowardsPlan, DateTime.UtcNow);
            InstallmentPlanner.MarkOverdue(installments, DateTime.UtcNow);

            invoice.PaidAmount = payments.Sum(x => x.Amount);
            invoice.RemainingAmount = total - invoice.PaidAmount;
            invoice.DueDate = installments.FirstOrDefault(x => x.Status != InstallmentStatus.Paid)?.DueDate;
            patient.AmountPaid = invoice.PaidAmount;
            patient.AmountRemaining = invoice.RemainingAmount;

            db.Payments.AddRange(payments);
            db.Installments.AddRange(installments);
        }
        await db.SaveChangesAsync();

        // ── Patient portal login for an existing patient ───────────────────
        var omar = await db.Patients.FirstOrDefaultAsync(p => p.ClinicId == clinic.Id && p.FirstName == "Omar" && p.LastName == "Khaled");
        if (omar != null && omar.UserId == null)
        {
            var portalUser = await CreateUserAsync(db, users, roles, "patient@demo.com", "Patient@123", "Omar", "Khaled",
                UserRole.Patient, clinic.Id, branchId, omar.Phone);
            omar.UserId = portalUser.Id;
            await db.SaveChangesAsync();
        }

        // ── Second clinic (for the Super Admin dashboard) ──────────────────
        await SeedSecondClinicAsync(db, users, roles, rnd);
    }

    private static async Task SeedSecondClinicAsync(ApplicationDbContext db, UserManager<IdentityUser> users,
        ApplicationRoleManager roles, Random rnd)
    {
        if (await db.Clinics.AnyAsync(c => c.Name == "Cairo Smile Center")) return;

        var clinic = new Clinic
        {
            Name = "Cairo Smile Center",
            Email = "info@cairosmile.demo",
            Phone = "+20224181818",
            Address = "8 El-Merghany St.",
            City = "Heliopolis",
            Country = "Egypt",
            BusinessHours = "Sat=12:00-21:00;Sun=12:00-21:00;Mon=12:00-21:00;Tue=12:00-21:00;Wed=12:00-21:00",
            WorkingDays = "Sat,Sun,Mon,Tue,Wed",
            PrimaryColor = "#00897B",
            SecondaryColor = "#FFFFFF",
            SubscriptionPlan = "Professional",
            SubscriptionStatus = "Active",
            Language = "ar",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-5),
        };
        db.Clinics.Add(clinic);
        var branch = new Branch { ClinicId = clinic.Id, Name = "Heliopolis Branch", Address = clinic.Address, Phone = clinic.Phone, IsMainBranch = true, OperatingHours = "12:00-21:00" };
        db.Branches.Add(branch);
        db.Subscriptions.Add(new Subscription
        {
            ClinicId = clinic.Id,
            Plan = SubscriptionPlan.Professional,
            Status = SubscriptionStatus.Active,
            BillingPeriod = BillingPeriod.Monthly,
            Amount = 1999m,
            StartDate = DateTime.UtcNow.AddMonths(-5),
            EndDate = DateTime.UtcNow.AddMonths(7),
            NextBillingDate = DateTime.UtcNow.AddDays(9),
            MaxPatients = 500,
            MaxStaff = 15,
            MaxBranches = 3,
        });
        await db.SaveChangesAsync();

        await CreateUserAsync(db, users, roles, "owner2@demo.com", "Owner@123", "Hany", "Soliman", UserRole.ClinicOwner, clinic.Id, branch.Id, "+201000002222");
        var drUser = await CreateUserAsync(db, users, roles, "dr.yasmin@cairosmile.demo", "Doctor@123", "Yasmin", "Fawzy", UserRole.Doctor, clinic.Id, branch.Id, "+201000003333");
        var doctor = new Doctor { UserId = drUser.Id, ClinicId = clinic.Id, Specialty = "Orthodontics", YearsOfExperience = 9, ConsultationFee = 600m, IsAcceptingPatients = true };
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var names = new[] { ("Ziad", "Sherif"), ("Malak", "Ashraf"), ("Adam", "Khalil"), ("Farida", "Sayed"), ("Seif", "Hany"), ("Lina", "Osama") };
        foreach (var (first, last) in names)
        {
            var cost = 20000m + rnd.Next(0, 8) * 5000m;
            var paid = Math.Round(cost * (decimal)(0.2 + rnd.NextDouble() * 0.6) / 500) * 500;
            var patient = new Patient
            {
                ClinicId = clinic.Id, BranchId = branch.Id, AssignedDoctorId = doctor.Id,
                FirstName = first, LastName = last, Phone = $"+2012{rnd.Next(10000000, 99999999)}",
                TreatmentType = TreatmentType.Ceramic, Status = PatientStatus.Active,
                TreatmentStartDate = DateTime.UtcNow.AddMonths(-rnd.Next(1, 5)),
                TotalTreatmentCost = cost, AmountPaid = paid, AmountRemaining = cost - paid,
                JourneyScore = 60, ComplianceScore = 80,
            };
            db.Patients.Add(patient);
            var invoice = new Invoice
            {
                ClinicId = clinic.Id, PatientId = patient.Id, DoctorId = doctor.Id,
                InvoiceNumber = $"CSC-{rnd.Next(1000, 9999)}", TotalAmount = cost, PaidAmount = paid, RemainingAmount = cost - paid,
                Status = InvoiceStatus.Partial, PaymentStatus = PaymentStatus.Partial, CreatedAt = patient.TreatmentStartDate!.Value,
            };
            db.Invoices.Add(invoice);
            db.Payments.Add(Pay(invoice, paid, invoice.CreatedAt, PaymentMethod.Cash));
            db.Appointments.Add(Appt(clinic.Id, branch.Id, patient.Id, doctor.Id,
                CairoToUtc(DateTime.UtcNow.Date.AddDays(rnd.Next(0, 10)).AddHours(13 + rnd.Next(0, 6))),
                AppointmentStatus.Scheduled, AppointmentType.Adjustment));
        }
        await db.SaveChangesAsync();
    }

    private static Appointment Appt(string clinicId, string branchId, string patientId, string doctorId, DateTime startUtc,
        AppointmentStatus status, AppointmentType type) => new()
    {
        ClinicId = clinicId,
        BranchId = branchId,
        PatientId = patientId,
        DoctorId = doctorId,
        AppointmentDate = startUtc,
        EndTime = startUtc.AddMinutes(30),
        Duration = 30,
        Status = status,
        Type = type,
        Color = status == AppointmentStatus.NoShow ? "#E53935" : "#4CAF50",
        CreatedAt = startUtc.AddDays(-7),
    };

    private static Payment Pay(Invoice invoice, decimal amount, DateTime when, PaymentMethod method) => new()
    {
        ClinicId = invoice.ClinicId,
        InvoiceId = invoice.Id,
        PatientId = invoice.PatientId,
        Amount = amount,
        PaymentMethod = method,
        Status = "Completed",
        PaymentDate = when,
        CreatedAt = when,
    };

    private static async Task<IdentityUser> CreateUserAsync(ApplicationDbContext db, UserManager<IdentityUser> users,
        ApplicationRoleManager roles, string email, string password, string first, string last, UserRole role,
        string clinicId, string branchId, string? phone)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing != null) return existing;

        var user = new IdentityUser
        {
            Email = email, UserName = email, FirstName = first, LastName = last, PhoneNumber = phone,
            Role = role.ToString(), ClinicId = clinicId, BranchId = branchId, IsActive = true,
            Language = Language.Arabic.ToString(), EmailConfirmed = true,
        };
        await SeedData.CreateUserAsync(db, users, user, password);
        await roles.AssignUserRoleAsync(user.Id, role.ToString());
        return user;
    }

    private static DateTime CairoToUtc(DateTime cairoLocal) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(cairoLocal, DateTimeKind.Unspecified), Cairo);

    private static TimeZoneInfo FindCairo()
    {
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
