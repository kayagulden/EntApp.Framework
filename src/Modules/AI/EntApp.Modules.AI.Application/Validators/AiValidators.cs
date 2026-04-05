using EntApp.Modules.AI.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.AI.Application.Validators;

public sealed class EmbedTextCommandValidator : AbstractValidator<EmbedTextCommand>
{
    public EmbedTextCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage("Text is required.");
    }
}

public sealed class StoreEmbeddingCommandValidator : AbstractValidator<StoreEmbeddingCommand>
{
    public StoreEmbeddingCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().WithMessage("Content is required.");
    }
}
