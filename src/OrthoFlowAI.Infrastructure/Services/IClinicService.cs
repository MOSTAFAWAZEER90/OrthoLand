using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;

namespace OrthoFlowAI.Infrastructure.Services;

public interface IClinicService
{
    Task<ApiResponse<Clinic>> GetByIdAsync(string id);
    Task<ApiResponse<PagedResult<Clinic>>> GetAllAsync(int pageNumber, int pageSize, string? search = null, bool? isActive = null);
    Task<ApiResponse<Clinic>> CreateAsync(Clinic clinic);
    Task<ApiResponse<Clinic>> UpdateAsync(Clinic clinic);
    Task<ApiResponse> DeleteAsync(string id);
    Task<ApiResponse<bool>> ToggleActiveAsync(string id);
}
