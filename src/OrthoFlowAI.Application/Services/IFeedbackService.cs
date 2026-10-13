using System.Collections.Generic;
using System.Threading.Tasks;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Application.Services;

public interface IFeedbackService
{
    Task<ApiResponse<FeedbackResponse>> CreateFeedbackAsync(CreateFeedbackRequest request);
    Task<ApiResponse<PagedResult<FeedbackResponse>>> GetAllAsync(FeedbackFilterRequest filter);
    Task<ApiResponse<FeedbackResponse>> GetByIdAsync(string id);
    Task<ApiResponse<FeedbackAnalyticsResponse>> GetAnalyticsAsync(string clinicId, DateTime? from = null, DateTime? to = null);
    Task<ApiResponse<NPSStatsResponse>> GetNPSStatsAsync(string clinicId, DateTime? from = null, DateTime? to = null);
    Task<ApiResponse> DeleteAsync(string id);
}