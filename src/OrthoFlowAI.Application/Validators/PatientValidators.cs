using FluentValidation;
using OrthoFlowAI.Application.DTOs;

namespace OrthoFlowAI.Application.Validators;

public class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("A valid phone number is required.")
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.Today).When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth must be in the past.");

        RuleFor(x => x.EmergencyContactPhone)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").When(x => !string.IsNullOrEmpty(x.EmergencyContactPhone))
            .WithMessage("A valid emergency contact phone number is required.");

        RuleFor(x => x.TotalTreatmentCost)
            .GreaterThanOrEqualTo(0).WithMessage("Total treatment cost cannot be negative.");

        RuleFor(x => x.TreatmentStartDate)
            .LessThanOrEqualTo(x => x.EstimatedEndDate).When(x => x.TreatmentStartDate.HasValue && x.EstimatedEndDate.HasValue)
            .WithMessage("Treatment start date must be before the estimated end date.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.Diagnosis)
            .MaximumLength(2000).WithMessage("Diagnosis must not exceed 2000 characters.");

        RuleFor(x => x.TreatmentPlanDescription)
            .MaximumLength(2000).WithMessage("Treatment plan description must not exceed 2000 characters.");

        RuleFor(x => x.Tags)
            .MaximumLength(500).WithMessage("Tags must not exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");

        RuleFor(x => x.InsuranceProvider)
            .MaximumLength(200).WithMessage("Insurance provider must not exceed 200 characters.");

        RuleFor(x => x.InsuranceNumber)
            .MaximumLength(100).WithMessage("Insurance number must not exceed 100 characters.");

        RuleFor(x => x.EmergencyContactName)
            .MaximumLength(100).WithMessage("Emergency contact name must not exceed 100 characters.");
    }
}

public class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name cannot be empty.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.")
            .When(x => x.FirstName != null);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name cannot be empty.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.")
            .When(x => x.LastName != null);

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("A valid phone number is required.")
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.Today).When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth must be in the past.");

        RuleFor(x => x.EmergencyContactPhone)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").When(x => !string.IsNullOrEmpty(x.EmergencyContactPhone))
            .WithMessage("A valid emergency contact phone number is required.");

        RuleFor(x => x.TotalTreatmentCost)
            .GreaterThanOrEqualTo(0).WithMessage("Total treatment cost cannot be negative.")
            .When(x => x.TotalTreatmentCost.HasValue);

        RuleFor(x => x.AmountPaid)
            .GreaterThanOrEqualTo(0).WithMessage("Amount paid cannot be negative.")
            .When(x => x.AmountPaid.HasValue);

        RuleFor(x => x.TreatmentStartDate)
            .LessThanOrEqualTo(x => x.EstimatedEndDate).When(x => x.TreatmentStartDate.HasValue && x.EstimatedEndDate.HasValue)
            .WithMessage("Treatment start date must be before the estimated end date.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.Diagnosis)
            .MaximumLength(2000).WithMessage("Diagnosis must not exceed 2000 characters.");

        RuleFor(x => x.TreatmentPlanDescription)
            .MaximumLength(2000).WithMessage("Treatment plan description must not exceed 2000 characters.");

        RuleFor(x => x.Tags)
            .MaximumLength(500).WithMessage("Tags must not exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");

        RuleFor(x => x.InsuranceProvider)
            .MaximumLength(200).WithMessage("Insurance provider must not exceed 200 characters.");

        RuleFor(x => x.InsuranceNumber)
            .MaximumLength(100).WithMessage("Insurance number must not exceed 100 characters.");

        RuleFor(x => x.EmergencyContactName)
            .MaximumLength(100).WithMessage("Emergency contact name must not exceed 100 characters.");
    }
}
