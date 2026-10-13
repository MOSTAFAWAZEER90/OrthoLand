using FluentValidation;
using OrthoFlowAI.Application.DTOs;

namespace OrthoFlowAI.Application.Validators;

public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Total amount must be greater than zero.");

        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.");

        RuleFor(x => x.Tax)
            .GreaterThanOrEqualTo(0).WithMessage("Tax cannot be negative.");

        RuleFor(x => x.Discount)
            .LessThan(x => x.TotalAmount)
            .WithMessage("Discount must be less than the total amount.");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");

        RuleFor(x => x.ItemsJson)
            .MaximumLength(8000).WithMessage("Items JSON must not exceed 8000 characters.");
    }
}

public class UpdateInvoiceRequestValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceRequestValidator()
    {
        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Total amount must be greater than zero.")
            .When(x => x.TotalAmount.HasValue);

        RuleFor(x => x.PaidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Paid amount cannot be negative.")
            .When(x => x.PaidAmount.HasValue);

        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.")
            .When(x => x.Discount.HasValue);

        RuleFor(x => x.Tax)
            .GreaterThanOrEqualTo(0).WithMessage("Tax cannot be negative.")
            .When(x => x.Tax.HasValue);

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");

        RuleFor(x => x.ItemsJson)
            .MaximumLength(8000).WithMessage("Items JSON must not exceed 8000 characters.");
    }
}

public class InvoiceFilterRequestValidator : AbstractValidator<InvoiceFilterRequest>
{
    public InvoiceFilterRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.DueDateTo)
            .GreaterThanOrEqualTo(x => x.DueDateFrom)
            .When(x => x.DueDateFrom.HasValue && x.DueDateTo.HasValue)
            .WithMessage("DueDateTo must be on or after DueDateFrom.");

        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Min amount cannot be negative.")
            .When(x => x.MinAmount.HasValue);

        RuleFor(x => x.MaxAmount)
            .GreaterThanOrEqualTo(x => x.MinAmount)
            .When(x => x.MaxAmount.HasValue && x.MinAmount.HasValue)
            .WithMessage("Max amount must be greater than or equal to min amount.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(500).WithMessage("Search term must not exceed 500 characters.");
    }
}

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("Invoice ID is required.");

        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.");

        RuleFor(x => x.TransactionId)
            .MaximumLength(200).WithMessage("Transaction ID must not exceed 200 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
    }
}
