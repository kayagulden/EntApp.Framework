using EntApp.Modules.RequestManagement.Domain.Enums;
using EntApp.Modules.RequestManagement.Domain.Ids;
using MediatR;

namespace EntApp.Modules.RequestManagement.Application.Commands;

// ── Department ───────────────────────────────────────────────
public sealed record CreateDepartmentCommand(
    string Name, string Code, string? Description,
    Guid? ManagerUserId, Guid? ParentDepartmentId) : IRequest<Guid>;

public sealed record UpdateDepartmentCommand(
    Guid Id, string Name, string Code, string? Description,
    Guid? ManagerUserId, Guid? ParentDepartmentId) : IRequest;

// ── RequestCategory ──────────────────────────────────────────
public sealed record CreateCategoryCommand(
    string Name, string Code, Guid DepartmentId,
    string? Description, Guid? SlaDefinitionId,
    Guid? WorkflowDefinitionId, string? FormSchemaJson,
    int? AutoProjectThreshold) : IRequest<Guid>;

public sealed record UpdateCategoryCommand(
    Guid Id, string Name, string Code, Guid DepartmentId,
    string? Description, Guid? SlaDefinitionId,
    Guid? WorkflowDefinitionId, string? FormSchemaJson,
    int? AutoProjectThreshold) : IRequest;

// ── SlaDefinition ────────────────────────────────────────────
public sealed record CreateSlaCommand(
    string Name, string? Description,
    string? ResponseTimeJson, string? ResolutionTimeJson) : IRequest<Guid>;

public sealed record UpdateSlaCommand(
    Guid Id, string Name, string? Description,
    string? ResponseTimeJson, string? ResolutionTimeJson) : IRequest;

// ── Ticket ───────────────────────────────────────────────────
public sealed record CreateTicketCommand(
    string Title, Guid CategoryId, Guid DepartmentId,
    string? Description, TicketPriority Priority,
    TicketChannel Channel) : IRequest<Guid>;

public sealed record UpdateTicketCommand(
    Guid Id, string Title, string? Description,
    TicketPriority Priority) : IRequest;

public sealed record AssignTicketCommand(Guid TicketId, Guid AssigneeUserId) : IRequest;

public sealed record ChangeTicketStatusCommand(
    Guid TicketId, TicketStatus NewStatus, string? Reason) : IRequest;

public sealed record CloseTicketCommand(Guid TicketId, string? Reason) : IRequest;

// ── TicketComment ────────────────────────────────────────────
public sealed record AddCommentCommand(
    Guid TicketId, string Content, bool IsInternal) : IRequest<Guid>;
