using EntApp.Modules.Workflow.Application.Commands;
using FluentValidation;

namespace EntApp.Modules.Workflow.Application.Validators;

public sealed class CreateDefinitionCommandValidator : AbstractValidator<CreateDefinitionCommand>
{
    public CreateDefinitionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
    }
}

public sealed class StartWorkflowCommandValidator : AbstractValidator<StartWorkflowCommand>
{
    public StartWorkflowCommandValidator()
    {
        RuleFor(x => x.DefinitionId).NotEmpty().WithMessage("DefinitionId is required.");
    }
}

public sealed class ApproveStepCommandValidator : AbstractValidator<ApproveStepCommand>
{
    public ApproveStepCommandValidator()
    {
        RuleFor(x => x.InstanceId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class RejectStepCommandValidator : AbstractValidator<RejectStepCommand>
{
    public RejectStepCommandValidator()
    {
        RuleFor(x => x.InstanceId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
