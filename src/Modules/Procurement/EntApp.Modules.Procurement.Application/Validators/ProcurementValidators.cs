using EntApp.Modules.Procurement.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.Procurement.Application.Validators;

public sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SupplierId).NotEmpty();
    }
}
