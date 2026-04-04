using EntApp.Modules.Audit.Domain.Entities;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using MediatR;

namespace EntApp.Modules.Audit.Application.DTOs;

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserName,
    string Action,
    string? EntityType,
    string? EntityId,
    string? OldValues,
    string? NewValues,
    string? AffectedColumns,
    string? IpAddress,
    string? Description,
    DateTime Timestamp);

public sealed record LoginRecordDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string Result,
    string? IpAddress,
    string? FailureReason,
    DateTime Timestamp);
