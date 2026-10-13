using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<DashboardStats>> GetStatsAsync(
        string clinicId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = now.AddDays(-(int)now.DayOfWeek);
        var stats = new DashboardStats();

        var allPatients = await _unitOfWork.Patients.FindAsync(p => p.ClinicId == clinicId);
        stats.TotalPatients = allPatients.Count;
        stats.ActivePatients = allPatients.Count(p => p.Status == PatientStatus.Active);
        stats.NewPatientsThisMonth = allPatients.Count(p => p.CreatedAt >= monthStart);

        var allDoctors = await _unitOfWork.Doctors.FindAsync(d => d.ClinicId == clinicId);
        stats.TotalDoctors = allDoctors.Count;

        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);
        var todayAppointments = await _unitOfWork.Appointments.FindAsync(a =>
            a.ClinicId == clinicId &&
            a.AppointmentDate >= todayStart &&
            a.AppointmentDate < todayEnd &&
            a.Status != AppointmentStatus.Cancelled);
        stats.TodayAppointments = todayAppointments.Count;

        var weekAppointments = await _unitOfWork.Appointments.FindAsync(a =>
            a.ClinicId == clinicId &&
            a.AppointmentDate >= weekStart &&
            a.AppointmentDate < weekStart.AddDays(7) &&
            a.Status != AppointmentStatus.Cancelled);
        stats.WeekAppointments = weekAppointments.Count;

        var monthAppointments = await _unitOfWork.Appointments.FindAsync(a =>
            a.ClinicId == clinicId &&
            a.AppointmentDate >= monthStart &&
            a.Status != AppointmentStatus.Cancelled);
        stats.MonthAppointments = monthAppointments.Count;

        var payments = await _unitOfWork.Payments.FindAsync(p => p.ClinicId == clinicId);
        var start = startDate ?? new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = endDate ?? now;

        stats.TotalRevenue = payments.Sum(p => p.Amount);
        stats.MonthRevenue = payments.Where(p => p.PaymentDate >= monthStart).Sum(p => p.Amount);

        var invoices = await _unitOfWork.Invoices.FindAsync(i => i.ClinicId == clinicId);
        stats.TotalInvoices = invoices.Count;
        stats.OverdueInvoices = invoices.Count(i =>
            i.Status == InvoiceStatus.Overdue ||
            (i.DueDate < now && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled));
        stats.PendingPayments = invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .Sum(i => i.RemainingAmount);

        var unreadNotifications = await _unitOfWork.Notifications.FindAsync(n =>
            n.ClinicId == clinicId && !n.IsRead);
        stats.PendingNotifications = unreadNotifications.Count;

        var activeEmergencies = await _unitOfWork.EmergencyRequests.FindAsync(e =>
            e.ClinicId == clinicId && e.Status != "Resolved");
        stats.ActiveEmergencies = activeEmergencies.Count;

        return ApiResponse<DashboardStats>.SuccessResponse(stats);
    }

    public async Task<ApiResponse<RevenueChart>> GetRevenueChartAsync(string clinicId, int months = 12)
    {
        var now = DateTime.UtcNow;
        var chart = new RevenueChart();

        for (var i = months - 1; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            var payments = await _unitOfWork.Payments.FindAsync(p =>
                p.ClinicId == clinicId &&
                p.PaymentDate >= monthStart &&
                p.PaymentDate < monthEnd);

            chart.DataPoints.Add(new ChartPoint
            {
                Label = monthStart.ToString("MMM yyyy"),
                Value = payments.Sum(p => p.Amount)
            });
        }

        return ApiResponse<RevenueChart>.SuccessResponse(chart);
    }

    public async Task<ApiResponse<AppointmentChart>> GetAppointmentChartAsync(string clinicId, int months = 12)
    {
        var now = DateTime.UtcNow;
        var chart = new AppointmentChart();

        for (var i = months - 1; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            var appointments = await _unitOfWork.Appointments.FindAsync(a =>
                a.ClinicId == clinicId &&
                a.AppointmentDate >= monthStart &&
                a.AppointmentDate < monthEnd &&
                a.Status != AppointmentStatus.Cancelled);

            chart.DataPoints.Add(new ChartPoint
            {
                Label = monthStart.ToString("MMM yyyy"),
                Value = appointments.Count
            });
        }

        return ApiResponse<AppointmentChart>.SuccessResponse(chart);
    }

    public async Task<ApiResponse<List<TopDoctor>>> GetTopDoctorsAsync(string clinicId, int count = 5)
    {
        var doctors = await _unitOfWork.Doctors.FindAsync(d => d.ClinicId == clinicId);
        var appointments = await _unitOfWork.Appointments.FindAsync(a => a.ClinicId == clinicId);
        var payments = await _unitOfWork.Payments.FindAsync(p => p.ClinicId == clinicId);

        var topDoctors = doctors
            .Select(d => new TopDoctor
            {
                DoctorId = d.Id,
                DoctorName = $"{d.User?.FirstName} {d.User?.LastName}",
                AppointmentCount = appointments.Count(a => a.DoctorId == d.Id && a.Status == AppointmentStatus.Completed),
                Revenue = payments
                    .Where(p => p.Invoice?.DoctorId == d.Id)
                    .Sum(p => p.Amount)
            })
            .OrderByDescending(d => d.AppointmentCount)
            .Take(count)
            .ToList();

        return ApiResponse<List<TopDoctor>>.SuccessResponse(topDoctors);
    }

    public async Task<ApiResponse<List<RecentActivity>>> GetRecentActivityAsync(string clinicId, int count = 20)
    {
        var logs = await _unitOfWork.AuditLogs.Query()
            .Where(a => a.ClinicId == clinicId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .Select(a => new RecentActivity
            {
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                UserName = a.UserId,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<RecentActivity>>.SuccessResponse(logs);
    }
}
