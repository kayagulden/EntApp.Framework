using EntApp.Shared.Contracts.Events;

namespace EntApp.Modules.Workflow.Application.IntegrationEvents;

/// <summary>
/// Workflow onayı tamamlandığında yayınlanır.
/// Dinleyiciler: Faz 16 modülleri (Request/Release/ChangeRequest status geçişleri).
/// </summary>
public sealed record ApprovalCompletedEvent(
    Guid InstanceId,
    Guid DefinitionId,
    string DefinitionName,
    string? ReferenceType,
    string? ReferenceId,
    Guid? InitiatorUserId,
    string FinalStatus,
    DateTime CompletedAt) : IntegrationEvent;

/// <summary>
/// Workflow reddedildiğinde yayınlanır.
/// </summary>
public sealed record ApprovalRejectedEvent(
    Guid InstanceId,
    Guid DefinitionId,
    string DefinitionName,
    string? ReferenceType,
    string? ReferenceId,
    Guid? InitiatorUserId,
    Guid RejectedByUserId,
    string? RejectionComment) : IntegrationEvent;

/// <summary>
/// Workflow iptal edildiğinde yayınlanır.
/// </summary>
public sealed record ApprovalCancelledEvent(
    Guid InstanceId,
    Guid DefinitionId,
    string DefinitionName,
    string? ReferenceType,
    string? ReferenceId) : IntegrationEvent;
