using System.ComponentModel.DataAnnotations;
using OrthoFlowAI.Application.DTOs.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class CreatePatientRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public Gender? Gender { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? EmergencyContactName { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(200)]
    public string? InsuranceProvider { get; set; }

    [MaxLength(100)]
    public string? InsuranceNumber { get; set; }

    [MaxLength(2000)]
    public string? Diagnosis { get; set; }

    [MaxLength(2000)]
    public string? TreatmentPlanDescription { get; set; }

    public TreatmentType? TreatmentType { get; set; }

    public DateTime? TreatmentStartDate { get; set; }

    public DateTime? EstimatedEndDate { get; set; }

    public string? AssignedDoctorId { get; set; }

    public string? BranchId { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalTreatmentCost { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class UpdatePatientRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public Gender? Gender { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? EmergencyContactName { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(200)]
    public string? InsuranceProvider { get; set; }

    [MaxLength(100)]
    public string? InsuranceNumber { get; set; }

    [MaxLength(2000)]
    public string? Diagnosis { get; set; }

    [MaxLength(2000)]
    public string? TreatmentPlanDescription { get; set; }

    public TreatmentType? TreatmentType { get; set; }

    public DateTime? TreatmentStartDate { get; set; }

    public DateTime? EstimatedEndDate { get; set; }

    public PatientStatus? Status { get; set; }

    public string? AssignedDoctorId { get; set; }

    public string? BranchId { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? TotalTreatmentCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? AmountPaid { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class PatientResponse
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsuranceNumber { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentPlanDescription { get; set; }
    public TreatmentType? TreatmentType { get; set; }
    public DateTime? TreatmentStartDate { get; set; }
    public DateTime? EstimatedEndDate { get; set; }
    public PatientStatus Status { get; set; }
    public string? AssignedDoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? Tags { get; set; }
    public double JourneyScore { get; set; }
    public double ComplianceScore { get; set; }
    public decimal TotalTreatmentCost { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRemaining { get; set; }
    public string? Notes { get; set; }
    public string? QRCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PatientListResponse
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public PatientStatus Status { get; set; }
    public string? DoctorName { get; set; }
    public string? BranchName { get; set; }
    public TreatmentType? TreatmentType { get; set; }
    public decimal AmountRemaining { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PatientFilterRequest : PaginationRequest
{
    public PatientStatus? Status { get; set; }
    public string? AssignedDoctorId { get; set; }
    public string? BranchId { get; set; }
    public TreatmentType? TreatmentType { get; set; }
    public Gender? Gender { get; set; }
    public DateTime? TreatmentStartDateFrom { get; set; }
    public DateTime? TreatmentStartDateTo { get; set; }
    public DateTime? DateOfBirthFrom { get; set; }
    public DateTime? DateOfBirthTo { get; set; }
    public decimal? MinAmountRemaining { get; set; }
    public decimal? MaxAmountRemaining { get; set; }
}
