using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;
using OrthoFlowAI.Infrastructure.Data;

namespace OrthoFlowAI.Infrastructure.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<FeedbackService> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<FeedbackResponse>> CreateFeedbackAsync(CreateFeedbackRequest request)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            if (string.IsNullOrEmpty(clinicId))
                return ApiResponse<FeedbackResponse>.FailResponse("Clinic context not found");

            var patient = await _unitOfWork.Patients.GetByIdAsync(request.PatientId);
            if (patient == null || patient.IsDeleted || patient.ClinicId != clinicId)
                return ApiResponse<FeedbackResponse>.FailResponse("Patient not found");

            var feedback = new Feedback
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                AppointmentId = request.AppointmentId,
                Rating = request.Rating,
                Comment = request.Comment,
                Type = request.Type,
                NPSScore = request.NPSScore,
                NPSComment = request.NPSComment,
                VisitDate = request.VisitDate,
                IsAnonymous = request.IsAnonymous,
                Tags = request.Tags,
                Sentiment = AnalyzeSentiment(request.Rating, request.Comment, request.NPSScore),
                ClinicId = clinicId
            };
            feedback.CreatedBy = request.IsAnonymous ? "anonymous" : "patient";

            await _unitOfWork.Feedbacks.AddAsync(feedback);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Feedback created: {Id} by patient {PatientId}", feedback.Id, request.PatientId);

            // Notify doctor if rating is low
            if (request.Rating <= 2 && !string.IsNullOrEmpty(request.DoctorId))
            {
                var doctor = await _unitOfWork.Doctors.GetByIdAsync(request.DoctorId);
                if (doctor != null && !string.IsNullOrEmpty(doctor.UserId))
                {
                    await CreateNotification(doctor.UserId, clinicId,
                        "Low Rating Received",
                        $"You received a {request.Rating}-star rating. Please review the feedback.",
                        NotificationType.General);
                }
            }

            return ApiResponse<FeedbackResponse>.SuccessResponse(MapToResponse(feedback), "Feedback submitted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feedback");
            return ApiResponse<FeedbackResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PagedResult<FeedbackResponse>>> GetAllAsync(FeedbackFilterRequest filter)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            if (string.IsNullOrEmpty(clinicId))
                return ApiResponse<PagedResult<FeedbackResponse>>.FailResponse("Clinic context not found");

            var query = _context.Feedbacks
                .Include(f => f.Patient)
                .Include(f => f.Doctor)
                    .ThenInclude(d => d.User)
                .Where(f => f.ClinicId == clinicId && !f.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.PatientId))
                query = query.Where(f => f.PatientId == filter.PatientId);

            if (!string.IsNullOrEmpty(filter.DoctorId))
                query = query.Where(f => f.DoctorId == filter.DoctorId);

            if (filter.Type.HasValue)
                query = query.Where(f => f.Type == filter.Type.Value);

            if (filter.Sentiment.HasValue)
                query = query.Where(f => f.Sentiment == filter.Sentiment.Value);

            if (filter.MinRating.HasValue)
                query = query.Where(f => f.Rating >= filter.MinRating.Value);

            if (filter.MaxRating.HasValue)
                query = query.Where(f => f.Rating <= filter.MaxRating.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(f => f.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(f => f.CreatedAt <= filter.ToDate.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(f => MapToResponse(f))
                .ToListAsync();

            var result = new PagedResult<FeedbackResponse>
            {
                Items = items,
                TotalCount = total,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<FeedbackResponse>>.SuccessResponse(result, "Feedbacks retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedbacks");
            return ApiResponse<PagedResult<FeedbackResponse>>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<FeedbackResponse>> GetByIdAsync(string id)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            var feedback = await _context.Feedbacks
                .Include(f => f.Patient)
                .Include(f => f.Doctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(f => f.Id == id && f.ClinicId == clinicId && !f.IsDeleted);

            if (feedback == null)
                return ApiResponse<FeedbackResponse>.FailResponse("Feedback not found");

            return ApiResponse<FeedbackResponse>.SuccessResponse(MapToResponse(feedback), "Feedback retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback {Id}", id);
            return ApiResponse<FeedbackResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<FeedbackAnalyticsResponse>> GetAnalyticsAsync(string clinicId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var query = _context.Feedbacks
                .Where(f => f.ClinicId == clinicId && !f.IsDeleted);

            if (from.HasValue) query = query.Where(f => f.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(f => f.CreatedAt <= to.Value);

            var feedbacks = await query.ToListAsync();

            if (!feedbacks.Any())
            {
                return ApiResponse<FeedbackAnalyticsResponse>.SuccessResponse(new FeedbackAnalyticsResponse(), "No feedback data");
            }

            var ratingDistribution = feedbacks
                .GroupBy(f => f.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            var typeDistribution = feedbacks
                .GroupBy(f => f.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            var sentimentDistribution = feedbacks
                .GroupBy(f => f.Sentiment)
                .ToDictionary(g => g.Key, g => g.Count());

            var analytics = new FeedbackAnalyticsResponse
            {
                AverageRating = feedbacks.Average(f => f.Rating),
                TotalFeedbacks = feedbacks.Count,
                RatingDistribution = ratingDistribution,
                TypeDistribution = typeDistribution,
                SentimentDistribution = sentimentDistribution,
                TopPositiveKeywords = ExtractKeywords(feedbacks.Where(f => f.Sentiment == FeedbackSentiment.Positive).Select(f => f.Comment ?? "")),
                TopNegativeKeywords = ExtractKeywords(feedbacks.Where(f => f.Sentiment == FeedbackSentiment.Negative).Select(f => f.Comment ?? "")),
                NPSStats = await GetNPSStatsInternal(clinicId, from, to)
            };

            return ApiResponse<FeedbackAnalyticsResponse>.SuccessResponse(analytics, "Analytics retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback analytics");
            return ApiResponse<FeedbackAnalyticsResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<NPSStatsResponse>> GetNPSStatsAsync(string clinicId, DateTime? from = null, DateTime? to = null)
    {
        var stats = await GetNPSStatsInternal(clinicId, from, to);
        return ApiResponse<NPSStatsResponse>.SuccessResponse(stats, "NPS stats retrieved");
    }

    public async Task<ApiResponse> DeleteAsync(string id)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            var feedback = await _unitOfWork.Feedbacks.GetByIdAsync(id);

            if (feedback == null || feedback.IsDeleted || feedback.ClinicId != clinicId)
                return ApiResponse.FailResponse("Feedback not found");

            feedback.IsDeleted = true;
            feedback.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Feedbacks.Update(feedback);
            await _unitOfWork.CommitAsync();

            return ApiResponse.SuccessResponse("Feedback deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feedback {Id}", id);
            return ApiResponse.FailResponse($"Error: {ex.Message}");
        }
    }

    private FeedbackResponse MapToResponse(Feedback f) => new()
    {
        Id = f.Id,
        PatientId = f.PatientId,
        PatientName = f.Patient != null ? $"{f.Patient.FirstName} {f.Patient.LastName}" : "",
        DoctorId = f.DoctorId,
        DoctorName = f.Doctor?.User != null ? $"{f.Doctor.User.FirstName} {f.Doctor.User.LastName}" : "",
        AppointmentId = f.AppointmentId,
        Rating = f.Rating,
        Comment = f.Comment,
        Type = f.Type,
        NPSScore = f.NPSScore,
        NPSComment = f.NPSComment,
        VisitDate = f.VisitDate,
        IsAnonymous = f.IsAnonymous,
        Tags = f.Tags,
        Sentiment = f.Sentiment,
        CreatedAt = f.CreatedAt
    };

    private FeedbackSentiment AnalyzeSentiment(int rating, string? comment, int? npsScore)
    {
        if (rating >= 4 || (npsScore.HasValue && npsScore >= 9))
            return FeedbackSentiment.Positive;
        if (rating <= 2 || (npsScore.HasValue && npsScore <= 6))
            return FeedbackSentiment.Negative;
        return FeedbackSentiment.Neutral;
    }

    private List<string> ExtractKeywords(IEnumerable<string> comments)
    {
        var words = comments
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .SelectMany(c => c.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(w => w.Length > 3)
            .GroupBy(w => w.ToLower())
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        return words;
    }

    private async Task<NPSStatsResponse> GetNPSStatsInternal(string clinicId, DateTime? from, DateTime? to)
    {
        var query = _context.Feedbacks
            .Where(f => f.ClinicId == clinicId && !f.IsDeleted && f.NPSScore.HasValue);

        if (from.HasValue) query = query.Where(f => f.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(f => f.CreatedAt <= to.Value);

        var scores = await query.Select(f => f.NPSScore!.Value).ToListAsync();

        if (!scores.Any())
        {
            return new NPSStatsResponse();
        }

        var promoters = scores.Count(s => s >= 9);
        var passives = scores.Count(s => s >= 7 && s <= 8);
        var detractors = scores.Count(s => s <= 6);
        var total = scores.Count;

        var monthlyTrend = await _context.Feedbacks
            .Where(f => f.ClinicId == clinicId && !f.IsDeleted && f.NPSScore.HasValue)
            .GroupBy(f => new { f.CreatedAt.Year, f.CreatedAt.Month })
            .Select(g => new MonthlyNPSData
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                AverageNPS = g.Average(f => f.NPSScore!.Value),
                Responses = g.Count()
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync();

        return new NPSStatsResponse
        {
            AverageNPS = scores.Average(),
            PromotersCount = promoters,
            PassivesCount = passives,
            DetractorsCount = detractors,
            TotalResponses = total,
            NPSPercentage = ((double)promoters - detractors) / total * 100,
            MonthlyTrend = monthlyTrend
        };
    }

    private async Task CreateNotification(string userId, string clinicId, string title, string message, NotificationType type)
    {
        if (string.IsNullOrEmpty(userId)) return;

        var notification = new Notification
        {
            UserId = userId,
            ClinicId = clinicId,
            Title = title,
            Message = message,
            Type = type,
            Channel = NotificationChannel.InApp,
            IsRead = false
        };
        notification.CreatedBy = "system";
        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.CommitAsync();
    }
}