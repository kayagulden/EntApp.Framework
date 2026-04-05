using MediatR;

namespace EntApp.Modules.Workflow.Application.Commands;

public sealed record CreateDefinitionCommand(string Name, string Title,
    string? Description = null, string? Category = null,
    string ApprovalType = "Sequential", string? StepDefinitionsJson = null,
    int? TimeoutHours = null) : IRequest<CreateDefinitionResult>;
public sealed record CreateDefinitionResult(Guid Id, string Name);

public sealed record StartWorkflowCommand(Guid DefinitionId,
    Guid? InitiatorUserId = null, string? ReferenceType = null,
    string? ReferenceId = null, string? Metadata = null) : IRequest<WorkflowActionResult>;

public sealed record ApproveStepCommand(Guid InstanceId, Guid StepId,
    Guid UserId, string? Comment = null) : IRequest<WorkflowActionResult>;

public sealed record RejectStepCommand(Guid InstanceId, Guid StepId,
    Guid UserId, string? Comment = null) : IRequest<WorkflowActionResult>;

public sealed record EscalateStepCommand(Guid InstanceId, Guid StepId,
    Guid EscalateToUserId, string? Comment = null) : IRequest<WorkflowActionResult>;

public sealed record CancelWorkflowCommand(Guid InstanceId) : IRequest<WorkflowActionResult>;

public sealed record WorkflowActionResult(Guid Id, string Status);
