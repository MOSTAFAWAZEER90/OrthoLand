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

public class ReferralService : IReferralService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReferralService> _logger;

    public ReferralService(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<ReferralService> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<ReferralResponse>> CreateReferralAsync(CreateReferralRequest request, string clinicId)
    {
        try
        {
            var referrer = await _unitOfWork.Patients.GetByIdAsync(request.ReferrerPatientId);
            if (referrer == null || referrer.IsDeleted || referrer.ClinicId != clinicId)
                return ApiResponse<ReferralResponse>.FailResponse("Referrer patient not found");

            // Check if referred patient exists or create new
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Email == request.ReferredEmail && p.ClinicId == clinicId && !p.IsDeleted);

            string referredPatientId;
            if (existingPatient != null)
            {
                referredPatientId = existingPatient.Id;
            }
            else
            {
                // Create new patient
                var newPatient = new Patient
                {
                    FirstName = request.ReferredFirstName,
                    LastName = request.ReferredLastName,
                    Email = request.ReferredEmail,
                    Phone = request.ReferredPhone,
                    Status = PatientStatus.Active,
                    ClinicId = clinicId
                };
                newPatient.CreatedBy = "referral-system";
                await _unitOfWork.Patients.AddAsync(newPatient);
                await _unitOfWork.CommitAsync();
                referredPatientId = newPatient.Id;
            }

            // Generate unique referral code
            var referralCode = GenerateReferralCode();

            var referral = new Referral
            {
                ReferrerPatientId = request.ReferrerPatientId,
                ReferredPatientId = referredPatientId,
                ReferralCode = referralCode,
                ReferrerDiscount = request.ReferrerDiscount,
                ReferredDiscount = request.ReferredDiscount,
                ReferrerReward = request.ReferrerReward,
                Status = ReferralStatus.Pending,
                Notes = request.Notes,
                ClinicId = clinicId
            };
            referral.CreatedBy = "referral-system";

            await _unitOfWork.Referrals.AddAsync(referral);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Referral created: {Code} by referrer {ReferrerId}", referralCode, request.ReferrerPatientId);

            // Notify referrer
            await CreateNotification(referrer.UserId ?? string.Empty, clinicId, 
                "New Referral Created", 
                $"Your referral code {referralCode} has been sent. You'll earn a reward when they book!",
                NotificationType.General);

            return ApiResponse<ReferralResponse>.SuccessResponse(MapToResponse(referral), "Referral created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating referral");
            return ApiResponse<ReferralResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ReferralResponse>> GetByCodeAsync(string code)
    {
        try
        {
            var referral = await _context.Referrals
                .Include(r => r.ReferrerPatient)
                .Include(r => r.ReferredPatient)
                .FirstOrDefaultAsync(r => r.ReferralCode == code && !r.IsDeleted);

            if (referral == null)
                return ApiResponse<ReferralResponse>.FailResponse("Invalid referral code");

            return ApiResponse<ReferralResponse>.SuccessResponse(MapToResponse(referral), "Referral found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting referral by code {Code}", code);
            return ApiResponse<ReferralResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ReferralResponse>> CompleteReferralAsync(string code, string referredPatientId)
    {
        try
        {
            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.ReferralCode == code && !r.IsDeleted);

            if (referral == null)
                return ApiResponse<ReferralResponse>.FailResponse("Invalid referral code");

            if (referral.Status != ReferralStatus.Pending)
                return ApiResponse<ReferralResponse>.FailResponse($"Referral already {referral.Status.ToString().ToLower()}");

            referral.Status = ReferralStatus.Completed;
            referral.CompletedAt = DateTime.UtcNow;
            referral.ReferredPatientId = referredPatientId;
            referral.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Referrals.Update(referral);
            await _unitOfWork.CommitAsync();

            // Notify referrer
            var referrer = await _unitOfWork.Patients.GetByIdAsync(referral.ReferrerPatientId);
            if (referrer != null && !string.IsNullOrEmpty(referrer.UserId))
            {
                await CreateNotification(referrer.UserId, referral.ClinicId,
                    "Referral Completed!",
                    $"Your referral has been completed. You've earned a reward of ${referral.ReferrerReward}!",
                    NotificationType.General);
            }

            _logger.LogInformation("Referral completed: {Code}", code);
            return ApiResponse<ReferralResponse>.SuccessResponse(MapToResponse(referral), "Referral completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing referral {Code}", code);
            return ApiResponse<ReferralResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PagedResult<ReferralResponse>>> GetReferralsAsync(string clinicId, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Referrals
                .Include(r => r.ReferrerPatient)
                .Include(r => r.ReferredPatient)
                .Where(r => r.ClinicId == clinicId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => MapToResponse(r))
                .ToListAsync();

            var result = new PagedResult<ReferralResponse>
            {
                Items = items,
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResult<ReferralResponse>>.SuccessResponse(result, "Referrals retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting referrals for clinic {ClinicId}", clinicId);
            return ApiResponse<PagedResult<ReferralResponse>>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ReferralStatsResponse>> GetReferrerStatsAsync(string referrerPatientId)
    {
        try
        {
            var referrals = await _context.Referrals
                .Where(r => r.ReferrerPatientId == referrerPatientId && !r.IsDeleted)
                .ToListAsync();

            var stats = new ReferralStatsResponse
            {
                TotalReferrals = referrals.Count,
                CompletedReferrals = referrals.Count(r => r.Status == ReferralStatus.Completed),
                PendingReferrals = referrals.Count(r => r.Status == ReferralStatus.Pending),
                TotalRewardsEarned = referrals.Where(r => r.Status == ReferralStatus.Completed).Sum(r => r.ReferrerReward),
                TotalDiscountsGiven = referrals.Sum(r => r.ReferrerDiscount + r.ReferredDiscount),
                RecentReferrals = referrals.OrderByDescending(r => r.CreatedAt).Take(10).Select(MapToResponse).ToList()
            };

            return ApiResponse<ReferralStatsResponse>.SuccessResponse(stats, "Referrer stats retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting referrer stats for {PatientId}", referrerPatientId);
            return ApiResponse<ReferralStatsResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ReferralProgramSettingsResponse>> GetProgramSettingsAsync(string clinicId)
    {
        // In production, fetch from clinic settings or database
        return ApiResponse<ReferralProgramSettingsResponse>.SuccessResponse(new ReferralProgramSettingsResponse
        {
            DefaultReferrerDiscount = 100,
            DefaultReferredDiscount = 100,
            DefaultReferrerReward = 50,
            CodeExpiryDays = 30,
            IsActive = true
        }, "Settings retrieved");
    }

    public async Task<ApiResponse> UpdateProgramSettingsAsync(string clinicId, ReferralProgramSettingsResponse settings)
    {
        // In production, save to clinic settings
        return ApiResponse.SuccessResponse("Settings updated");
    }

    private string GenerateReferralCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private ReferralResponse MapToResponse(Referral r) => new()
    {
        Id = r.Id,
        ReferrerPatientId = r.ReferrerPatientId,
        ReferrerName = r.ReferrerPatient != null ? $"{r.ReferrerPatient.FirstName} {r.ReferrerPatient.LastName}" : "",
        ReferredPatientId = r.ReferredPatientId,
        ReferredName = r.ReferredPatient != null ? $"{r.ReferredPatient.FirstName} {r.ReferredPatient.LastName}" : "",
        ReferralCode = r.ReferralCode,
        ReferrerDiscount = r.ReferrerDiscount,
        ReferredDiscount = r.ReferredDiscount,
        ReferrerReward = r.ReferrerReward,
        Status = r.Status,
        ReferredAt = r.ReferredAt,
        CompletedAt = r.CompletedAt,
        Notes = r.Notes
    };

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