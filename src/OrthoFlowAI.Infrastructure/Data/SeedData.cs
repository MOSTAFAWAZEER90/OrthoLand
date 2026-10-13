using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Infrastructure.Data;
using OrthoFlowAI.Infrastructure.Identity;
using ApplicationUser = OrthoFlowAI.Infrastructure.Identity.ApplicationIdentityUser;

namespace OrthoFlowAI.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ApplicationRoleManager roleManager)
    {
        if (await context.Clinics.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        var clinicId = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid().ToString();

        var superAdminUserId = Guid.NewGuid().ToString();
        var ownerUserId = Guid.NewGuid().ToString();
        var doctor1UserId = Guid.NewGuid().ToString();
        var doctor2UserId = Guid.NewGuid().ToString();
        var receptionistUserId = Guid.NewGuid().ToString();

        var doctor1Id = Guid.NewGuid().ToString();
        var doctor2Id = Guid.NewGuid().ToString();
        var staffId = Guid.NewGuid().ToString();

        var patient1Id = Guid.NewGuid().ToString();
        var patient2Id = Guid.NewGuid().ToString();
        var patient3Id = Guid.NewGuid().ToString();
        var patient4Id = Guid.NewGuid().ToString();
        var patient5Id = Guid.NewGuid().ToString();

        var subscriptionId = Guid.NewGuid().ToString();

        // ── 1. SuperAdmin ──────────────────────────────────────────────────
        var superAdmin = new ApplicationUser
        {
            Id = superAdminUserId,
            FirstName = "System",
            LastName = "Administrator",
            Email = "admin@orthoflow.ai",
            UserName = "admin@orthoflow.ai",
            PhoneNumber = "+201000000000",
            Role = UserRole.SuperAdmin.ToString(),
            IsActive = true,
            Language = Language.English.ToString(),
            CreatedAt = now,
            NormalizedEmail = "ADMIN@ORTHOFLOW.AI",
            NormalizedUserName = "ADMIN@ORTHOFLOW.AI",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        if (await userManager.FindByEmailAsync(superAdmin.Email) is null)
        {
            await CreateUserAsync(userManager, superAdmin, "Admin@123");
            await roleManager.AssignUserRoleAsync(superAdmin.Id, UserRole.SuperAdmin.ToString());
        }

        // ── 2. Demo Clinic ─────────────────────────────────────────────────
        var clinic = new Clinic
        {
            Id = clinicId,
            Name = "Smile Perfect Orthodontics",
            Email = "clinic@demo.com",
            Phone = "+20225551234",
            Address = "42 Tahrir Street, Downtown",
            City = "Cairo",
            Country = "Egypt",
            Website = "https://smileperfect.demo.com",
            BusinessHours = "09:00-17:00",
            WorkingDays = "Sun,Mon,Tue,Wed,Thu",
            PrimaryColor = "#1E88E5",
            SecondaryColor = "#FFFFFF",
            SubscriptionPlan = "Starter",
            SubscriptionStatus = "Active",
            Language = "ar",
            IsActive = true,
            CreatedAt = now,
            CreatedBy = superAdminUserId
        };
        context.Clinics.Add(clinic);

        // ── 3. Branch ──────────────────────────────────────────────────────
        var branch = new Branch
        {
            Id = branchId,
            ClinicId = clinicId,
            Name = "Main Branch",
            Address = "42 Tahrir Street, Downtown, Cairo",
            Phone = "+20225551234",
            Email = "main@smileperfect.demo.com",
            IsMainBranch = true,
            OperatingHours = "09:00-17:00",
            CreatedAt = now,
            CreatedBy = superAdminUserId
        };
        context.Branches.Add(branch);

        // ── 4. Clinic Owner ────────────────────────────────────────────────
        var ownerIdentity = new ApplicationUser
        {
            Id = ownerUserId,
            FirstName = "Mohamed",
            LastName = "El-Sayed",
            Email = "owner@demo.com",
            UserName = "owner@demo.com",
            PhoneNumber = "+201011111111",
            Role = UserRole.ClinicOwner.ToString(),
            ClinicId = clinicId,
            BranchId = branchId,
            IsActive = true,
            Language = Language.Arabic.ToString(),
            CreatedAt = now,
            NormalizedEmail = "OWNER@DEMO.COM",
            NormalizedUserName = "OWNER@DEMO.COM",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        if (await userManager.FindByEmailAsync(ownerIdentity.Email) is null)
        {
            await CreateUserAsync(userManager, ownerIdentity, "Owner@123");
            await roleManager.AssignUserRoleAsync(ownerIdentity.Id, UserRole.ClinicOwner.ToString());
        }

        // ── 5. Doctor 1 – Ahmed Hassan ────────────────────────────────────
        var doctor1User = new ApplicationUser
        {
            Id = doctor1UserId,
            FirstName = "Ahmed",
            LastName = "Hassan",
            Email = "dr.ahmed@smileperfect.demo.com",
            UserName = "dr.ahmed@smileperfect.demo.com",
            PhoneNumber = "+201022222222",
            Role = UserRole.Doctor.ToString(),
            ClinicId = clinicId,
            BranchId = branchId,
            IsActive = true,
            Language = Language.Arabic.ToString(),
            CreatedAt = now,
            NormalizedEmail = "DR.AHMED@SMILEPERFECT.DEMO.COM",
            NormalizedUserName = "DR.AHMED@SMILEPERFECT.DEMO.COM",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        if (await userManager.FindByEmailAsync(doctor1User.Email) is null)
        {
            await CreateUserAsync(userManager, doctor1User, "Doctor@123");
            await roleManager.AssignUserRoleAsync(doctor1User.Id, UserRole.Doctor.ToString());
        }

        var doctor1 = new Doctor
        {
            Id = doctor1Id,
            UserId = doctor1UserId,
            ClinicId = clinicId,
            Specialty = "Orthodontics & Dentofacial Orthopedics",
            LicenseNumber = "EG-DENT-2010-4421",
            Bio = "Dr. Ahmed Hassan is a board-certified orthodontist with over 14 years of experience in traditional braces, Invisalign, and surgical orthodontics. He graduated from Cairo University Faculty of Dentistry and completed his orthodontic residency at Ain Shams University.",
            YearsOfExperience = 14,
            ConsultationFee = 750m,
            IsAcceptingPatients = true,
            CreatedAt = now,
            CreatedBy = ownerUserId
        };

        // ── 6. Doctor 2 – Sara Mohamed ────────────────────────────────────
        var doctor2User = new ApplicationUser
        {
            Id = doctor2UserId,
            FirstName = "Sara",
            LastName = "Mohamed",
            Email = "dr.sara@smileperfect.demo.com",
            UserName = "dr.sara@smileperfect.demo.com",
            PhoneNumber = "+201033333333",
            Role = UserRole.Doctor.ToString(),
            ClinicId = clinicId,
            BranchId = branchId,
            IsActive = true,
            Language = Language.Arabic.ToString(),
            CreatedAt = now,
            NormalizedEmail = "DR.SARA@SMILEPERFECT.DEMO.COM",
            NormalizedUserName = "DR.SARA@SMILEPERFECT.DEMO.COM",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        if (await userManager.FindByEmailAsync(doctor2User.Email) is null)
        {
            await CreateUserAsync(userManager, doctor2User, "Doctor@123");
            await roleManager.AssignUserRoleAsync(doctor2User.Id, UserRole.Doctor.ToString());
        }

        var doctor2 = new Doctor
        {
            Id = doctor2Id,
            UserId = doctor2UserId,
            ClinicId = clinicId,
            Specialty = "Pediatric Orthodontics & Invisalign",
            LicenseNumber = "EG-DENT-2015-7832",
            Bio = "Dr. Sara Mohamed specializes in pediatric orthodontics and clear aligner therapy. With 9 years of clinical experience, she is a certified Invisalign Diamond provider and passionate about early intervention treatment.",
            YearsOfExperience = 9,
            ConsultationFee = 700m,
            IsAcceptingPatients = true,
            CreatedAt = now,
            CreatedBy = ownerUserId
        };

        // ── 7. Receptionist ────────────────────────────────────────────────
        var receptionistUser = new ApplicationUser
        {
            Id = receptionistUserId,
            FirstName = "Nour",
            LastName = "Abdallah",
            Email = "nour@smileperfect.demo.com",
            UserName = "nour@smileperfect.demo.com",
            PhoneNumber = "+201044444444",
            Role = UserRole.Receptionist.ToString(),
            ClinicId = clinicId,
            BranchId = branchId,
            IsActive = true,
            Language = Language.Arabic.ToString(),
            CreatedAt = now,
            NormalizedEmail = "NOUR@SMILEPERFECT.DEMO.COM",
            NormalizedUserName = "NOUR@SMILEPERFECT.DEMO.COM",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        if (await userManager.FindByEmailAsync(receptionistUser.Email) is null)
        {
            await CreateUserAsync(userManager, receptionistUser, "Staff@123");
            await roleManager.AssignUserRoleAsync(receptionistUser.Id, UserRole.Receptionist.ToString());
        }

        var staff = new Staff
        {
            Id = staffId,
            UserId = receptionistUserId,
            ClinicId = clinicId,
            Position = "Front Desk Receptionist",
            Department = "Administration",
            Status = StaffStatus.Active,
            HireDate = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Salary = 8500m,
            ScheduleJson = "{\"sunday\":\"09:00-17:00\",\"monday\":\"09:00-17:00\",\"tuesday\":\"09:00-17:00\",\"wednesday\":\"09:00-17:00\",\"thursday\":\"09:00-14:00\"}",
            PermissionsJson = "{\"canBookAppointments\":true,\"canViewPatients\":true,\"canCreateInvoices\":true,\"canManagePayments\":true}",
            CreatedAt = now,
            CreatedBy = ownerUserId
        };

        // ── 8. Patients ────────────────────────────────────────────────────
        var patients = new List<Patient>
        {
            new Patient
            {
                Id = patient1Id,
                ClinicId = clinicId,
                BranchId = branchId,
                AssignedDoctorId = doctor1Id,
                FirstName = "Omar",
                LastName = "Khaled",
                Email = "omar.khaled@email.com",
                Phone = "+201055551001",
                DateOfBirth = new DateTime(2002, 4, 12, 0, 0, 0, DateTimeKind.Utc),
                Gender = Gender.Male,
                Address = "15 Nile Street, Zamalek",
                City = "Cairo",
                EmergencyContactName = "Khaled Ibrahim",
                EmergencyContactPhone = "+201055551099",
                InsuranceProvider = "Bupa Egypt",
                InsuranceNumber = "BUPA-2024-88431",
                Diagnosis = "Class II Division 1 malocclusion with severe overjet (9mm) and crowding in lower arch",
                TreatmentPlanDescription = "Full upper and lower fixed braces with extraction of upper first premolars. Estimated 24-month treatment duration with possible surgical phase for jaw correction.",
                TreatmentType = TreatmentType.Damon,
                TreatmentStartDate = now.AddDays(-120),
                EstimatedEndDate = now.AddMonths(12),
                Status = PatientStatus.Active,
                Tags = "braces,damon,extraction,active",
                JourneyScore = 72.5,
                ComplianceScore = 85.0,
                TotalTreatmentCost = 45000m,
                AmountPaid = 22500m,
                AmountRemaining = 22500m,
                Notes = "Patient is compliant with elastic wear. Next adjustment scheduled for wire change.",
                CreatedAt = now.AddDays(-150),
                CreatedBy = ownerUserId
            },
            new Patient
            {
                Id = patient2Id,
                ClinicId = clinicId,
                BranchId = branchId,
                AssignedDoctorId = doctor2Id,
                FirstName = "Fatma",
                LastName = "Ali",
                Email = "fatma.ali@email.com",
                Phone = "+201055552002",
                DateOfBirth = new DateTime(2010, 8, 3, 0, 0, 0, DateTimeKind.Utc),
                Gender = Gender.Female,
                Address = "28 Baghdad Street, Heliopolis",
                City = "Cairo",
                EmergencyContactName = "Ali Mahmoud",
                EmergencyContactPhone = "+201055552098",
                InsuranceProvider = "MetLife Egypt",
                InsuranceNumber = "MET-2024-66120",
                Diagnosis = "Class I malocclusion with anterior crossbite and mild crowding. Mixed dentition phase with early permanent molars erupted.",
                TreatmentPlanDescription = "Phase 1 interceptive orthodontics with rapid palatal expander followed by partial fixed appliances. Phase 2 to begin after permanent dentition eruption.",
                TreatmentType = TreatmentType.Ceramic,
                TreatmentStartDate = now.AddDays(-60),
                EstimatedEndDate = now.AddMonths(18),
                Status = PatientStatus.Active,
                Tags = "pediatric,phase1,invisalign,expander",
                JourneyScore = 90.0,
                ComplianceScore = 92.0,
                TotalTreatmentCost = 35000m,
                AmountPaid = 35000m,
                AmountRemaining = 0m,
                Notes = "Excellent patient compliance. Expansion complete, transitioning to fixed ceramic braces next month.",
                CreatedAt = now.AddDays(-75),
                CreatedBy = doctor2UserId
            },
            new Patient
            {
                Id = patient3Id,
                ClinicId = clinicId,
                BranchId = branchId,
                AssignedDoctorId = doctor1Id,
                FirstName = "Youssef",
                LastName = "Tarek",
                Email = "youssef.t@email.com",
                Phone = "+201055553003",
                DateOfBirth = new DateTime(1995, 12, 18, 0, 0, 0, DateTimeKind.Utc),
                Gender = Gender.Male,
                Address = "7 Pyramids Road, Giza",
                City = "Giza",
                EmergencyContactName = "Tarek Osman",
                EmergencyContactPhone = "+201055553097",
                InsuranceProvider = "Cigna Egypt",
                InsuranceNumber = "CIG-2024-33012",
                Diagnosis = "Class III malocclusion with anterior crossbite and mandibular prognathism",
                TreatmentPlanDescription = "Orthodontic treatment with temporary anchorage devices (TADs) for camouflage approach. Upper arch expansion with Class III elastics.",
                TreatmentType = TreatmentType.SelfLigating,
                TreatmentStartDate = now.AddDays(-300),
                EstimatedEndDate = now.AddMonths(3),
                Status = PatientStatus.Active,
                Tags = "braces,self-ligating,TADs,class3",
                JourneyScore = 88.0,
                ComplianceScore = 78.0,
                TotalTreatmentCost = 52000m,
                AmountPaid = 40000m,
                AmountRemaining = 12000m,
                Notes = "Treatment nearing completion. Debonding appointment scheduled for next month.",
                CreatedAt = now.AddDays(-330),
                CreatedBy = ownerUserId
            },
            new Patient
            {
                Id = patient4Id,
                ClinicId = clinicId,
                BranchId = branchId,
                AssignedDoctorId = doctor2Id,
                FirstName = "Nada",
                LastName = "Hussein",
                Email = "nada.h@email.com",
                Phone = "+201055554004",
                DateOfBirth = new DateTime(2008, 2, 27, 0, 0, 0, DateTimeKind.Utc),
                Gender = Gender.Female,
                Address = "91 Mohandessin Street, Giza",
                City = "Giza",
                EmergencyContactName = "Hussein Gamal",
                EmergencyContactPhone = "+201055554096",
                InsuranceProvider = "AXA Egypt",
                InsuranceNumber = "AXA-2024-11250",
                Diagnosis = "Class II Division 2 malocclusion with deep bite and retroclined upper incisors",
                TreatmentPlanDescription = "Invisalign Teen treatment plan with 30 aligners. IPR scheduled for lower anterior segment. Aligner changes every 10 days.",
                TreatmentType = TreatmentType.Invisalign,
                TreatmentStartDate = now.AddDays(-45),
                EstimatedEndDate = now.AddMonths(15),
                Status = PatientStatus.Active,
                Tags = "invisalign,teen,deep-bite",
                JourneyScore = 65.0,
                ComplianceScore = 55.0,
                TotalTreatmentCost = 40000m,
                AmountPaid = 15000m,
                AmountRemaining = 25000m,
                Notes = "Patient reports difficulty remembering to wear aligners 22 hours/day. Compliance monitoring needed.",
                CreatedAt = now.AddDays(-55),
                CreatedBy = doctor2UserId
            },
            new Patient
            {
                Id = patient5Id,
                ClinicId = clinicId,
                BranchId = branchId,
                AssignedDoctorId = doctor1Id,
                FirstName = "Karim",
                LastName = "Magdy",
                Email = "karim.m@email.com",
                Phone = "+201055555005",
                DateOfBirth = new DateTime(1990, 6, 9, 0, 0, 0, DateTimeKind.Utc),
                Gender = Gender.Male,
                Address = "33 Garden City, Cairo",
                City = "Cairo",
                EmergencyContactName = "Magdy Saleh",
                EmergencyContactPhone = "+201055555095",
                InsuranceProvider = "Allianz Egypt",
                InsuranceNumber = "ALL-2024-44008",
                Diagnosis = "Class I mild crowding with diastema. Post-treatment relapse from previous orthodontic treatment at age 16.",
                TreatmentPlanDescription = "Clear retainer therapy with minor alignment using Invisalign Lite (14 aligners). Fixed lingual retainer planned for long-term retention.",
                TreatmentType = TreatmentType.Invisalign,
                TreatmentStartDate = now.AddDays(-180),
                EstimatedEndDate = now.AddDays(-30),
                Status = PatientStatus.Completed,
                Tags = "invisalign,retention,completed",
                JourneyScore = 100.0,
                ComplianceScore = 95.0,
                TotalTreatmentCost = 25000m,
                AmountPaid = 25000m,
                AmountRemaining = 0m,
                Notes = "Treatment completed successfully. Fixed lingual retainer bonded. Follow-up every 3 months.",
                CreatedAt = now.AddDays(-200),
                CreatedBy = ownerUserId
            }
        };

        context.Doctors.AddRange(new[] { doctor1, doctor2 });
        context.Staff.Add(staff);

        context.Patients.AddRange(patients);

        // ── 9. Appointments (10 across this month) ─────────────────────────
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                PatientId = patient1Id,
                DoctorId = doctor1Id,
                AppointmentDate = startOfMonth.AddDays(2).AddHours(9),
                EndTime = startOfMonth.AddDays(2).AddHours(9).AddMinutes(30),
                Status = AppointmentStatus.Completed,
                Type = AppointmentType.Adjustment,
                Notes = "Wire change from 0.014 to 0.016 NiTi. Added power chain for space closure.",
                Duration = 30,
                Color = "#4CAF50",
                IsRecurring = true,
                RecurrencePattern = "every 4 weeks",
                RecurrenceEndDate = now.AddMonths(10),
                CreatedAt = now.AddDays(-10),
                CreatedBy = receptionistUserId
            },
            new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                PatientId = patient2Id,
                DoctorId = doctor2Id,
                AppointmentDate = startOfMonth.AddDays(5).AddHours(10),
                EndTime = startOfMonth.AddDays(5).AddHours(10).AddMinutes(45),
                Status = AppointmentStatus.Completed,
                Type = AppointmentType.Consultation,
                Notes = "Progress review after expansion phase. Took impressions for ceramic brackets.",
                Duration = 45,
                Color = "#2196F3",
                CreatedAt = now.AddDays(-8),
                CreatedBy = receptionistUserId
            },
            new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                PatientId = patient3Id,
                DoctorId = doctor1Id,
                AppointmentDate = startOfMonth.AddDays(7).AddHours(11),
                EndTime = startOfMonth.AddDays(7).AddHours(11).AddMinutes(30),
                Status = AppointmentStatus.Completed,
                Type = AppointmentType.Adjustment,
                Notes = "Final adjustments before debonding. Elastic wear evaluated - Class III correction satisfactory.",
                Duration = 30,
                Color = "#4CAF50",
                CreatedAt = now.AddDays(-6),
                CreatedBy = doctor1UserId
            },
            new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                PatientId = patient4Id,
                DoctorId = doctor2Id,
                AppointmentDate = startOfMonth.AddDays(10).AddHours(9),
                EndTime = startOfMonth.AddDays(10).AddHours(9).AddMinutes(30),
                Status = AppointmentStatus.Completed,
                Type = AppointmentType.FollowUp,
                Notes = "Aligner check. Patient reported losing aligner #6 for 2 days. Replacement ordered.",
                Duration = 30,
                Color = "#FF9800",
                CreatedAt = now.AddDays(-5),
                CreatedBy = receptionistUserId
            },
            new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                PatientId = patient1Id,
                DoctorId = doctor1Id,
                AppointmentDate = startOfMonth.AddDays(14).AddHours(14),
                EndTime = startOfMonth.AddDays(14).AddHours(14).AddMinutes(30),
                Status = AppointmentStatus.Confirmed,
                Type = AppointmentType.Photo,
                Notes = "Progress photography session - intraoral and extraoral photos for treatment documentation.",
                Duration = 30,
                Color = "#9C27B0",
                CreatedAt = now.AddDays(-3),
                CreatedBy = receptionistUserId
            },
            new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                PatientId = patient2Id,
                DoctorId = doctor2Id,
                AppointmentDate = startOfMonth.AddDays(18).AddHours(10),
                EndTime = startOfMonth.AddDays(18).AddHours(10).AddMinutes(60),
                Status = AppointmentStatus.Scheduled,
                Type = AppointmentType.Bonding,
                Notes = "Bonding appointment for upper ceramic brackets. Estimated 60 minutes.",
                Duration = 60,
                Color = "#E91E63",
                CreatedAt = now.AddDays(-2),
                CreatedBy = receptionistUserId
            },
            new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                PatientId = patient3Id,
                DoctorId = doctor1Id,
                AppointmentDate = startOfMonth.AddDays(20).AddHours(11),
                EndTime = startOfMonth.AddDays(20).AddHours(11).AddMinutes(90),
                Status = AppointmentStatus.Scheduled,
                Type = AppointmentType.Debonding,
                Notes = "Debonding appointment. Bonded lingual retainer on lower 6 teeth. Upper Hawley retainer impressions.",
                Duration = 90,
                Color = "#FF5722",
                CreatedAt = now.AddDays(-1),
                CreatedBy = doctor1UserId
            },
            new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                PatientId = patient4Id,
                DoctorId = doctor2Id,
                AppointmentDate = startOfMonth.AddDays(22).AddHours(9),
                EndTime = startOfMonth.AddDays(22).AddHours(9).AddMinutes(45),
                Status = AppointmentStatus.Scheduled,
                Type = AppointmentType.Adjustment,
                Notes = "New aligner delivery (sets 7-12). IPR on lower anterior teeth.",
                Duration = 45,
                Color = "#00BCD4",
                CreatedAt = now,
                CreatedBy = receptionistUserId
            },
            new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                PatientId = patient1Id,
                DoctorId = doctor1Id,
                AppointmentDate = startOfMonth.AddDays(25).AddHours(14),
                EndTime = startOfMonth.AddDays(25).AddHours(14).AddMinutes(30),
                Status = AppointmentStatus.Scheduled,
                Type = AppointmentType.Adjustment,
                Notes = "Monthly adjustment. Wire progression to 0.018 stainless steel. Check space closure.",
                Duration = 30,
                Color = "#4CAF50",
                IsRecurring = true,
                RecurrencePattern = "every 4 weeks",
                CreatedAt = now,
                CreatedBy = receptionistUserId
            },
            new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                PatientId = patient5Id,
                DoctorId = doctor1Id,
                AppointmentDate = startOfMonth.AddDays(28).AddHours(15),
                EndTime = startOfMonth.AddDays(28).AddHours(15).AddMinutes(30),
                Status = AppointmentStatus.Scheduled,
                Type = AppointmentType.FollowUp,
                Notes = "Post-treatment follow-up. Retainer check and stability assessment.",
                Duration = 30,
                Color = "#607D8B",
                CreatedAt = now,
                CreatedBy = doctor1UserId
            }
        };

        context.Appointments.AddRange(appointments);

        // ── 10. Treatment Visits ────────────────────────────────────────────
        var treatmentVisits = new List<TreatmentVisit>
        {
            new TreatmentVisit
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                PatientId = patient1Id,
                DoctorId = doctor1Id,
                AppointmentId = appointments[0].Id,
                VisitDate = startOfMonth.AddDays(2),
                WireChanged = true,
                BracketReplaced = false,
                RubberBandsUsed = true,
                PainLevel = PainLevel.Mild,
                DoctorNotes = "Wire changed to 0.016 NiTi. Power chain applied from upper left 4 to right 4. Patient tolerating treatment well.",
                Medication = "Ibuprofen 400mg as needed",
                NextAppointmentDate = startOfMonth.AddDays(30),
                CreatedAt = startOfMonth.AddDays(2),
                CreatedBy = doctor1UserId
            },
            new TreatmentVisit
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                PatientId = patient3Id,
                DoctorId = doctor1Id,
                AppointmentId = appointments[2].Id,
                VisitDate = startOfMonth.AddDays(7),
                WireChanged = false,
                BracketReplaced = false,
                RubberBandsUsed = true,
                PainLevel = PainLevel.None,
                DoctorNotes = "Final assessment before debonding. Overjet reduced from 9mm to 2mm. Molar relationship Class I achieved.",
                NextAppointmentDate = startOfMonth.AddDays(20),
                CreatedAt = startOfMonth.AddDays(7),
                CreatedBy = doctor1UserId
            }
        };

        context.TreatmentVisits.AddRange(treatmentVisits);

        // ── 11. Invoices ───────────────────────────────────────────────────
        var invoice1Id = Guid.NewGuid().ToString();
        var invoice2Id = Guid.NewGuid().ToString();
        var invoice3Id = Guid.NewGuid().ToString();
        var invoice4Id = Guid.NewGuid().ToString();
        var invoice5Id = Guid.NewGuid().ToString();

        var invoices = new List<Invoice>
        {
            new Invoice
            {
                Id = invoice1Id,
                ClinicId = clinicId,
                InvoiceNumber = "INV-2026-001",
                PatientId = patient1Id,
                DoctorId = doctor1Id,
                TotalAmount = 45000m,
                PaidAmount = 22500m,
                RemainingAmount = 22500m,
                Discount = 0m,
                Tax = 0m,
                Status = InvoiceStatus.Sent,
                PaymentStatus = PaymentStatus.Partial,
                DueDate = now.AddMonths(6),
                Notes = "Full treatment invoice for Damon braces - 24 month plan.",
                ItemsJson = "[{\"description\":\"Damon Q Brackets - Upper & Lower\",\"amount\":25000},{\"description\":\"Treatment Fee (24 months)\",\"amount\":20000}]",
                CreatedAt = now.AddDays(-150),
                CreatedBy = receptionistUserId
            },
            new Invoice
            {
                Id = invoice2Id,
                ClinicId = clinicId,
                InvoiceNumber = "INV-2026-002",
                PatientId = patient2Id,
                DoctorId = doctor2Id,
                TotalAmount = 35000m,
                PaidAmount = 35000m,
                RemainingAmount = 0m,
                Discount = 0m,
                Tax = 0m,
                Status = InvoiceStatus.Paid,
                PaymentStatus = PaymentStatus.Paid,
                DueDate = now.AddDays(-30),
                Notes = "Full payment received for Phase 1 orthodontic treatment.",
                ItemsJson = "[{\"description\":\"Rapid Palatal Expander\",\"amount\":8000},{\"description\":\"Ceramic Braces - Upper\",\"amount\":15000},{\"description\":\"Treatment Monitoring (18 months)\",\"amount\":12000}]",
                CreatedAt = now.AddDays(-75),
                CreatedBy = receptionistUserId
            },
            new Invoice
            {
                Id = invoice3Id,
                ClinicId = clinicId,
                InvoiceNumber = "INV-2026-003",
                PatientId = patient3Id,
                DoctorId = doctor1Id,
                TotalAmount = 52000m,
                PaidAmount = 40000m,
                RemainingAmount = 12000m,
                Discount = 2000m,
                Tax = 0m,
                Status = InvoiceStatus.Sent,
                PaymentStatus = PaymentStatus.Partial,
                DueDate = now.AddMonths(1),
                Notes = "Self-ligating braces with TADs. 2000 EGP loyalty discount applied.",
                ItemsJson = "[{\"description\":\"Self-Ligating Brackets - Upper & Lower\",\"amount\":30000},{\"description\":\"TADs (4 mini-screws)\",\"amount\":12000},{\"description\":\"Treatment Fee (15 months)\",\"amount\":12000}]",
                CreatedAt = now.AddDays(-330),
                CreatedBy = receptionistUserId
            },
            new Invoice
            {
                Id = invoice4Id,
                ClinicId = clinicId,
                InvoiceNumber = "INV-2026-004",
                PatientId = patient4Id,
                DoctorId = doctor2Id,
                TotalAmount = 40000m,
                PaidAmount = 15000m,
                RemainingAmount = 25000m,
                Discount = 0m,
                Tax = 0m,
                Status = InvoiceStatus.Sent,
                PaymentStatus = PaymentStatus.Partial,
                DueDate = now.AddMonths(12),
                Notes = "Invisalign Teen - 30 aligners. Payment plan: 37.5% upfront.",
                ItemsJson = "[{\"description\":\"Invisalign Teen - 30 aligners\",\"amount\":32000},{\"description\":\"Attachment bonding\",\"amount\":3000},{\"description\":\"Dental Monitoring subscription\",\"amount\":5000}]",
                CreatedAt = now.AddDays(-55),
                CreatedBy = receptionistUserId
            },
            new Invoice
            {
                Id = invoice5Id,
                ClinicId = clinicId,
                InvoiceNumber = "INV-2026-005",
                PatientId = patient5Id,
                DoctorId = doctor1Id,
                TotalAmount = 25000m,
                PaidAmount = 25000m,
                RemainingAmount = 0m,
                Discount = 0m,
                Tax = 0m,
                Status = InvoiceStatus.Paid,
                PaymentStatus = PaymentStatus.Paid,
                DueDate = now.AddDays(-60),
                Notes = "Invisalign Lite treatment completed. Full payment received.",
                ItemsJson = "[{\"description\":\"Invisalign Lite - 14 aligners\",\"amount\":20000},{\"description\":\"Fixed lingual retainer\",\"amount\":5000}]",
                CreatedAt = now.AddDays(-200),
                CreatedBy = receptionistUserId
            }
        };

        context.Invoices.AddRange(invoices);

        // ── 12. Payments ───────────────────────────────────────────────────
        var payments = new List<Payment>
        {
            new Payment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                InvoiceId = invoice1Id,
                PatientId = patient1Id,
                Amount = 22500m,
                PaymentMethod = PaymentMethod.BankTransfer,
                TransactionId = "TXN-BT-2026-001",
                Status = "Completed",
                Notes = "Initial 50% payment for Damon braces treatment",
                PaymentDate = now.AddDays(-148),
                CreatedAt = now.AddDays(-148),
                CreatedBy = receptionistUserId
            },
            new Payment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                InvoiceId = invoice2Id,
                PatientId = patient2Id,
                Amount = 35000m,
                PaymentMethod = PaymentMethod.Cash,
                TransactionId = "TXN-CS-2026-002",
                Status = "Completed",
                Notes = "Full payment in cash at clinic",
                PaymentDate = now.AddDays(-74),
                CreatedAt = now.AddDays(-74),
                CreatedBy = receptionistUserId
            },
            new Payment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                InvoiceId = invoice3Id,
                PatientId = patient3Id,
                Amount = 30000m,
                PaymentMethod = PaymentMethod.CreditCard,
                TransactionId = "TXN-CC-2026-003",
                Status = "Completed",
                Notes = "Initial payment via Visa ending in 4421",
                PaymentDate = now.AddDays(-328),
                CreatedAt = now.AddDays(-328),
                CreatedBy = receptionistUserId
            },
            new Payment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                InvoiceId = invoice3Id,
                PatientId = patient3Id,
                Amount = 10000m,
                PaymentMethod = PaymentMethod.BankTransfer,
                TransactionId = "TXN-BT-2026-004",
                Status = "Completed",
                Notes = "Monthly installment #1",
                PaymentDate = now.AddDays(-298),
                CreatedAt = now.AddDays(-298),
                CreatedBy = receptionistUserId
            },
            new Payment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                InvoiceId = invoice4Id,
                PatientId = patient4Id,
                Amount = 15000m,
                PaymentMethod = PaymentMethod.Insurance,
                TransactionId = "TXN-INS-2026-005",
                Status = "Completed",
                Notes = "Partial payment - AXA insurance coverage + patient co-pay",
                PaymentDate = now.AddDays(-53),
                CreatedAt = now.AddDays(-53),
                CreatedBy = receptionistUserId
            },
            new Payment
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                InvoiceId = invoice5Id,
                PatientId = patient5Id,
                Amount = 25000m,
                PaymentMethod = PaymentMethod.Online,
                TransactionId = "TXN-ON-2026-006",
                Status = "Completed",
                Notes = "Online payment via Fawry - full amount",
                PaymentDate = now.AddDays(-198),
                CreatedAt = now.AddDays(-198),
                CreatedBy = receptionistUserId
            }
        };

        context.Payments.AddRange(payments);

        // ── 13. Notifications ──────────────────────────────────────────────
        var notifications = new List<Notification>
        {
            new Notification
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                UserId = ownerUserId,
                Title = "Welcome to OrthoFlowAI",
                Message = "Your clinic has been set up successfully. Start by inviting your team and adding patients.",
                Type = NotificationType.Welcome,
                IsRead = true,
                ReadAt = now.AddDays(-14),
                Channel = NotificationChannel.InApp,
                CreatedAt = now.AddDays(-15),
                CreatedBy = superAdminUserId
            },
            new Notification
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                UserId = doctor1UserId,
                Title = "Appointment Completed",
                Message = "Treatment visit with Omar Khaled completed. Wire changed to 0.016 NiTi with power chain.",
                Type = NotificationType.TreatmentUpdate,
                IsRead = true,
                ReadAt = now.AddDays(-2),
                Channel = NotificationChannel.InApp,
                CreatedAt = now.AddDays(-2),
                CreatedBy = receptionistUserId
            },
            new Notification
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                UserId = ownerUserId,
                Title = "Payment Received",
                Message = "Payment of 22,500 EGP received from Omar Khaled for invoice INV-2026-001 via bank transfer.",
                Type = NotificationType.PaymentReceived,
                IsRead = true,
                ReadAt = now.AddDays(-5),
                Channel = NotificationChannel.InApp,
                MetadataJson = "{\"invoiceNumber\":\"INV-2026-001\",\"amount\":22500}",
                CreatedAt = now.AddDays(-6),
                CreatedBy = receptionistUserId
            },
            new Notification
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                UserId = doctor2UserId,
                Title = "Patient Compliance Alert",
                Message = "Nada Hussein compliance score dropped to 55%. Aligner #6 lost. Please review treatment progress.",
                Type = NotificationType.TreatmentUpdate,
                IsRead = false,
                Channel = NotificationChannel.InApp,
                MetadataJson = "{\"patientId\":\"" + patient4Id + "\",\"complianceScore\":55}",
                CreatedAt = now.AddDays(-1),
                CreatedBy = superAdminUserId
            },
            new Notification
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                UserId = receptionistUserId,
                Title = "Upcoming Appointments",
                Message = "5 appointments scheduled for tomorrow. Ensure all patient files are prepared.",
                Type = NotificationType.AppointmentReminder,
                IsRead = false,
                Channel = NotificationChannel.InApp,
                CreatedAt = now,
                CreatedBy = superAdminUserId
            }
        };

        context.Notifications.AddRange(notifications);

        // ── 14. Inventory Items ────────────────────────────────────────────
        var inventoryItems = new List<InventoryItem>
        {
            new InventoryItem
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                Name = "Damon Q Bracket Kit - Upper",
                Category = InventoryCategory.Brackets,
                Brand = "Ormco",
                Model = "Damon Q - MBT",
                Quantity = 24,
                MinQuantity = 10,
                UnitPrice = 350m,
                SupplierName = "Egypt Dental Supply Co.",
                SupplierPhone = "+20227775555",
                LastRestockedAt = now.AddDays(-20),
                CreatedAt = now.AddDays(-60),
                CreatedBy = ownerUserId
            },
            new InventoryItem
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                Name = "NiTi Archwire Kit 0.012-0.019",
                Category = InventoryCategory.Wires,
                Brand = "3M Unitek",
                Model = "Multistrand NiTi",
                Quantity = 50,
                MinQuantity = 20,
                UnitPrice = 85m,
                SupplierName = "Dental World Egypt",
                SupplierPhone = "+20228889999",
                LastRestockedAt = now.AddDays(-10),
                CreatedAt = now.AddDays(-90),
                CreatedBy = ownerUserId
            },
            new InventoryItem
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                Name = "Ceramic Brackets - Clarity SL",
                Category = InventoryCategory.Brackets,
                Brand = "3M Unitek",
                Model = "Clarity SL",
                Quantity = 8,
                MinQuantity = 10,
                UnitPrice = 500m,
                SupplierName = "Egypt Dental Supply Co.",
                SupplierPhone = "+20227775555",
                LastRestockedAt = now.AddDays(-45),
                CreatedAt = now.AddDays(-120),
                CreatedBy = ownerUserId
            },
            new InventoryItem
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                Name = "Orthodontic Elastics - Class II",
                Category = InventoryCategory.Elastics,
                Brand = "Alastick",
                Model = "Medium 3/8\" - 4.5oz",
                Quantity = 200,
                MinQuantity = 50,
                UnitPrice = 15m,
                SupplierName = "Dental World Egypt",
                SupplierPhone = "+20228889999",
                LastRestockedAt = now.AddDays(-15),
                CreatedAt = now.AddDays(-30),
                CreatedBy = receptionistUserId
            },
            new InventoryItem
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                Name = "Transbond XT Light Cure Adhesive",
                Category = InventoryCategory.Adhesive,
                Brand = "3M Unitek",
                Model = "Transbond XT",
                Quantity = 15,
                MinQuantity = 5,
                UnitPrice = 320m,
                SupplierName = "Egypt Dental Supply Co.",
                SupplierPhone = "+20227775555",
                LastRestockedAt = now.AddDays(-30),
                CreatedAt = now.AddDays(-60),
                CreatedBy = ownerUserId
            },
            new InventoryItem
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                Name = "Power Chain - Closed",
                Category = InventoryCategory.RubberBands,
                Brand = "G&H Orthodontics",
                Model = "Closed Link - Clear",
                Quantity = 30,
                MinQuantity = 10,
                UnitPrice = 45m,
                SupplierName = "Dental World Egypt",
                SupplierPhone = "+20228889999",
                LastRestockedAt = now.AddDays(-5),
                CreatedAt = now.AddDays(-40),
                CreatedBy = receptionistUserId
            },
            new InventoryItem
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                BranchId = branchId,
                Name = "Band Pusher & Ligature Director Kit",
                Category = InventoryCategory.Tools,
                Brand = "Hu-Friedy",
                Model = "Ortho Instrument Set",
                Quantity = 6,
                MinQuantity = 3,
                UnitPrice = 1200m,
                SupplierName = "Medical Instruments Egypt",
                SupplierPhone = "+20226664444",
                LastRestockedAt = now.AddDays(-90),
                CreatedAt = now.AddDays(-180),
                CreatedBy = ownerUserId
            }
        };

        context.InventoryItems.AddRange(inventoryItems);

        // ── 15. Subscription ───────────────────────────────────────────────
        var subscription = new Subscription
        {
            Id = subscriptionId,
            ClinicId = clinicId,
            Plan = SubscriptionPlan.Starter,
            Status = SubscriptionStatus.Active,
            BillingPeriod = BillingPeriod.Monthly,
            Amount = 999m,
            StartDate = now.AddDays(-30),
            EndDate = now.AddMonths(11),
            NextBillingDate = now.AddDays(1),
            TrialEndsAt = now.AddDays(-16),
            MaxPatients = 100,
            MaxStaff = 5,
            MaxBranches = 1,
            FeaturesJson = "[\"patient_management\",\"appointment_scheduling\",\"invoicing\",\"basic_ai_reports\",\"notifications\",\"inventory_tracking\"]",
            CreatedAt = now.AddDays(-30),
            CreatedBy = superAdminUserId
        };

        context.Subscriptions.Add(subscription);

        // ── 16. Feedbacks ──────────────────────────────────────────────────
        var feedbacks = new List<Feedback>
        {
            new Feedback
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                PatientId = patient5Id,
                DoctorId = doctor1Id,
                Rating = 5,
                Comment = "Excellent treatment experience with Dr. Ahmed. The Invisalign process was smooth and the results exceeded my expectations. The staff at Smile Perfect were always professional and welcoming.",
                Type = FeedbackType.Rating,
                NPSScore = 10,
                VisitDate = now.AddDays(-35),
                CreatedAt = now.AddDays(-34),
                CreatedBy = patient5Id
            },
            new Feedback
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                PatientId = patient2Id,
                DoctorId = doctor2Id,
                Rating = 5,
                Comment = "My daughter loves visiting Dr. Sara. She makes the orthodontic experience fun for kids. The ceramic braces look great and the expansion treatment was very effective.",
                Type = FeedbackType.Rating,
                NPSScore = 9,
                VisitDate = now.AddDays(-5),
                CreatedAt = now.AddDays(-4),
                CreatedBy = patient2Id
            }
        };

        context.Feedbacks.AddRange(feedbacks);

        // ── 17. Audit Logs ─────────────────────────────────────────────────
        var auditLogs = new List<AuditLog>
        {
            new AuditLog
            {
                Id = Guid.NewGuid().ToString(),
                UserId = ownerUserId,
                ClinicId = clinicId,
                Action = "Create",
                EntityName = "Clinic",
                EntityId = clinicId,
                NewValuesJson = "{\"name\":\"Smile Perfect Orthodontics\",\"email\":\"clinic@demo.com\"}",
                IpAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                CreatedAt = now.AddDays(-15)
            },
            new AuditLog
            {
                Id = Guid.NewGuid().ToString(),
                UserId = superAdminUserId,
                ClinicId = clinicId,
                Action = "Create",
                EntityName = "Subscription",
                EntityId = subscriptionId,
                NewValuesJson = "{\"plan\":\"Starter\",\"status\":\"Active\",\"amount\":999}",
                IpAddress = "10.0.0.1",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                CreatedAt = now.AddDays(-30)
            }
        };

        context.AuditLogs.AddRange(auditLogs);

        // ── Persist ────────────────────────────────────────────────────────
        await context.SaveChangesAsync();
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password)
    {
        var existing = await userManager.FindByEmailAsync(user.Email);
        if (existing is not null)
            return;

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create user {user.Email}: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
