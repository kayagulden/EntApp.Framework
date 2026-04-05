using EntApp.Modules.HR.Application.DTOs;
using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.HR.Application.Queries;

// ── List Employees ──────────────────────────────────────────
public sealed record ListEmployeesQuery(
    string? Search, string? Department, string? Status,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<EmployeeListDto>>;

// ── Get Employee ────────────────────────────────────────────
public sealed record GetEmployeeQuery(Guid Id) : IRequest<EmployeeDetailDto?>;

// ── Org Chart ───────────────────────────────────────────────
public sealed record GetOrgChartQuery() : IRequest<List<OrgChartDto>>;

// ── List Leave Requests ─────────────────────────────────────
public sealed record ListLeaveRequestsQuery(
    Guid? EmployeeId, string? Status,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<LeaveRequestListDto>>;

// ── Leave Balance ───────────────────────────────────────────
public sealed record GetLeaveBalanceQuery(Guid EmployeeId) : IRequest<LeaveBalanceDto?>;

// ── List Attendances ────────────────────────────────────────
public sealed record ListAttendancesQuery(
    Guid? EmployeeId, DateTime? Date,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<AttendanceListDto>>;
