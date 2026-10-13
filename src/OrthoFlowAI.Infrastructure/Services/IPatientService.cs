using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Infrastructure.Services;

public interface IPatientService
{
    Task<ApiResponse<Patient>> GetByIdAsync(string id);
    Task<ApiResponse<PagedResult<Patient>>> GetAllAsync(
        int pageNumber, int pageSize, string? search = null, PatientStatus? status = null,
        string? doctorId = null, string? branchId = null, TreatmentType? treatmentType = null);
    Task<ApiResponse<Patient>> CreateAsync(Patient patient);
    Task<ApiResponse<Patient>> UpdateAsync(Patient patient);
    Task<ApiResponse> DeleteAsync(string id);
    Task<ApiResponse<Patient>> AssignDoctorAsync(string patientId, string doctorId);
    Task<ApiResponse<decimal>> UpdatePaymentSummaryAsync(string patientId);
}
