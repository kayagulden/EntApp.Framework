using EntApp.Modules.Inventory.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.Inventory.Application.Validators;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateStockMovementCommandValidator : AbstractValidator<CreateStockMovementCommand>
{
    public CreateStockMovementCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
