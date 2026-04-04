using EntApp.Modules.Audit.Application.DTOs;
using EntApp.Modules.Audit.Application.Queries;
using EntApp.Modules.Audit.Domain.Entities;
using EntApp.Modules.Audit.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Audit.Infrastructure.Handlers;

// ─── GetAuditLogs ───────────────────────────────────
public sealed class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogDto>>>
{
    private readonly AuditDbContext _db;

    public GetAuditLogsHandler(AuditDbContext db) => _db = db;

    public async Task<Result<PagedResult<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.Action) &&
            Enum.TryParse<AuditAction>(request.Action, true, out var action))
            query = query.Where(a => a.Action == action);

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(a => a.Timestamp >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.Timestamp <= request.ToDate.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.UserId, a.UserName,
                a.Action.ToString(), a.EntityType, a.EntityId,
                a.OldValues, a.NewValues, a.AffectedColumns,
                a.IpAddress, a.Description, a.Timestamp))
            .ToListAsync(ct);

        return Result<PagedResult<AuditLogDto>>.Success(
            new PagedResult<AuditLogDto> { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize });
    }
}

// ─── GetLoginRecords ────────────────────────────────
public sealed class GetLoginRecordsHandler : IRequestHandler<GetLoginRecordsQuery, Result<PagedResult<LoginRecordDto>>>
{
    private readonly AuditDbContext _db;

    public GetLoginRecordsHandler(AuditDbContext db) => _db = db;

    public async Task<Result<PagedResult<LoginRecordDto>>> Handle(GetLoginRecordsQuery request, CancellationToken ct)
    {
        var query = _db.LoginRecords.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(r => r.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Result) &&
            Enum.TryParse<LoginResult>(request.Result, true, out var result))
            query = query.Where(r => r.Result == result);

        if (request.FromDate.HasValue)
            query = query.Where(r => r.Timestamp >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(r => r.Timestamp <= request.ToDate.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new LoginRecordDto(
                r.Id, r.UserId, r.UserName,
                r.Result.ToString(), r.IpAddress,
                r.FailureReason, r.Timestamp))
            .ToListAsync(ct);

        return Result<PagedResult<LoginRecordDto>>.Success(
            new PagedResult<LoginRecordDto> { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize });
    }
}
