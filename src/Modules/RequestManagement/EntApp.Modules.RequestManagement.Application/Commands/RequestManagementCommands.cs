using EntApp.Modules.RequestManagement.Domain.Enums;
using EntApp.Modules.RequestManagement.Domain.Ids;
using MediatR;

namespace EntApp.Modules.RequestManagement.Application.Commands;

// ── RequestCategory ──────────────────────────────────────────
public sealed record CreateCategoryCommand(
    string Name, string Code, Guid DepartmentId,
    string? Description, Guid? SlaDefinitionId,
    string? WorkflowDefinitionId, string? FormSchemaJson,
    int? AutoProjectThreshold, Guid? DefaultQueueId = null) : IRequest<Guid>;

public sealed record UpdateCategoryCommand(
    Guid Id, string Name, string Code, Guid DepartmentId,
    string? Description, Guid? SlaDefinitionId,
    string? WorkflowDefinitionId, string? FormSchemaJson,
    int? AutoProjectThreshold, Guid? DefaultQueueId = null) : IRequest;

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
    TicketChannel Channel, string? FormDataJson = null,
    Guid? ReporterUserId = null,
    Guid? ConfigurationItemId = null) : IRequest<Guid>;

public sealed record UpdateTicketCommand(
    Guid Id, string Title, string? Description,
    TicketPriority Priority,
    Guid? ConfigurationItemId = null) : IRequest;

public sealed record AssignTicketCommand(Guid TicketId, Guid AssigneeUserId) : IRequest;

public sealed record ChangeTicketStatusCommand(
    Guid TicketId, TicketStatus NewStatus, string? Reason) : IRequest;

public sealed record CloseTicketCommand(Guid TicketId, string? Reason) : IRequest;

/// <summary>Ticket'ı belirtilen queue'ya manuel olarak yönlendirir (dispatcher işlemi).</summary>
public sealed record RouteTicketToQueueCommand(Guid TicketId, Guid QueueId) : IRequest;

/// <summary>Kullanıcı ticket'ı kendi üzerine alır (claim). Queue havuzundan self-assign.</summary>
public sealed record ClaimTicketCommand(Guid TicketId, Guid ClaimerUserId) : IRequest;

/// <summary>Ticket'ı havuza geri bırakır — assignee kaldırılır, status Open'a döner.</summary>
public sealed record UnclaimTicketCommand(Guid TicketId) : IRequest;

// ── TicketComment ────────────────────────────────────────────
public sealed record AddCommentCommand(
    Guid TicketId, string Content, bool IsInternal) : IRequest<Guid>;
