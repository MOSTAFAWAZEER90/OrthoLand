using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;
using OrthoFlowAI.Infrastructure.Data;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/super-admin/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminClinicsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SuperAdminClinicsController> _logger;

    public SuperAdminClinicsController(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<SuperAdminClinicsController> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all clinics with pagination and filtering (Super Admin only)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClinicSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] SuperAdminClinicFilterRequest filter)
    {
        _logger.LogInformation("Super Admin getting all clinics - Page: {PageNumber}, Size: {PageSize}", filter.PageNumber, filter.PageSize);

        var query = _context.Clinics.Where(c => !c.IsDeleted).AsQueryable();

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(c => c.Name.Contains(filter.SearchTerm) || c.Email.Contains(filter.SearchTerm));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == filter.IsActive.Value);
        }

        if (!string.IsNullOrEmpty(filter.SubscriptionPlan))
        {
            query = query.Where(c => c.SubscriptionPlan == filter.SubscriptionPlan);
        }

        if (!string.IsNullOrEmpty(filter.SubscriptionStatus))
        {
            query = query.Where(c => c.SubscriptionStatus == filter.SubscriptionStatus);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new ClinicSummaryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Logo = c.Logo,
                Email = c.Email,
                Phone = c.Phone,
                City = c.City,
                Country = c.Country,
                SubscriptionPlan = c.SubscriptionPlan,
                SubscriptionStatus = c.SubscriptionStatus,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                TotalPatients = _context.Patients.Count(p => p.ClinicId == c.Id && !p.IsDeleted),
                TotalDoctors = _context.Doctors.Count(d => d.ClinicId == c.Id && !d.IsDeleted),
                TotalBranches = _context.Branches.Count(b => b.ClinicId == c.Id && !b.IsDeleted),
                TotalAppointments = _context.Appointments.Count(a => a.ClinicId == c.Id && !a.IsDeleted),
                TotalRevenue = _context.Invoices.Where(i => i.ClinicId == c.Id && !i.IsDeleted && i.PaymentStatus == PaymentStatus.Paid).Sum(i => i.TotalAmount)
            })
            .ToListAsync();

        var result = new PagedResult<ClinicSummaryResponse>
        {
            Items = items,
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        return Ok(ApiResponse<PagedResult<ClinicSummaryResponse>>.SuccessResponse(result, "Clinics retrieved"));
    }

    /// <summary>
    /// Get a clinic by ID (Super Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ClinicDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogInformation("Super Admin getting clinic: {ClinicId}", id);

        var clinic = await _context.Clinics.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (clinic == null)
        {
            return NotFound(ApiResponse.FailResponse("Clinic not found"));
        }

        var response = new ClinicDetailResponse
        {
            Id = clinic.Id,
            Name = clinic.Name,
            Logo = clinic.Logo,
            Phone = clinic.Phone,
            Email = clinic.Email,
            Address = clinic.Address,
            City = clinic.City,
            Country = clinic.Country,
            Website = clinic.Website,
            BusinessHours = clinic.BusinessHours,
            WorkingDays = clinic.WorkingDays,
            PrimaryColor = clinic.PrimaryColor,
            SecondaryColor = clinic.SecondaryColor,
            SubscriptionPlan = clinic.SubscriptionPlan,
            SubscriptionStatus = clinic.SubscriptionStatus,
            Language = clinic.Language,
            IsActive = clinic.IsActive,
            CreatedAt = clinic.CreatedAt,
            Stats = new ClinicStats
            {
                TotalPatients = await _context.Patients.CountAsync(p => p.ClinicId == clinic.Id && !p.IsDeleted),
                TotalDoctors = await _context.Doctors.CountAsync(d => d.ClinicId == clinic.Id && !d.IsDeleted),
                TotalStaff = await _context.Staff.CountAsync(s => s.ClinicId == clinic.Id && !s.IsDeleted),
                TotalBranches = await _context.Branches.CountAsync(b => b.ClinicId == clinic.Id && !b.IsDeleted),
                TotalAppointments = await _context.Appointments.CountAsync(a => a.ClinicId == clinic.Id && !a.IsDeleted),
                TotalInvoices = await _context.Invoices.CountAsync(i => i.ClinicId == clinic.Id && !i.IsDeleted),
                TotalRevenue = await _context.Invoices.Where(i => i.ClinicId == clinic.Id && !i.IsDeleted && i.PaymentStatus == PaymentStatus.Paid).SumAsync(i => i.TotalAmount),
                OutstandingBalance = await _context.Invoices.Where(i => i.ClinicId == clinic.Id && !i.IsDeleted && (i.PaymentStatus == PaymentStatus.Pending || i.PaymentStatus == PaymentStatus.Partial || i.PaymentStatus == PaymentStatus.Overdue)).SumAsync(i => i.TotalAmount - i.PaidAmount),
                ActiveSubscriptions = await _context.Subscriptions.CountAsync(s => s.ClinicId == clinic.Id && s.Status == SubscriptionStatus.Active)
            }
        };

        return Ok(ApiResponse<ClinicDetailResponse>.SuccessResponse(response, "Clinic retrieved"));
    }

    /// <summary>
    /// Create a new clinic (Super Admin only)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Clinic>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateClinicRequest request)
    {
        _logger.LogInformation("Super Admin creating clinic: {Name}", request.Name);

        var clinic = new Clinic
        {
            Name = request.Name,
            Logo = request.Logo,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            Website = request.Website,
            BusinessHours = request.BusinessHours,
            WorkingDays = request.WorkingDays,
            PrimaryColor = request.PrimaryColor ?? "#6366f1",
            SecondaryColor = request.SecondaryColor ?? "#06b6d4",
            SubscriptionPlan = request.SubscriptionPlan ?? "Starter",
            SubscriptionStatus = request.SubscriptionStatus ?? "Trial",
            Language = request.Language ?? "English",
            IsActive = true
        };

        await _unitOfWork.Clinics.AddAsync(clinic);
        await _unitOfWork.CommitAsync();

        // Create default subscription
        var planEnum = Enum.TryParse<SubscriptionPlan>(clinic.SubscriptionPlan, out var sp) ? sp : SubscriptionPlan.Starter;
        var statusEnum = Enum.TryParse<SubscriptionStatus>(clinic.SubscriptionStatus, out var ss) ? ss : SubscriptionStatus.Trial;

        var subscription = new Subscription
        {
            ClinicId = clinic.Id,
            Plan = planEnum,
            Status = statusEnum,
            BillingPeriod = BillingPeriod.Monthly,
            Amount = planEnum switch
            {
                SubscriptionPlan.Professional => 299,
                SubscriptionPlan.Enterprise => 999,
                _ => 99
            },
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            MaxPatients = planEnum switch
            {
                SubscriptionPlan.Professional => 1000,
                SubscriptionPlan.Enterprise => -1, // unlimited
                _ => 300
            },
            MaxStaff = planEnum switch
            {
                SubscriptionPlan.Professional => 10,
                SubscriptionPlan.Enterprise => -1,
                _ => 2
            },
            MaxBranches = planEnum switch
            {
                SubscriptionPlan.Professional => 5,
                SubscriptionPlan.Enterprise => -1,
                _ => 1
            },
            FeaturesJson = System.Text.Json.JsonSerializer.Serialize(GetPlanFeatures(clinic.SubscriptionPlan ?? "Starter"))
        };
        subscription.CreatedBy = "super-admin";
        await _unitOfWork.Subscriptions.AddAsync(subscription);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Clinic created: {Id}", clinic.Id);
        return CreatedAtAction(nameof(GetById), new { id = clinic.Id }, ApiResponse<Clinic>.SuccessResponse(clinic, "Clinic created"));
    }

    /// <summary>
    /// Update a clinic (Super Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Clinic>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateClinicRequest request)
    {
        _logger.LogInformation("Super Admin updating clinic: {ClinicId}", id);

        var clinic = await _unitOfWork.Clinics.GetByIdAsync(id);
        if (clinic == null || clinic.IsDeleted)
        {
            return NotFound(ApiResponse.FailResponse("Clinic not found"));
        }

        if (request.Name != null) clinic.Name = request.Name;
        if (request.Logo != null) clinic.Logo = request.Logo;
        if (request.Phone != null) clinic.Phone = request.Phone;
        if (request.Email != null) clinic.Email = request.Email;
        if (request.Address != null) clinic.Address = request.Address;
        if (request.City != null) clinic.City = request.City;
        if (request.Country != null) clinic.Country = request.Country;
        if (request.Website != null) clinic.Website = request.Website;
        if (request.BusinessHours != null) clinic.BusinessHours = request.BusinessHours;
        if (request.WorkingDays != null) clinic.WorkingDays = request.WorkingDays;
        if (request.PrimaryColor != null) clinic.PrimaryColor = request.PrimaryColor;
        if (request.SecondaryColor != null) clinic.SecondaryColor = request.SecondaryColor;
        if (request.SubscriptionPlan != null) clinic.SubscriptionPlan = request.SubscriptionPlan;
        if (request.SubscriptionStatus != null) clinic.SubscriptionStatus = request.SubscriptionStatus;
        if (request.Language != null) clinic.Language = request.Language;
        if (request.IsActive.HasValue) clinic.IsActive = request.IsActive.Value;

        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = "super-admin";

        _unitOfWork.Clinics.Update(clinic);
        await _unitOfWork.CommitAsync();

        return Ok(ApiResponse<Clinic>.SuccessResponse(clinic, "Clinic updated"));
    }

    /// <summary>
    /// Activate/Deactivate a clinic (Super Admin only)
    /// </summary>
    [HttpPost("{id}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        _logger.LogInformation("Super Admin toggling clinic status: {ClinicId}", id);

        var clinic = await _unitOfWork.Clinics.GetByIdAsync(id);
        if (clinic == null || clinic.IsDeleted)
        {
            return NotFound(ApiResponse.FailResponse("Clinic not found"));
        }

        clinic.IsActive = !clinic.IsActive;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = "super-admin";

        _unitOfWork.Clinics.Update(clinic);
        await _unitOfWork.CommitAsync();

        return Ok(ApiResponse.SuccessResponse($"Clinic {(clinic.IsActive ? "activated" : "deactivated")}"));
    }

    /// <summary>
    /// Get all clinics with pagination and filtering (POST for complex filters)
    /// </summary>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClinicSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Filter([FromBody] SuperAdminClinicFilterRequest filter)
    {
        return await GetAll(filter);
    }

    /// <summary>
    /// Get global system statistics (Super Admin only)
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<SuperAdminStatsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        _logger.LogInformation("Super Admin getting global stats");

        var stats = new SuperAdminStatsResponse
        {
            TotalClinics = await _context.Clinics.CountAsync(c => !c.IsDeleted),
            ActiveClinics = await _context.Clinics.CountAsync(c => !c.IsDeleted && c.IsActive),
            TotalPatients = await _context.Patients.CountAsync(p => !p.IsDeleted),
            TotalDoctors = await _context.Doctors.CountAsync(d => !d.IsDeleted),
            TotalAppointments = await _context.Appointments.CountAsync(a => !a.IsDeleted),
            TotalRevenue = await _context.Invoices.Where(i => !i.IsDeleted && i.PaymentStatus == PaymentStatus.Paid).SumAsync(i => i.TotalAmount),
            MonthlyRevenue = await _context.Invoices
                .Where(i => !i.IsDeleted && i.PaymentStatus == PaymentStatus.Paid && i.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                .SumAsync(i => i.TotalAmount),
            SubscriptionsByPlan = await _context.Subscriptions
                .Where(s => !s.IsDeleted)
                .GroupBy(s => s.Plan)
                .Select(g => new SubscriptionPlanStat { Plan = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(),
            TopClinicsByRevenue = await _context.Clinics
                .Where(c => !c.IsDeleted)
                .Select(c => new ClinicRevenueStat
                {
                    ClinicId = c.Id,
                    ClinicName = c.Name,
                    Revenue = _context.Invoices.Where(i => i.ClinicId == c.Id && !i.IsDeleted && i.PaymentStatus == PaymentStatus.Paid).Sum(i => i.TotalAmount),
                    PatientCount = _context.Patients.Count(p => p.ClinicId == c.Id && !p.IsDeleted)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync()
        };

        return Ok(ApiResponse<SuperAdminStatsResponse>.SuccessResponse(stats, "Global stats retrieved"));
    }

    private static List<string> GetPlanFeatures(string plan) => plan switch
    {
        "Starter" => new List<string> { "Up to 300 patients", "Appointment reminders", "Basic photo management", "2 users", "1 branch" },
        "Professional" => new List<string> { "Unlimited patients", "WhatsApp integration", "AI Reports", "10 users", "5 branches", "Advanced analytics", "Inventory management" },
        "Enterprise" => new List<string> { "Unlimited everything", "Custom branding", "API access", "Dedicated support", "Custom integrations", "White-label mobile app" },
        _ => new List<string> { "Basic features" }
    };
}

public class SuperAdminClinicFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public string? SubscriptionPlan { get; set; }
    public string? SubscriptionStatus { get; set; }
}

public class ClinicSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? SubscriptionPlan { get; set; }
    public string? SubscriptionStatus { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalBranches { get; set; }
    public int TotalAppointments { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class ClinicDetailResponse
{
    public string Id { get; set; } = string.Empty;
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
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public ClinicStats Stats { get; set; } = new();
}

public class ClinicStats
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalStaff { get; set; }
    public int TotalBranches { get; set; }
    public int TotalAppointments { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int ActiveSubscriptions { get; set; }
}

public class SuperAdminStatsResponse
{
    public int TotalClinics { get; set; }
    public int ActiveClinics { get; set; }
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalAppointments { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public List<SubscriptionPlanStat> SubscriptionsByPlan { get; set; } = new();
    public List<ClinicRevenueStat> TopClinicsByRevenue { get; set; } = new();
}

public class SubscriptionPlanStat
{
    public string Plan { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ClinicRevenueStat
{
    public string ClinicId { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int PatientCount { get; set; }
}

public class CreateClinicRequest
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
    public string? Language { get; set; }
}

public class UpdateClinicRequest
{
    public string? Name { get; set; }
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
    public string? Language { get; set; }
    public bool? IsActive { get; set; }
}