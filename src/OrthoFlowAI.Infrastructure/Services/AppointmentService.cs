using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public AppointmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Appointment>> GetByIdAsync(string id)
    {
        var appointments = await _unitOfWork.Appointments.FindAsync(
            a => a.Id == id,
            a => a.Patient!,
            a => a.Doctor!,
            a => a.Branch!);

        var appointment = appointments.FirstOrDefault();
        if (appointment is null)
        {
            return ApiResponse<Appointment>.FailResponse("Appointment not found.");
        }

        return ApiResponse<Appointment>.SuccessResponse(appointment);
    }

    public async Task<ApiResponse<PagedResult<Appointment>>> GetAllAsync(
        int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null,
        string? doctorId = null, string? patientId = null, AppointmentStatus? status = null)
    {
        Expression<Func<Appointment, bool>>? filter = BuildFilter(startDate, endDate, doctorId, patientId, status);

        var result = await _unitOfWork.Appointments.GetPagedAsync(
            pageNumber,
            pageSize,
            filter,
            q => q.OrderByDescending(a => a.AppointmentDate));

        return ApiResponse<PagedResult<Appointment>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<List<Appointment>>> GetCalendarEventsAsync(
        DateTime startDate, DateTime endDate, string? doctorId = null)
    {
        Expression<Func<Appointment, bool>> filter = a =>
            a.AppointmentDate >= startDate && a.AppointmentDate <= endDate;

        if (!string.IsNullOrWhiteSpace(doctorId))
        {
            filter = a => a.AppointmentDate >= startDate &&
                          a.AppointmentDate <= endDate &&
                          a.DoctorId == doctorId;
        }

        var appointments = await _unitOfWork.Appointments.FindAsync(filter);
        var sorted = appointments.OrderBy(a => a.AppointmentDate).ToList();

        return ApiResponse<List<Appointment>>.SuccessResponse(sorted);
    }

    public async Task<ApiResponse<Appointment>> CreateAsync(Appointment appointment)
    {
        var isAvailable = await CheckAvailabilityAsync(
            appointment.DoctorId, appointment.AppointmentDate, appointment.Duration);

        if (!isAvailable.Data)
        {
            return ApiResponse<Appointment>.FailResponse("The selected time slot is not available.");
        }

        appointment.CreatedAt = DateTime.UtcNow;
        appointment.Status = AppointmentStatus.Scheduled;

        if (appointment.EndTime is null)
        {
            appointment.EndTime = appointment.AppointmentDate.AddMinutes(appointment.Duration);
        }

        await _unitOfWork.Appointments.AddAsync(appointment);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Appointment>.SuccessResponse(appointment, "Appointment created successfully.");
    }

    public async Task<ApiResponse<Appointment>> UpdateAsync(Appointment appointment)
    {
        var existing = await _unitOfWork.Appointments.GetByIdAsync(appointment.Id);
        if (existing is null)
        {
            return ApiResponse<Appointment>.FailResponse("Appointment not found.");
        }

        if (existing.Status == AppointmentStatus.Completed || existing.Status == AppointmentStatus.Cancelled)
        {
            return ApiResponse<Appointment>.FailResponse("Cannot modify a completed or cancelled appointment.");
        }

        existing.PatientId = appointment.PatientId;
        existing.DoctorId = appointment.DoctorId;
        existing.BranchId = appointment.BranchId;
        existing.AppointmentDate = appointment.AppointmentDate;
        existing.EndTime = appointment.EndTime;
        existing.Type = appointment.Type;
        existing.Notes = appointment.Notes;
        existing.Duration = appointment.Duration;
        existing.Color = appointment.Color;
        existing.IsRecurring = appointment.IsRecurring;
        existing.RecurrencePattern = appointment.RecurrencePattern;
        existing.RecurrenceEndDate = appointment.RecurrenceEndDate;

        _unitOfWork.Appointments.Update(existing);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Appointment>.SuccessResponse(existing, "Appointment updated successfully.");
    }

    public async Task<ApiResponse> DeleteAsync(string id)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment is null)
        {
            return ApiResponse.FailResponse("Appointment not found.");
        }

        appointment.IsDeleted = true;
        _unitOfWork.Appointments.Update(appointment);
        await _unitOfWork.CommitAsync();

        return ApiResponse.SuccessResponse("Appointment deleted successfully.");
    }

    public async Task<ApiResponse<Appointment>> UpdateStatusAsync(string id, AppointmentStatus status)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment is null)
        {
            return ApiResponse<Appointment>.FailResponse("Appointment not found.");
        }

        appointment.Status = status;
        _unitOfWork.Appointments.Update(appointment);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Appointment>.SuccessResponse(appointment, $"Appointment status updated to {status}.");
    }

    public async Task<ApiResponse<Appointment>> RescheduleAsync(string id, DateTime newDate, DateTime? newEndTime)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment is null)
        {
            return ApiResponse<Appointment>.FailResponse("Appointment not found.");
        }

        if (appointment.Status == AppointmentStatus.Completed || appointment.Status == AppointmentStatus.Cancelled)
        {
            return ApiResponse<Appointment>.FailResponse("Cannot reschedule a completed or cancelled appointment.");
        }

        var isAvailable = await CheckAvailabilityAsync(appointment.DoctorId, newDate, appointment.Duration);
        if (!isAvailable.Data)
        {
            return ApiResponse<Appointment>.FailResponse("The new time slot is not available.");
        }

        appointment.AppointmentDate = newDate;
        appointment.EndTime = newEndTime ?? newDate.AddMinutes(appointment.Duration);
        appointment.Status = AppointmentStatus.Rescheduled;

        _unitOfWork.Appointments.Update(appointment);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Appointment>.SuccessResponse(appointment, "Appointment rescheduled successfully.");
    }

    public async Task<ApiResponse<int>> GetDailyCountAsync(string doctorId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var appointments = await _unitOfWork.Appointments.FindAsync(a =>
            a.DoctorId == doctorId &&
            a.AppointmentDate >= startOfDay &&
            a.AppointmentDate < endOfDay &&
            a.Status != AppointmentStatus.Cancelled);

        return ApiResponse<int>.SuccessResponse(appointments.Count);
    }

    public async Task<ApiResponse<bool>> CheckAvailabilityAsync(string doctorId, DateTime date, int duration)
    {
        var endTime = date.AddMinutes(duration);
        var conflicting = await _unitOfWork.Appointments.FindAsync(a =>
            a.DoctorId == doctorId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.AppointmentDate < endTime &&
            (a.EndTime ?? a.AppointmentDate.AddMinutes(a.Duration)) > date);

        return ApiResponse<bool>.SuccessResponse(conflicting.Count == 0);
    }

    private static Expression<Func<Appointment, bool>>? BuildFilter(
        DateTime? startDate, DateTime? endDate, string? doctorId, string? patientId, AppointmentStatus? status)
    {
        Expression<Func<Appointment, bool>>? filter = null;

        if (startDate.HasValue)
        {
            filter = CombineFilters(filter, a => a.AppointmentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            filter = CombineFilters(filter, a => a.AppointmentDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(doctorId))
        {
            filter = CombineFilters(filter, a => a.DoctorId == doctorId);
        }

        if (!string.IsNullOrWhiteSpace(patientId))
        {
            filter = CombineFilters(filter, a => a.PatientId == patientId);
        }

        if (status.HasValue)
        {
            filter = CombineFilters(filter, a => a.Status == status.Value);
        }

        return filter;
    }

    private static Expression<Func<Appointment, bool>>? CombineFilters(
        Expression<Func<Appointment, bool>>? first, Expression<Func<Appointment, bool>> second)
    {
        if (first is null)
        {
            return second;
        }

        var parameter = Expression.Parameter(typeof(Appointment), "a");
        var body = Expression.AndAlso(
            Expression.Invoke(first, parameter),
            Expression.Invoke(second, parameter));

        return Expression.Lambda<Func<Appointment, bool>>(body, parameter);
    }
}
