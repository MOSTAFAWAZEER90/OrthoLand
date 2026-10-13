using System.Collections.Generic;
using System.Threading.Tasks;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Application.Services;

public interface IReferralService
{
    Task<ApiResponse<ReferralResponse>> CreateReferralAsync(CreateReferralRequest request, string clinicId);
    Task<ApiResponse<ReferralResponse>> GetByCodeAsync(string code);
    Task<ApiResponse<ReferralResponse>> CompleteReferralAsync(string code, string referredPatientId);
    Task<ApiResponse<PagedResult<ReferralResponse>>> GetReferralsAsync(string clinicId, int page = 1, int pageSize = 20);
    Task<ApiResponse<ReferralStatsResponse>> GetReferrerStatsAsync(string referrerPatientId);
    Task<ApiResponse<ReferralProgramSettingsResponse>> GetProgramSettingsAsync(string clinicId);
    Task<ApiResponse> UpdateProgramSettingsAsync(string clinicId, ReferralProgramSettingsResponse settings);
}