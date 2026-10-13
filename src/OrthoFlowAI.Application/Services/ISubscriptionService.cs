using System.Collections.Generic;
using System.Threading.Tasks;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Application.DTOs;

namespace OrthoFlowAI.Application.Services;

public interface ISubscriptionService
{
    Task<ApiResponse<SubscriptionCheckoutResponse>> CreateCheckoutSessionAsync(string clinicId, string priceId, string successUrl, string cancelUrl);
    Task<ApiResponse<SubscriptionPortalResponse>> CreateBillingPortalSessionAsync(string clinicId, string returnUrl);
    Task<ApiResponse> HandleWebhookAsync(string json, string signature);
    Task<ApiResponse<ClinicSubscriptionStatusResponse>> GetSubscriptionStatusAsync(string clinicId);
    Task<ApiResponse<List<SubscriptionPlanResponse>>> GetAvailablePlansAsync();
}