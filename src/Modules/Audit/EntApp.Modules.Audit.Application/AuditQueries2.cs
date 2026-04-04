using EntApp.Modules.Audit.Application.DTOs;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using MediatR;

namespace EntApp.Modules.Audit.Application.Queries;

/// <summary>Audit logları sayfalanmış getir.</summary>
public sealed record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    string? EntityType = null,
    string? Action = null,
    Guid? UserId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<Result<PagedResult<AuditLogDto>>>;

/// <summary>Login kayıtlarını sayfalanmış getir.</summary>
public sealed record GetLoginRecordsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    string? Result = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<Result<PagedResult<LoginRecordDto>>>;
