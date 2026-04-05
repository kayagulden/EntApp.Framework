using EntApp.Modules.TaskManagement.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.TaskManagement.Application.Validators;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.AuthorUserId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}

public sealed class CreateTimeEntryCommandValidator : AbstractValidator<CreateTimeEntryCommand>
{
    public CreateTimeEntryCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Hours).GreaterThan(0);
    }
}
