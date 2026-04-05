using EntApp.Modules.HR.Application.DTOs;
using EntApp.Modules.HR.Application.Queries;
using EntApp.Modules.HR.Domain.Enums;
using EntApp.Modules.HR.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.HR.Infrastructure.Handlers.Queries;

// ── List Employees ──────────────────────────────────────────
public sealed class ListEmployeesQueryHandler(HrDbContext db)
    : IRequestHandler<ListEmployeesQuery, PagedResult<EmployeeListDto>>
{
    public async Task<PagedResult<EmployeeListDto>> Handle(ListEmployeesQuery request, CancellationToken ct)
    {
        var query = db.Employees.AsQueryable();

        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(e => e.FirstName.Contains(request.Search)
                || e.LastName.Contains(request.Search)
                || e.EmployeeNumber.Contains(request.Search));
        if (!string.IsNullOrEmpty(request.Department))
            query = query.Where(e => e.Department == request.Department);
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<EmployeeStatus>(request.Status, out var s))
            query = query.Where(e => e.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(e => new EmployeeListDto(
                e.Id.Value, e.EmployeeNumber, e.FirstName, e.LastName, e.Email,
                e.Department, e.Position, e.Status.ToString(),
                e.EmploymentType.ToString(), e.HireDate))
            .ToListAsync(ct);

        return new PagedResult<EmployeeListDto>
        {
            Items = items, TotalCount = total,
            PageNumber = request.Page, PageSize = request.PageSize
        };
    }
}

// ── Get Employee ────────────────────────────────────────────
public sealed class GetEmployeeQueryHandler(HrDbContext db)
    : IRequestHandler<GetEmployeeQuery, EmployeeDetailDto?>
{
    public async Task<EmployeeDetailDto?> Handle(GetEmployeeQuery request, CancellationToken ct)
    {
        var e = await db.Employees.Include(x => x.DirectReports)
            .FirstOrDefaultAsync(x => x.Id.Value == request.Id, ct);

        if (e is null) return null;

        return new EmployeeDetailDto(
            e.Id.Value, e.EmployeeNumber, e.FirstName, e.LastName,
            e.Email, e.Phone, e.NationalId, e.DateOfBirth, e.HireDate,
            e.TerminationDate, e.Department, e.Position,
            e.ManagerId?.Value, e.Status.ToString(), e.EmploymentType.ToString(),
            e.AnnualLeaveEntitlement,
            e.DirectReports.Select(d => new EmployeeListDto(
                d.Id.Value, d.EmployeeNumber, d.FirstName, d.LastName, d.Email,
                d.Department, d.Position, d.Status.ToString(),
                d.EmploymentType.ToString(), d.HireDate)).ToList());
    }
}

// ── Org Chart ───────────────────────────────────────────────
public sealed class GetOrgChartQueryHandler(HrDbContext db)
    : IRequestHandler<GetOrgChartQuery, List<OrgChartDto>>
{
    public async Task<List<OrgChartDto>> Handle(GetOrgChartQuery request, CancellationToken ct)
    {
        return await db.Employees
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => new OrgChartDto(e.Id.Value, e.FirstName, e.LastName,
                e.Department, e.Position, e.ManagerId != null ? e.ManagerId.Value.Value : (Guid?)null))
            .ToListAsync(ct);
    }
}

// ── List Leave Requests ─────────────────────────────────────
public sealed class ListLeaveRequestsQueryHandler(HrDbContext db)
    : IRequestHandler<ListLeaveRequestsQuery, PagedResult<LeaveRequestListDto>>
{
    public async Task<PagedResult<LeaveRequestListDto>> Handle(ListLeaveRequestsQuery request, CancellationToken ct)
    {
        var query = db.LeaveRequests.Include(l => l.Employee).AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(l => l.EmployeeId.Value == request.EmployeeId.Value);
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<LeaveRequestStatus>(request.Status, out var s))
            query = query.Where(l => l.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(l => l.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(l => new LeaveRequestListDto(
                l.Id.Value, l.EmployeeId.Value,
                l.Employee.FirstName + " " + l.Employee.LastName,
                l.LeaveType.ToString(), l.Status.ToString(),
                l.StartDate, l.EndDate, l.TotalDays, l.Reason, l.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<LeaveRequestListDto>
        {
            Items = items, TotalCount = total,
            PageNumber = request.Page, PageSize = request.PageSize
        };
    }
}

// ── Leave Balance ───────────────────────────────────────────
public sealed class GetLeaveBalanceQueryHandler(HrDbContext db)
    : IRequestHandler<GetLeaveBalanceQuery, LeaveBalanceDto?>
{
    public async Task<LeaveBalanceDto?> Handle(GetLeaveBalanceQuery request, CancellationToken ct)
    {
        var employee = await db.Employees.FindAsync([request.EmployeeId], ct);
        if (employee is null) return null;

        var usedDays = await db.LeaveRequests
            .Where(l => l.EmployeeId.Value == request.EmployeeId
                && l.LeaveType == LeaveType.Annual
                && l.Status == LeaveRequestStatus.Approved
                && l.StartDate.Year == DateTime.UtcNow.Year)
            .SumAsync(l => l.TotalDays, ct);

        return new LeaveBalanceDto(
            request.EmployeeId, employee.FullName, DateTime.UtcNow.Year,
            employee.AnnualLeaveEntitlement, usedDays,
            employee.AnnualLeaveEntitlement - usedDays);
    }
}

// ── List Attendances ────────────────────────────────────────
public sealed class ListAttendancesQueryHandler(HrDbContext db)
    : IRequestHandler<ListAttendancesQuery, PagedResult<AttendanceListDto>>
{
    public async Task<PagedResult<AttendanceListDto>> Handle(ListAttendancesQuery request, CancellationToken ct)
    {
        var query = db.Attendances.AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(a => a.EmployeeId.Value == request.EmployeeId.Value);
        if (request.Date.HasValue)
            query = query.Where(a => a.Date == request.Date.Value.Date);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.Date)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(a => new AttendanceListDto(
                a.Id.Value, a.EmployeeId.Value, a.Date, a.CheckIn, a.CheckOut,
                a.Status.ToString(), a.WorkedHours, a.OvertimeHours))
            .ToListAsync(ct);

        return new PagedResult<AttendanceListDto>
        {
            Items = items, TotalCount = total,
            PageNumber = request.Page, PageSize = request.PageSize
        };
    }
}
