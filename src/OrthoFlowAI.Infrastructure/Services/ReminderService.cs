using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;
using OrthoFlowAI.Infrastructure.Data;
using OrthoFlowAI.Infrastructure.Settings;

namespace OrthoFlowAI.Infrastructure.Services;

public class ReminderService : IReminderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationChannelFactory _channelFactory;
    private readonly ILogger<ReminderService> _logger;
    private readonly ApplicationDbContext _context;

    public ReminderService(
        IUnitOfWork unitOfWork,
        INotificationChannelFactory channelFactory,
        ILogger<ReminderService> logger,
        ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _channelFactory = channelFactory;
        _logger = logger;
        _context = context;
    }

    public async Task<ApiResponse> SendAppointmentRemindersAsync()
    {
        try
        {
            _logger.LogInformation("Starting appointment reminders job");
            
            var now = DateTime.UtcNow;
            var tomorrow = now.Date.AddDays(1);
            var in3Days = now.Date.AddDays(3);
            var in7Days = now.Date.AddDays(7);
            var in2Hours = now.AddHours(2);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Where(a => !a.IsDeleted 
                    && a.Status != AppointmentStatus.Cancelled
                    && a.Status != AppointmentStatus.Completed
                    && a.Status != AppointmentStatus.NoShow)
                .ToListAsync();

            var sentCount = 0;
            var failedCount = 0;

            foreach (var appointment in appointments)
            {
                var timeUntil = appointment.AppointmentDate - now;
                var shouldRemind = false;
                var reminderType = "";

                if (timeUntil.TotalDays <= 7 && timeUntil.TotalDays > 6.5 && !appointment.Reminder7DaysSent)
                {
                    shouldRemind = true;
                    reminderType = "7_days";
                }
                else if (timeUntil.TotalDays <= 3 && timeUntil.TotalDays > 2.5 && !appointment.Reminder3DaysSent)
                {
                    shouldRemind = true;
                    reminderType = "3_days";
                }
                else if (timeUntil.TotalDays <= 1 && timeUntil.TotalDays > 0.5 && !appointment.Reminder1DaySent)
                {
                    shouldRemind = true;
                    reminderType = "1_day";
                }
                else if (timeUntil.TotalHours <= 2 && timeUntil.TotalHours > 1.5 && !appointment.Reminder2HoursSent)
                {
                    shouldRemind = true;
                    reminderType = "2_hours";
                }

                if (shouldRemind && appointment.Patient != null)
                {
                    var patient = appointment.Patient;
                    var doctorName = appointment.Doctor?.User != null 
                        ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" 
                        : "your doctor";

                    var message = $"Reminder: You have an appointment with {doctorName} on {appointment.AppointmentDate:MMMM dd, yyyy at hh:mm tt}. Please arrive 15 minutes early.";
                    var subject = "Appointment Reminder";

                    var result = await SendMultiChannelNotificationAsync(patient, subject, message, appointment);
                    
                    if (result.Success)
                    {
                        sentCount++;
                        // Mark reminder as sent
                        switch (reminderType)
                        {
                            case "7_days": appointment.Reminder7DaysSent = true; break;
                            case "3_days": appointment.Reminder3DaysSent = true; break;
                            case "1_day": appointment.Reminder1DaySent = true; break;
                            case "2_hours": appointment.Reminder2HoursSent = true; break;
                        }
                        appointment.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Appointments.Update(appointment);
                    }
                    else
                    {
                        failedCount++;
                    }
                }
            }

            await _unitOfWork.CommitAsync();
            
            _logger.LogInformation("Appointment reminders completed. Sent: {Sent}, Failed: {Failed}", sentCount, failedCount);
            return ApiResponse.SuccessResponse($"Appointment reminders sent: {sentCount}, Failed: {failedCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in appointment reminders job");
            return ApiResponse.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse> SendMissedAppointmentNotificationsAsync()
    {
        try
        {
            _logger.LogInformation("Starting missed appointment notifications job");
            
            var now = DateTime.UtcNow;
            var yesterday = now.Date.AddDays(-1);

            var missedAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Where(a => !a.IsDeleted 
                    && a.AppointmentDate < now
                    && a.AppointmentDate >= yesterday
                    && a.Status == AppointmentStatus.Scheduled)
                .ToListAsync();

            var sentCount = 0;
            var failedCount = 0;

            foreach (var appointment in missedAppointments)
            {
                appointment.Status = AppointmentStatus.NoShow;
                appointment.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Appointments.Update(appointment);

                if (appointment.Patient != null)
                {
                    var doctorName = appointment.Doctor?.User != null 
                        ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" 
                        : "your doctor";

                    var message = $"You missed your appointment with {doctorName} scheduled for {appointment.AppointmentDate:MMMM dd, yyyy at hh:mm tt}. Please contact us to reschedule.";
                    var subject = "Missed Appointment";

                    var result = await SendMultiChannelNotificationAsync(appointment.Patient, subject, message, appointment);
                    
                    if (result.Success)
                        sentCount++;
                    else
                        failedCount++;
                }
            }

            await _unitOfWork.CommitAsync();
            
            _logger.LogInformation("Missed appointment notifications completed. Sent: {Sent}, Failed: {Failed}", sentCount, failedCount);
            return ApiResponse.SuccessResponse($"Missed appointment notifications sent: {sentCount}, Failed: {failedCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in missed appointment notifications job");
            return ApiResponse.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse> SendMonthlyReportNotificationsAsync()
    {
        try
        {
            _logger.LogInformation("Starting monthly report notifications job");
            
            var patients = await _context.Patients
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .Where(p => !p.IsDeleted && p.Status == PatientStatus.Active)
                .ToListAsync();

            var sentCount = 0;
            var failedCount = 0;

            foreach (var patient in patients)
            {
                // Check if monthly report is due (simplified: first day of month)
                if (DateTime.UtcNow.Day == 1)
                {
                    var message = $"Your monthly treatment progress report is ready. Please check your portal for details.";
                    var subject = "Monthly Treatment Report";

                    var result = await SendMultiChannelNotificationAsync(patient, subject, message, null);
                    
                    if (result.Success)
                        sentCount++;
                    else
                        failedCount++;
                }
            }

            _logger.LogInformation("Monthly report notifications completed. Sent: {Sent}, Failed: {Failed}", sentCount, failedCount);
            return ApiResponse.SuccessResponse($"Monthly reports sent: {sentCount}, Failed: {failedCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in monthly report notifications job");
            return ApiResponse.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse> SendPaymentDueRemindersAsync()
    {
        try
        {
            _logger.LogInformation("Starting payment due reminders job");
            
            var overdueInvoices = await _context.Invoices
                .Include(i => i.Patient)
                .Where(i => !i.IsDeleted 
                    && (i.PaymentStatus == PaymentStatus.Pending 
                        || i.PaymentStatus == PaymentStatus.Partial 
                        || i.PaymentStatus == PaymentStatus.Overdue)
                    && i.DueDate <= DateTime.UtcNow.AddDays(3))
                .ToListAsync();

            var sentCount = 0;
            var failedCount = 0;

            foreach (var invoice in overdueInvoices)
            {
                if (invoice.Patient != null && invoice.DueDate.HasValue)
                {
                    var daysOverdue = (DateTime.UtcNow - invoice.DueDate.Value).Days;
                    var statusText = daysOverdue > 0 ? $"overdue by {daysOverdue} days" : $"due in {Math.Abs(daysOverdue)} days";
                    
                    var message = $"Payment reminder: Invoice #{invoice.InvoiceNumber} for ${invoice.TotalAmount:N2} is {statusText}. Please make payment to avoid service interruption.";
                    var subject = "Payment Due Reminder";

                    var result = await SendMultiChannelNotificationAsync(invoice.Patient, subject, message, null);
                    
                    if (result.Success)
                        sentCount++;
                    else
                        failedCount++;
                }
            }

            _logger.LogInformation("Payment due reminders completed. Sent: {Sent}, Failed: {Failed}", sentCount, failedCount);
            return ApiResponse.SuccessResponse($"Payment reminders sent: {sentCount}, Failed: {failedCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in payment due reminders job");
            return ApiResponse.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse> SendBirthdayGreetingsAsync()
    {
        try
        {
            _logger.LogInformation("Starting birthday greetings job");
            
            var today = DateTime.UtcNow.Date;
            var patients = await _context.Patients
                .Where(p => !p.IsDeleted 
                    && p.DateOfBirth.HasValue
                    && p.DateOfBirth.Value.Month == today.Month
                    && p.DateOfBirth.Value.Day == today.Day
                    && p.Status == PatientStatus.Active)
                .ToListAsync();

            var sentCount = 0;
            var failedCount = 0;

            foreach (var patient in patients)
            {
                var message = $"Happy Birthday {patient.FirstName}! 🎉 Wishing you a wonderful day and a healthy smile all year long!";
                var subject = "Happy Birthday!";

                var result = await SendMultiChannelNotificationAsync(patient, subject, message, null);
                
                if (result.Success)
                    sentCount++;
                else
                    failedCount++;
            }

            _logger.LogInformation("Birthday greetings completed. Sent: {Sent}, Failed: {Failed}", sentCount, failedCount);
            return ApiResponse.SuccessResponse($"Birthday greetings sent: {sentCount}, Failed: {failedCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in birthday greetings job");
            return ApiResponse.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse> SendHolidayGreetingsAsync()
    {
        // Placeholder for holiday greetings - implement based on clinic's calendar
        return ApiResponse.SuccessResponse("Holiday greetings job completed");
    }

    public async Task<ApiResponse> SendTreatmentRemindersAsync()
    {
        try
        {
            _logger.LogInformation("Starting treatment reminders job");
            
            // Remind patients about rubber bands, cleaning, etc.
            var patients = await _context.Patients
                .Where(p => !p.IsDeleted && p.Status == PatientStatus.Active)
                .ToListAsync();

            var sentCount = 0;
            var failedCount = 0;

            foreach (var patient in patients)
            {
                // Simplified: send weekly treatment reminder
                if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Monday)
                {
                    var message = $"Weekly reminder: Don't forget to wear your rubber bands as prescribed and maintain good oral hygiene! 🦷";
                    var subject = "Weekly Treatment Reminder";

                    var result = await SendMultiChannelNotificationAsync(patient, subject, message, null);
                    
                    if (result.Success)
                        sentCount++;
                    else
                        failedCount++;
                }
            }

            _logger.LogInformation("Treatment reminders completed. Sent: {Sent}, Failed: {Failed}", sentCount, failedCount);
            return ApiResponse.SuccessResponse($"Treatment reminders sent: {sentCount}, Failed: {failedCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in treatment reminders job");
            return ApiResponse.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse> ProcessAllRemindersAsync()
    {
        var results = new List<ApiResponse>
        {
            await SendAppointmentRemindersAsync(),
            await SendMissedAppointmentNotificationsAsync(),
            await SendPaymentDueRemindersAsync(),
            await SendTreatmentRemindersAsync()
        };

        // Only run monthly/birthday on specific days
        if (DateTime.UtcNow.Day == 1)
        {
            results.Add(await SendMonthlyReportNotificationsAsync());
        }

        results.Add(await SendBirthdayGreetingsAsync());

        var totalSent = results.Where(r => r.Success).Count();
        return ApiResponse.SuccessResponse($"All reminders processed. Successful jobs: {totalSent}/{results.Count}");
    }

    private async Task<ApiResponse> SendMultiChannelNotificationAsync(Patient patient, string subject, string message, Appointment? appointment)
    {
        var channels = _channelFactory.GetAvailableChannels().ToList();
        var results = new List<ApiResponse>();

        foreach (var channel in channels)
        {
            try
            {
                string recipient = "";
                var metadata = new Dictionary<string, string>();

                switch (channel.Channel)
                {
                    case NotificationChannel.Email:
                        if (!string.IsNullOrEmpty(patient.Email))
                        {
                            recipient = patient.Email;
                            metadata["channel"] = "email";
                        }
                        break;
                    case NotificationChannel.WhatsApp:
                        if (!string.IsNullOrEmpty(patient.Phone))
                        {
                            recipient = patient.Phone;
                            metadata["channel"] = "whatsapp";
                        }
                        break;
                    case NotificationChannel.SMS:
                        if (!string.IsNullOrEmpty(patient.Phone))
                        {
                            recipient = patient.Phone;
                            metadata["channel"] = "sms";
                        }
                        break;
                    case NotificationChannel.Push:
                        if (!string.IsNullOrEmpty(patient.DeviceToken))
                        {
                            recipient = patient.DeviceToken;
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(recipient))
                {
                    var result = await channel.SendAsync(recipient, subject, message, metadata);
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending via {Channel}", channel.Channel);
                results.Add(ApiResponse.FailResponse($"Channel {channel.Channel} error: {ex.Message}"));
            }
        }

        // Create in-app notification
        var inAppNotification = new Notification
        {
            UserId = patient.UserId,
            ClinicId = patient.ClinicId,
            Title = subject,
            Message = message,
            Type = appointment != null ? NotificationType.AppointmentReminder : NotificationType.General,
            Channel = NotificationChannel.InApp,
            IsRead = false,
            RelatedEntityId = appointment?.Id,
            RelatedEntityType = "Appointment"
        };
        inAppNotification.CreatedBy = "system";
        await _unitOfWork.Notifications.AddAsync(inAppNotification);
        await _unitOfWork.CommitAsync();

        return results.Any(r => r.Success) 
            ? ApiResponse.SuccessResponse("Notification sent via at least one channel")
            : ApiResponse.FailResponse("All channels failed");
    }
}