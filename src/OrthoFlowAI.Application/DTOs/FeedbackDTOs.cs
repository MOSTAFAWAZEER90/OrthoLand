using System.ComponentModel.DataAnnotations;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.DTOs;

public class CreateFeedbackRequest
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    public string? DoctorId { get; set; }
    public string? AppointmentId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    public string? Comment { get; set; }

    [Required]
    public FeedbackType Type { get; set; }

    [Range(0, 10)]
    public int? NPSScore { get; set; }

    public string? NPSComment { get; set; }
    public DateTime? VisitDate { get; set; }
    public bool IsAnonymous { get; set; }
    public string? Tags { get; set; }
}

public class FeedbackResponse
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string? AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public FeedbackType Type { get; set; }
    public int? NPSScore { get; set; }
    public string? NPSComment { get; set; }
    public DateTime? VisitDate { get; set; }
    public bool IsAnonymous { get; set; }
    public string? Tags { get; set; }
    public FeedbackSentiment Sentiment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FeedbackFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? PatientId { get; set; }
    public string? DoctorId { get; set; }
    public string? ClinicId { get; set; }
    public FeedbackType? Type { get; set; }
    public FeedbackSentiment? Sentiment { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class NPSStatsResponse
{
    public double AverageNPS { get; set; }
    public int PromotersCount { get; set; } // 9-10
    public int PassivesCount { get; set; } // 7-8
    public int DetractorsCount { get; set; } // 0-6
    public int TotalResponses { get; set; }
    public double NPSPercentage { get; set; }
    public List<MonthlyNPSData> MonthlyTrend { get; set; } = new();
}

public class MonthlyNPSData
{
    public int Year { get; set; }
    public int Month { get; set; }
    public double AverageNPS { get; set; }
    public int Responses { get; set; }
}

public class FeedbackAnalyticsResponse
{
    public double AverageRating { get; set; }
    public int TotalFeedbacks { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
    public Dictionary<FeedbackType, int> TypeDistribution { get; set; } = new();
    public Dictionary<FeedbackSentiment, int> SentimentDistribution { get; set; } = new();
    public List<string> TopPositiveKeywords { get; set; } = new();
    public List<string> TopNegativeKeywords { get; set; } = new();
    public NPSStatsResponse NPSStats { get; set; } = new();
}