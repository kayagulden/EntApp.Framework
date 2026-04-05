using EntApp.Modules.CRM.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.CRM.Application.Validators;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.TaxNumber).MaximumLength(20);
    }
}

public sealed class CreateContactCommandValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => x.Email is not null);
    }
}

public sealed class CreateOpportunityCommandValidator : AbstractValidator<CreateOpportunityCommand>
{
    public CreateOpportunityCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EstimatedValue).GreaterThanOrEqualTo(0);
    }
}
