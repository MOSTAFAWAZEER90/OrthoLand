using System;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class InventoryDto
{
    public string Name { get; set; } = string.Empty;
    public InventoryCategory Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int Quantity { get; set; }
    public int MinQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierPhone { get; set; }
    public string? BranchId { get; set; }
}

public class RestockRequest
{
    public int Quantity { get; set; }
}

public class BranchDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsMainBranch { get; set; }
    public string? OperatingHours { get; set; }
}

public class StaffResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public StaffStatus Status { get; set; }
    public DateTime? HireDate { get; set; }
    public decimal? Salary { get; set; }
}

public class StaffDto
{
    public string UserId { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public StaffStatus Status { get; set; } = StaffStatus.Active;
    public DateTime? HireDate { get; set; }
    public decimal? Salary { get; set; }
}

public class StaffUpdateDto
{
    public string? Position { get; set; }
    public string? Department { get; set; }
    public StaffStatus? Status { get; set; }
    public DateTime? HireDate { get; set; }
    public decimal? Salary { get; set; }
}

public class AnalyticsResponse
{
    public int PatientCount { get; set; }
    public int AppointmentCount { get; set; }
    public int CompletedAppointments { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PhotoCount { get; set; }
    public int InventoryCount { get; set; }
    public int LowStockCount { get; set; }
    public int StaffCount { get; set; }
    public int BranchCount { get; set; }
    public int ActiveTreatmentCount { get; set; }
    public int CompletedTreatmentCount { get; set; }
    public decimal AverageJourneyScore { get; set; }
}
