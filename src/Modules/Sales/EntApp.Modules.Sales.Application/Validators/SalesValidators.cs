using EntApp.Modules.Sales.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.Sales.Application.Validators;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}

public sealed class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
{
    public CreatePriceListCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
