using System;
using System.Collections.Generic;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class PatientPortalSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentPlanDescription { get; set; }
    public TreatmentType? TreatmentType { get; set; }
    public DateTime? TreatmentStartDate { get; set; }
    public DateTime? EstimatedEndDate { get; set; }
    public double JourneyScore { get; set; }
    public double ComplianceScore { get; set; }
    public decimal TotalTreatmentCost { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRemaining { get; set; }
    public PortalStats Stats { get; set; } = new();
    public string? AssignedDoctorName { get; set; }
    public string? BranchName { get; set; }
}

public class PortalStats
{
    public int TotalAppointments { get; set; }
    public int UpcomingAppointments { get; set; }
    public int CompletedTreatments { get; set; }
    public int TotalPhotos { get; set; }
    public int UnpaidInvoices { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class PortalAppointmentResponse
{
    public string Id { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public AppointmentType Type { get; set; }
    public AppointmentStatus Status { get; set; }
    public int Duration { get; set; }
    public string? Notes { get; set; }
}

public class PortalInvoiceResponse
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance => TotalAmount - PaidAmount;
    public InvoiceStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? Notes { get; set; }
}

public class PortalPainReportRequest
{
    public PainLevel PainLevel { get; set; }
    public bool HasBrokenWire { get; set; }
    public bool HasLooseBracket { get; set; }
    public bool HasBleeding { get; set; }
    public bool HasSwelling { get; set; }
    public string? Description { get; set; }
}

public class PortalPainReportResponse
{
    public string Id { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public PainLevel PainLevel { get; set; }
    public bool HasBrokenWire { get; set; }
    public bool HasLooseBracket { get; set; }
    public bool HasBleeding { get; set; }
    public bool HasSwelling { get; set; }
    public string? Description { get; set; }
    public bool IsEmergency { get; set; }
    public bool DoctorNotified { get; set; }
}

public class PortalEmergencyRequest
{
    public EmergencySeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PortalEmergencyResponse
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EmergencySeverity Severity { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PortalMessageRequest
{
    public string Message { get; set; } = string.Empty;
}

public class PortalChatResponse
{
    public string Id { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string? SenderName { get; set; }
}
