using System.Collections.Generic;
using System.Threading.Tasks;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Application.Services;

public interface IReminderService
{
    Task<ApiResponse> SendAppointmentRemindersAsync();
    Task<ApiResponse> SendMissedAppointmentNotificationsAsync();
    Task<ApiResponse> SendMonthlyReportNotificationsAsync();
    Task<ApiResponse> SendPaymentDueRemindersAsync();
    Task<ApiResponse> SendBirthdayGreetingsAsync();
    Task<ApiResponse> SendHolidayGreetingsAsync();
    Task<ApiResponse> SendTreatmentRemindersAsync();
    Task<ApiResponse> ProcessAllRemindersAsync();
}