using EntApp.Modules.MyModule.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.MyModule.Application.Validators;

/// <summary>
/// FluentValidation kuralları — ValidationBehavior pipeline'ı
/// tarafından otomatik çalıştırılır, endpoint'te manual kontrol gerekmez.
/// </summary>

public sealed class CreateSampleEntityCommandValidator : AbstractValidator<CreateSampleEntityCommand>
{
    public CreateSampleEntityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");
    }
}

public sealed class UpdateSampleEntityCommandValidator : AbstractValidator<UpdateSampleEntityCommand>
{
    public UpdateSampleEntityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);
    }
}
