using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Infrastructure.Services;

public interface IAIService
{
    Task<ApiResponse<string>> GenerateTreatmentReportAsync(TreatmentReportRequest request);
    Task<ApiResponse<string>> GenerateTreatmentSummaryAsync(string patientId, List<TreatmentVisitData> visits);
    Task<ApiResponse<string>> AnalyzePainReportAsync(string patientId, List<PainReportData> reports);
    Task<ApiResponse<List<string>>> GenerateRecommendationsAsync(string patientProfile);
    Task<ApiResponse<string>> GenerateClinicalNotesAsync(string appointmentId, ClinicalNotesRequest request);
}
