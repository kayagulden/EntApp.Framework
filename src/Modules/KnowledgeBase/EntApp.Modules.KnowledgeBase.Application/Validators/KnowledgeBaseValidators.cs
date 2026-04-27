using EntApp.Modules.KnowledgeBase.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.KnowledgeBase.Application.Validators;

public sealed class CreateWikiSpaceValidator : AbstractValidator<CreateWikiSpaceCommand>
{
    public CreateWikiSpaceValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug sadece küçük harf, rakam ve tire içerebilir.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.IconEmoji).MaximumLength(10);
    }
}

public sealed class CreateWikiPageValidator : AbstractValidator<CreateWikiPageCommand>
{
    public CreateWikiPageValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentJson).NotEmpty();
        RuleFor(x => x.ContentHtml).NotEmpty();
    }
}

public sealed class UpdateWikiPageValidator : AbstractValidator<UpdateWikiPageCommand>
{
    public UpdateWikiPageValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(500).When(x => x.Title is not null);
        RuleFor(x => x.ChangeNote).MaximumLength(500).When(x => x.ChangeNote is not null);
    }
}
