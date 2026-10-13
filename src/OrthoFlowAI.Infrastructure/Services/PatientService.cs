using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly IUnitOfWork _unitOfWork;

    public PatientService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Patient>> GetByIdAsync(string id)
    {
        var patients = await _unitOfWork.Patients.FindAsync(
            p => p.Id == id,
            p => p.Doctor!,
            p => p.Branch!,
            p => p.Clinic!);

        var patient = patients.FirstOrDefault();
        if (patient is null)
        {
            return ApiResponse<Patient>.FailResponse("Patient not found.");
        }

        return ApiResponse<Patient>.SuccessResponse(patient);
    }

    public async Task<ApiResponse<PagedResult<Patient>>> GetAllAsync(
        int pageNumber, int pageSize, string? search = null, PatientStatus? status = null,
        string? doctorId = null, string? branchId = null, TreatmentType? treatmentType = null)
    {
        Expression<Func<Patient, bool>>? filter = BuildFilter(search, status, doctorId, branchId, treatmentType);

        var result = await _unitOfWork.Patients.GetPagedAsync(
            pageNumber,
            pageSize,
            filter,
            q => q.OrderByDescending(p => p.CreatedAt));

        return ApiResponse<PagedResult<Patient>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<Patient>> CreateAsync(Patient patient)
    {
        patient.CreatedAt = DateTime.UtcNow;
        patient.Status = PatientStatus.Active;
        patient.AmountRemaining = patient.TotalTreatmentCost - patient.AmountPaid;

        await _unitOfWork.Patients.AddAsync(patient);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Patient>.SuccessResponse(patient, "Patient created successfully.");
    }

    public async Task<ApiResponse<Patient>> UpdateAsync(Patient patient)
    {
        var existing = await _unitOfWork.Patients.GetByIdAsync(patient.Id);
        if (existing is null)
        {
            return ApiResponse<Patient>.FailResponse("Patient not found.");
        }

        existing.FirstName = patient.FirstName;
        existing.LastName = patient.LastName;
        existing.Email = patient.Email;
        existing.Phone = patient.Phone;
        existing.DateOfBirth = patient.DateOfBirth;
        existing.Gender = patient.Gender;
        existing.Address = patient.Address;
        existing.City = patient.City;
        existing.EmergencyContactName = patient.EmergencyContactName;
        existing.EmergencyContactPhone = patient.EmergencyContactPhone;
        existing.InsuranceProvider = patient.InsuranceProvider;
        existing.InsuranceNumber = patient.InsuranceNumber;
        existing.Diagnosis = patient.Diagnosis;
        existing.TreatmentPlanDescription = patient.TreatmentPlanDescription;
        existing.TreatmentType = patient.TreatmentType;
        existing.TreatmentStartDate = patient.TreatmentStartDate;
        existing.EstimatedEndDate = patient.EstimatedEndDate;
        existing.Status = patient.Status;
        existing.BranchId = patient.BranchId;
        existing.Tags = patient.Tags;
        existing.TotalTreatmentCost = patient.TotalTreatmentCost;
        existing.AmountPaid = patient.AmountPaid;
        existing.AmountRemaining = patient.TotalTreatmentCost - patient.AmountPaid;
        existing.Notes = patient.Notes;

        _unitOfWork.Patients.Update(existing);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Patient>.SuccessResponse(existing, "Patient updated successfully.");
    }

    public async Task<ApiResponse> DeleteAsync(string id)
    {
        var patient = await _unitOfWork.Patients.GetByIdAsync(id);
        if (patient is null)
        {
            return ApiResponse.FailResponse("Patient not found.");
        }

        patient.IsDeleted = true;
        _unitOfWork.Patients.Update(patient);
        await _unitOfWork.CommitAsync();

        return ApiResponse.SuccessResponse("Patient deleted successfully.");
    }

    public async Task<ApiResponse<Patient>> AssignDoctorAsync(string patientId, string doctorId)
    {
        var patient = await _unitOfWork.Patients.GetByIdAsync(patientId);
        if (patient is null)
        {
            return ApiResponse<Patient>.FailResponse("Patient not found.");
        }

        var doctorExists = await _unitOfWork.Doctors.AnyAsync(d => d.Id == doctorId);
        if (!doctorExists)
        {
            return ApiResponse<Patient>.FailResponse("Doctor not found.");
        }

        patient.AssignedDoctorId = doctorId;
        _unitOfWork.Patients.Update(patient);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Patient>.SuccessResponse(patient, "Doctor assigned successfully.");
    }

    public async Task<ApiResponse<decimal>> UpdatePaymentSummaryAsync(string patientId)
    {
        var patient = await _unitOfWork.Patients.GetByIdAsync(patientId);
        if (patient is null)
        {
            return ApiResponse<decimal>.FailResponse("Patient not found.");
        }

        var invoices = await _unitOfWork.Invoices.FindAsync(i => i.PatientId == patientId && !i.IsDeleted);
        var totalPaid = invoices.Sum(i => i.PaidAmount);
        var totalCost = patient.TotalTreatmentCost;

        patient.AmountPaid = totalPaid;
        patient.AmountRemaining = totalCost - totalPaid;

        _unitOfWork.Patients.Update(patient);
        await _unitOfWork.CommitAsync();

        return ApiResponse<decimal>.SuccessResponse(patient.AmountRemaining, "Payment summary updated.");
    }

    private static Expression<Func<Patient, bool>>? BuildFilter(
        string? search, PatientStatus? status, string? doctorId, string? branchId, TreatmentType? treatmentType)
    {
        Expression<Func<Patient, bool>>? filter = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filter = CombineFilters(filter, p =>
                p.FirstName.Contains(search) ||
                p.LastName.Contains(search) ||
                (p.Email != null && p.Email.Contains(search)) ||
                (p.Phone != null && p.Phone.Contains(search)));
        }

        if (status.HasValue)
        {
            filter = CombineFilters(filter, p => p.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(doctorId))
        {
            filter = CombineFilters(filter, p => p.AssignedDoctorId == doctorId);
        }

        if (!string.IsNullOrWhiteSpace(branchId))
        {
            filter = CombineFilters(filter, p => p.BranchId == branchId);
        }

        if (treatmentType.HasValue)
        {
            filter = CombineFilters(filter, p => p.TreatmentType == treatmentType.Value);
        }

        return filter;
    }

    private static Expression<Func<Patient, bool>>? CombineFilters(
        Expression<Func<Patient, bool>>? first, Expression<Func<Patient, bool>> second)
    {
        if (first is null)
        {
            return second;
        }

        var parameter = Expression.Parameter(typeof(Patient), "p");
        var body = Expression.AndAlso(
            Expression.Invoke(first, parameter),
            Expression.Invoke(second, parameter));

        return Expression.Lambda<Func<Patient, bool>>(body, parameter);
    }
}
