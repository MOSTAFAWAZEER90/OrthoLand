using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Infrastructure.Settings;

namespace OrthoFlowAI.Infrastructure.Services;

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAISettings _settings;
    private readonly ILogger<AIService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AIService(HttpClient httpClient, IOptions<OpenAISettings> settings, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    public async Task<ApiResponse<string>> GenerateTreatmentReportAsync(TreatmentReportRequest request)
    {
        var visitSummary = string.Join("\n", request.Visits.Select(v =>
            $"- Date: {v.VisitDate:yyyy-MM-dd}, Notes: {v.DoctorNotes ?? "N/A"}, " +
            $"Wire Changed: {v.WireChanged}, Bracket Replaced: {v.BracketReplaced}, " +
            $"Rubber Bands: {v.RubberBandsUsed}, Pain: {v.PainLevel ?? "N/A"}, " +
            $"Medication: {v.Medication ?? "N/A"}"));

        var prompt = $"""
            Generate a comprehensive orthodontic treatment report for patient {request.PatientName}.
            
            Diagnosis: {request.Diagnosis}
            Treatment Type: {request.TreatmentType}
            Treatment Start Date: {request.StartDate:yyyy-MM-dd}
            
            Visit History:
            {visitSummary}
            
            {(string.IsNullOrEmpty(request.AdditionalNotes) ? "" : $"Additional Notes: {request.AdditionalNotes}")}
            
            Please provide:
            1. Treatment progress summary
            2. Key milestones achieved
            3. Current treatment phase
            4. Recommendations for next steps
            5. Estimated time to completion
            """;

        return await CallOpenAIAsync(prompt, "You are an experienced orthodontist assistant that generates professional treatment reports.");
    }

    public async Task<ApiResponse<string>> GenerateTreatmentSummaryAsync(string patientId, List<TreatmentVisitData> visits)
    {
        var visitSummary = string.Join("\n", visits.Select(v =>
            $"- {v.VisitDate:yyyy-MM-dd}: Pain {v.PainLevel}, " +
            $"Wire Changed: {v.WireChanged}, Bracket Replaced: {v.BracketReplaced}"));

        var prompt = $"""
            Generate a concise treatment summary for patient {patientId} based on {visits.Count} visits:
            
            {visitSummary}
            
            Provide a brief, professional summary highlighting progress and current status.
            """;

        return await CallOpenAIAsync(prompt, "You are an orthodontic assistant providing treatment summaries.");
    }

    public async Task<ApiResponse<string>> AnalyzePainReportAsync(string patientId, List<PainReportData> reports)
    {
        var reportSummary = string.Join("\n", reports.Select(r =>
            $"- {r.ReportDate:yyyy-MM-dd}: Pain {r.PainLevel}, " +
            $"Broken Wire: {r.HasBrokenWire}, Loose Bracket: {r.HasLooseBracket}, " +
            $"Bleeding: {r.HasBleeding}, Swelling: {r.HasSwelling}, " +
            $"Description: {r.Description ?? "N/A"}"));

        var prompt = $"""
            Analyze pain reports for patient {patientId}:
            
            {reportSummary}
            
            Please provide:
            1. Pain trend analysis
            2. Potential causes
            3. Recommended actions
            4. Whether urgent attention is needed
            """;

        return await CallOpenAIAsync(prompt, "You are an orthodontic pain specialist analyzing patient reports.");
    }

    public async Task<ApiResponse<List<string>>> GenerateRecommendationsAsync(string patientProfile)
    {
        var prompt = $"""
            Based on the following patient profile, generate orthodontic treatment recommendations:
            
            {patientProfile}
            
            Provide 5-10 specific, actionable recommendations as a numbered list.
            """;

        var result = await CallOpenAIAsync(prompt, "You are an orthodontic treatment planning specialist.");
        if (!result.Success)
        {
            return ApiResponse<List<string>>.FailResponse(result.Message, result.Errors);
        }

        var recommendations = result.Data!
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ' ', '-', ')'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        return ApiResponse<List<string>>.SuccessResponse(recommendations);
    }

    public async Task<ApiResponse<string>> GenerateClinicalNotesAsync(string appointmentId, ClinicalNotesRequest request)
    {
        var prompt = $"""
            Generate professional clinical notes for an orthodontic appointment:
            
            Patient: {request.PatientName}
            Appointment Type: {request.AppointmentType}
            {(string.IsNullOrEmpty(request.Observations) ? "" : $"Observations: {request.Observations}")}
            {(string.IsNullOrEmpty(request.ProceduresPerformed) ? "" : $"Procedures Performed: {request.ProceduresPerformed}")}
            {(string.IsNullOrEmpty(request.NextSteps) ? "" : $"Next Steps: {request.NextSteps}")}
            
            Format as professional clinical notes following standard orthodontic documentation.
            """;

        return await CallOpenAIAsync(prompt, "You are an orthodontist generating clinical notes. Be professional and precise.");
    }

    private async Task<ApiResponse<string>> CallOpenAIAsync(string userPrompt, string systemPrompt)
    {
        try
        {
            var requestBody = new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = _settings.MaxTokens,
                temperature = _settings.Temperature
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error {StatusCode}: {Body}", response.StatusCode, errorBody);
                return ApiResponse<string>.FailResponse($"AI service returned status {response.StatusCode}.");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                return ApiResponse<string>.FailResponse("AI service returned no response.");
            }

            var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return ApiResponse<string>.FailResponse("AI service returned empty content.");
            }

            return ApiResponse<string>.SuccessResponse(messageContent.Trim());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling OpenAI API");
            return ApiResponse<string>.FailResponse("AI service is currently unavailable. Please try again later.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "OpenAI API request timed out");
            return ApiResponse<string>.FailResponse("AI service request timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling OpenAI API");
            return ApiResponse<string>.FailResponse("An unexpected error occurred with the AI service.");
        }
    }
}
