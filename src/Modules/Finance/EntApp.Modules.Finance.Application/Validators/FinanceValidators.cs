using EntApp.Modules.Finance.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.Finance.Application.Validators;

public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AccountId).NotEmpty();
    }
}

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
