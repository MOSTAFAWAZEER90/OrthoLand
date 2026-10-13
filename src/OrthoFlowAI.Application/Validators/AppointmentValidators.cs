using FluentValidation;
using OrthoFlowAI.Application.DTOs;

namespace OrthoFlowAI.Application.Validators;

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Doctor ID is required.");

        RuleFor(x => x.AppointmentDate)
            .NotEmpty().WithMessage("Appointment date is required.")
            .GreaterThan(DateTime.UtcNow.AddMinutes(-30))
            .WithMessage("Appointment date must be in the future.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.AppointmentDate)
            .When(x => x.EndTime.HasValue)
            .WithMessage("End time must be after the appointment date.");

        RuleFor(x => x.Duration)
            .InclusiveBetween(5, 480)
            .WithMessage("Duration must be between 5 and 480 minutes.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");

        RuleFor(x => x.RecurrencePattern)
            .MaximumLength(500).WithMessage("Recurrence pattern must not exceed 500 characters.");

        RuleFor(x => x.RecurrenceEndDate)
            .GreaterThan(x => x.AppointmentDate)
            .When(x => x.RecurrenceEndDate.HasValue)
            .WithMessage("Recurrence end date must be after the appointment date.");
    }
}

public class UpdateAppointmentRequestValidator : AbstractValidator<UpdateAppointmentRequest>
{
    public UpdateAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID cannot be empty.")
            .When(x => x.PatientId != null);

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Doctor ID cannot be empty.")
            .When(x => x.DoctorId != null);

        RuleFor(x => x.AppointmentDate)
            .GreaterThan(DateTime.UtcNow.AddMinutes(-30))
            .When(x => x.AppointmentDate.HasValue)
            .WithMessage("Appointment date must be in the future.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.AppointmentDate)
            .When(x => x.EndTime.HasValue && x.AppointmentDate.HasValue)
            .WithMessage("End time must be after the appointment date.");

        RuleFor(x => x.Duration)
            .InclusiveBetween(5, 480)
            .When(x => x.Duration.HasValue)
            .WithMessage("Duration must be between 5 and 480 minutes.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");

        RuleFor(x => x.RecurrencePattern)
            .MaximumLength(500).WithMessage("Recurrence pattern must not exceed 500 characters.");

        RuleFor(x => x.RecurrenceEndDate)
            .GreaterThan(x => x.AppointmentDate)
            .When(x => x.RecurrenceEndDate.HasValue && x.AppointmentDate.HasValue)
            .WithMessage("Recurrence end date must be after the appointment date.");
    }
}

public class AppointmentFilterRequestValidator : AbstractValidator<AppointmentFilterRequest>
{
    public AppointmentFilterRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("DateTo must be on or after DateFrom.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(500).WithMessage("Search term must not exceed 500 characters.");
    }
}
