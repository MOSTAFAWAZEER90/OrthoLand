using FluentValidation;
using OrthoFlowAI.Application.DTOs;

namespace OrthoFlowAI.Application.Validators;

public class CreateClinicRequestValidator : AbstractValidator<CreateClinicRequest>
{
    public CreateClinicRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required.")
            .MaximumLength(200).WithMessage("Clinic name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("A valid phone number is required.")
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.Website)
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _)).When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("A valid URL is required.");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language is required.")
            .MaximumLength(10).WithMessage("Language must not exceed 10 characters.");

        RuleFor(x => x.PrimaryColor)
            .Matches(@"^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrEmpty(x.PrimaryColor))
            .WithMessage("Primary color must be a valid hex color (e.g. #FFFFFF).");

        RuleFor(x => x.SecondaryColor)
            .Matches(@"^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrEmpty(x.SecondaryColor))
            .WithMessage("Secondary color must be a valid hex color (e.g. #000000).");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");
    }
}

public class UpdateClinicRequestValidator : AbstractValidator<UpdateClinicRequest>
{
    public UpdateClinicRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name cannot be empty.")
            .MaximumLength(200).WithMessage("Clinic name must not exceed 200 characters.")
            .When(x => x.Name != null);

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("A valid phone number is required.")
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.Website)
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _)).When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("A valid URL is required.");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language cannot be empty.")
            .MaximumLength(10).WithMessage("Language must not exceed 10 characters.")
            .When(x => x.Language != null);

        RuleFor(x => x.PrimaryColor)
            .Matches(@"^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrEmpty(x.PrimaryColor))
            .WithMessage("Primary color must be a valid hex color (e.g. #FFFFFF).");

        RuleFor(x => x.SecondaryColor)
            .Matches(@"^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrEmpty(x.SecondaryColor))
            .WithMessage("Secondary color must be a valid hex color (e.g. #000000).");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");
    }
}

public class ClinicSettingsRequestValidator : AbstractValidator<ClinicSettingsRequest>
{
    public ClinicSettingsRequestValidator()
    {
        RuleFor(x => x.PrimaryColor)
            .Matches(@"^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrEmpty(x.PrimaryColor))
            .WithMessage("Primary color must be a valid hex color (e.g. #FFFFFF).");

        RuleFor(x => x.SecondaryColor)
            .Matches(@"^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrEmpty(x.SecondaryColor))
            .WithMessage("Secondary color must be a valid hex color (e.g. #000000).");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language cannot be empty.")
            .When(x => x.Language != null);

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("A valid phone number is required.");

        RuleFor(x => x.BusinessHours)
            .MaximumLength(2000).WithMessage("Business hours must not exceed 2000 characters.");

        RuleFor(x => x.WorkingDays)
            .MaximumLength(500).WithMessage("Working days must not exceed 500 characters.");

        RuleFor(x => x.Logo)
            .MaximumLength(500).WithMessage("Logo URL must not exceed 500 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");
    }
}
