using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Infrastructure.Services;

public interface IAppointmentService
{
    Task<ApiResponse<Appointment>> GetByIdAsync(string id);
    Task<ApiResponse<PagedResult<Appointment>>> GetAllAsync(
        int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null,
        string? doctorId = null, string? patientId = null, AppointmentStatus? status = null);
    Task<ApiResponse<List<Appointment>>> GetCalendarEventsAsync(
        DateTime startDate, DateTime endDate, string? doctorId = null);
    Task<ApiResponse<Appointment>> CreateAsync(Appointment appointment);
    Task<ApiResponse<Appointment>> UpdateAsync(Appointment appointment);
    Task<ApiResponse> DeleteAsync(string id);
    Task<ApiResponse<Appointment>> UpdateStatusAsync(string id, AppointmentStatus status);
    Task<ApiResponse<Appointment>> RescheduleAsync(string id, DateTime newDate, DateTime? newEndTime);
    Task<ApiResponse<int>> GetDailyCountAsync(string doctorId, DateTime date);
    Task<ApiResponse<bool>> CheckAvailabilityAsync(string doctorId, DateTime date, int duration);
}
